// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Sysbench Client workload executor.
    /// </summary>
    public class SysbenchOLTPClientExecutor : SysbenchOLTPExecutor
    {
        private const string SysbenchFileName = "src/sysbench";
        private readonly IPackageManager packageManager;
        private readonly IStateManager stateManager;
        private string sysbenchPrepareArguments;
        private string sysbenchExecutionArguments;
        private string sysbenchLoggingArguments;
        private string sysbenchDirectory;
        private string sysbenchPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchOLTPClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public SysbenchOLTPClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.packageManager = this.SystemManager.PackageManager;
            this.stateManager = this.SystemManager.StateManager;
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// The total time of execution option passed to Sysbench.
        /// </summary>
        public string Duration
        {
            get
            { 
                string timeSpan = this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.Duration));
                return TimeSpan.Parse(timeSpan).TotalSeconds.ToString();
            }
        }

        /// <summary>
        /// The database name option passed to Sysbench.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.DatabaseName), "sbtest");
            }
        }

        /// <summary>
        /// The workload option passed to Sysbench.
        /// </summary>
        public string Workload
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.Workload));
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

                if (this.Parameters.TryGetValue(nameof(this.NumTables), out IConvertible value) && value != null
                    && this.CpuArchitecture != Architecture.Arm64)
                {
                    numTables = value.ToInt32(CultureInfo.InvariantCulture);
                }

                return numTables;
            }
        }

        /// <summary>
        /// Number of records per table.
        /// </summary>
        public int RecordCount
        {
            get
            {
                // default formulaic setup of the database
                // records & threads depend on the core count

                CpuInfo cpuInfo = this.SystemManager.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
                int coreCount = cpuInfo.LogicalCoreCount;

                int recordCountExponent = this.DatabaseScenario == SysbenchOLTPScenario.Balanced ? 
                    (int)Math.Log2(coreCount) : (int)Math.Log2(coreCount) + 2;

                int numRecords = (int)Math.Pow(10, recordCountExponent);

                if (this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.RecordCount), out IConvertible recordCount)
                    && this.DatabaseScenario != SysbenchOLTPScenario.Balanced)
                {
                    numRecords = recordCount.ToInt32(CultureInfo.InvariantCulture);
                }

                return numRecords;
            }
        }

        /// <summary>
        /// Skips initialization of the tables and records in the database
        /// </summary>
        public bool SkipInitialize
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SysbenchOLTPClientExecutor.SkipInitialize), true);
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int Threads
        {
            get
            {
                // default formulaic setup of the database threads depend on the core count

                CpuInfo cpuInfo = this.SystemManager.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();

                int numThreads = this.DatabaseScenario == SysbenchOLTPScenario.Balanced ? 
                    1 : cpuInfo.LogicalCoreCount * 8;

                if (this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.Threads), out IConvertible threads) && threads != null)
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
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance server in targetServers)
                {
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // 1) Confirm server is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                            await this.ServerApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                .ConfigureAwait(false);

                            // 2) Confirm the server-side application (e.g. web server) is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                            await this.ServerApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Execute the client workload.
                            // ===========================================================================
                            await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }));
                }
            }
            else
            {
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.InitializeApiClients();

            // get sysbench workload path

            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None)
                .ConfigureAwait(false);

            this.sysbenchDirectory = this.GetPackagePath(this.PackageName);

            // store state with initialization status & record/table counts, if does not exist already

            SysbenchOLTPState state = await this.stateManager.GetStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), cancellationToken)
                ?? new SysbenchOLTPState();

            if (!state.SysbenchInitialized)
            {
                // install sysbench using repo scripts
                await this.InstallSysbenchOLTPPackage(cancellationToken).ConfigureAwait(false);
                state.SysbenchInitialized = true;

                await this.stateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken);
            }

            this.sysbenchPrepareArguments = $@"oltp_common --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} --mysql-host={this.ServerIpAddress} prepare";
            this.sysbenchLoggingArguments = $"{this.Workload} --threads={this.Threads} --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--mysql-host={this.ServerIpAddress} --time={this.Duration} ";
            this.sysbenchPath = this.PlatformSpecifics.Combine(this.sysbenchDirectory, SysbenchOLTPClientExecutor.SysbenchFileName);
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Sysbench",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string text = process.StandardOutput.ToString();

                if (!string.IsNullOrEmpty(text)) 
                {
                    try
                    {
                        SysbenchOLTPMetricsParser parser = new SysbenchOLTPMetricsParser(text);
                        IList<Metric> metrics = parser.Parse();

                        this.Logger.LogMetrics(
                            toolName: "Sysbench",
                            scenarioName: "OLTP " + this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            metrics,
                            null,
                            scenarioArguments: this.sysbenchLoggingArguments,
                            this.Tags,
                            telemetryContext);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadException($"Failed to parse sysbench output.", exc, ErrorReason.InvalidResults);
                    }
                }
            }
        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // first, prepare database if needed; then run the sysbench command

                    await this.PrepareMySQLDatabase(telemetryContext, cancellationToken);

                    using (IProcessProxy process = await this.ExecuteCommandAsync(this.sysbenchPath, this.sysbenchExecutionArguments + "run", this.sysbenchDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                            this.CaptureMetrics(process, telemetryContext, cancellationToken);
                        }
                    }
                }
            });
        }

        private async Task InstallSysbenchOLTPPackage(CancellationToken cancellationToken) 
        {
            const string autogenScriptCommand = "./autogen.sh";
            const string configureScriptCommand = "./configure";
            const string makeCommand = "make -j";
            const string makeInstallCommand = "make install";

            // build sysbench
            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(autogenScriptCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(configureScriptCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(makeCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(makeInstallCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task PrepareMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchOLTPState state = await this.stateManager.GetStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), cancellationToken);

            if (!this.SkipInitialize || this.NumTables > state.NumTables || this.RecordCount > state.RecordCount)
            {
                // only cleanup & prepare it if needed -- ie. if the state table/record counts are different than current

                await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchExecutionArguments + "cleanup", this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchPrepareArguments, this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);

                state.Properties[nameof(SysbenchOLTPState.NumTables)] = this.NumTables;
                state.Properties[nameof(SysbenchOLTPState.RecordCount)] = this.RecordCount;

                // save the updated state configuration

                await this.stateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken);
            }

            if (this.DatabaseScenario == SysbenchOLTPScenario.Balanced)
            {
                // grab state stored by the server -- this has the list of disk paths

                HttpResponseMessage response = await this.ServerApiClient.GetStateAsync(nameof(SysbenchOLTPState), cancellationToken)
                    .ConfigureAwait(false);

                string responseContent = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                SysbenchOLTPState serverState = responseContent.FromJson<SysbenchOLTPState>();

                string diskPaths = serverState.Properties[nameof(SysbenchOLTPState.DiskPathsArgument)].ToString();

                await this.PrepareBalancedScenarioAsync(diskPaths, telemetryContext, cancellationToken);
            }
        }

        private async Task PrepareBalancedScenarioAsync(string diskPaths, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // client work in the balanced scenario includes copying all tables from OS disk to data disk,
            // dropping old tables & renaming them

            string balancedScript = "balanced-client.sh";
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("sysbencholtp");
            string balancedArguments = $"{this.ServerIpAddress} 10 {this.DatabaseName} {diskPaths}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                this.PlatformSpecifics.Combine(scriptsDirectory, balancedScript),
                balancedArguments,
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
    }
}
