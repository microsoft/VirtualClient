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
        public int TableCount
        {
            get
            {
                int tableCount = 10;

                if (this.Parameters.TryGetValue(nameof(SysbenchConfiguration.TableCount), out IConvertible tables)
                    && this.DatabaseScenario != SysbenchScenario.Balanced)
                {
                    tableCount = tables.ToInt32(CultureInfo.InvariantCulture);
                }

                return tableCount;
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

            if (!state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.PrepareMySQLDatabase(telemetryContext, cancellationToken);
                    }
                });

                if (this.RecordCount > 1)
                {
                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
                }
            }
        }

        private async Task PrepareMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = $"python3";

            string arguments = $"{this.SysbenchPackagePath}/populate-database.py --dbName {this.DatabaseName} " +
                $"--tableCount {this.TableCount} --recordCount {this.RecordCount} --threadCount {this.Threads}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
                arguments,
                this.SysbenchPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                }
            }
        }
    }
}
