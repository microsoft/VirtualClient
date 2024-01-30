// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Configures the MySQL database for Sysbench use.
    /// </summary>
    public class SysbenchConfiguration : SysbenchExecutor
    {
        private readonly IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchConfiguration"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public SysbenchConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.stateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchConfiguration.DiskFilter), string.Empty);
            }
        }

        /// <summary>
        /// The workload option passed to Sysbench.
        /// </summary>
        public int NumTables
        {
            get
            {
                int numTables = 10;

                if (this.Parameters.TryGetValue(nameof(SysbenchConfiguration.NumTables), out IConvertible tables)
                    && this.DatabaseScenario != SysbenchScenario.Balanced)
                {
                    numTables = tables.ToInt32(CultureInfo.InvariantCulture);
                }

                return numTables;
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int Threads
        {
            get
            {
                int numThreads = 1;

                if (this.Parameters.TryGetValue(nameof(SysbenchConfiguration.Threads), out IConvertible threads) && threads != null)
                {
                    numThreads = threads.ToInt32(CultureInfo.InvariantCulture);
                }

                return numThreads;
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.PrepareMySQLDatabase(state, telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Sysbench server side.
        /// </summary>
        protected async override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("sysbench");

                await this.SystemManager.MakeFilesExecutableAsync(
                    scriptsDirectory,
                    this.Platform,
                    cancellationToken);
            }

            await base.InitializeAsync(telemetryContext, cancellationToken);
            return;
        }

        private async Task PrepareMySQLDatabase(SysbenchState state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    await this.PopulateDatabaseAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
                });
            }

        }

        private async Task PopulateDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // includes copying all tables from OS disk to data disk,
            // dropping old tables & renaming them

            string balancedScript = "distribute-database.sh";
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName);
            this.SysbenchPackagePath = this.GetPackagePath(this.PackageName);

            string diskPaths = await this.GetDiskPathsAsync(telemetryContext, cancellationToken);

            string arguments = $"{this.SysbenchPackagePath} {this.DatabaseName} {this.NumTables} {this.RecordCount - 1} {this.Threads} {diskPaths}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                this.PlatformSpecifics.Combine(scriptsDirectory, balancedScript),
                arguments,
                scriptsDirectory,
                telemetryContext,
                cancellationToken,
                runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfWorkloadFailed();
                }
            }
        }

        private async Task<string> GetDiskPathsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string diskPaths = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                string diskFilter = "osdisk:false";

                if (!string.IsNullOrEmpty(this.DiskFilter))
                {
                    diskFilter += string.Concat("&", this.DiskFilter);
                }

                IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (disks?.Any() != true)
                {
                    throw new WorkloadException(
                        "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, diskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                foreach (Disk disk in disksToTest)
                {
                    if (disk.GetPreferredAccessPath(this.Platform) != "/mnt")
                    {
                        diskPaths += $"{disk.GetPreferredAccessPath(this.Platform)} ";
                    }
                }
            }

            return diskPaths;
        }
    }
}
