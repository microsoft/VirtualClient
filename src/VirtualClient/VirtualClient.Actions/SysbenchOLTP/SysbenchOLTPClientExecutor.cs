// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

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
        public string DurationSecs
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.DurationSecs));
            }
        }

        /// <summary>
        /// The database name option passed to Sysbench.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.DatabaseName));
            }
        }

        /// <summary>
        /// The number of tables option passed to Sysbench.
        /// </summary>
        public string NumTables
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.NumTables));
            }
        }

        /// <summary>
        /// The table size option passed to Sysbench.
        /// </summary>
        public string RecordCount
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.RecordCount));
            }
        }

        /// <summary>
        /// The number of threads option passed to Sysbench.
        /// </summary>
        public string Threads
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.Threads));
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
            }

            state.TableCount = state.TableCount < 0 ? 0 : state.TableCount;
            state.RecordCount = state.RecordCount < 0 ? 0 : state.RecordCount;

            await this.stateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken);

            // set arguments & path up based on prepare arguments in profile 

            this.sysbenchPrepareArguments = $@"oltp_common --tables={this.NumTables} --mysql-db={this.DatabaseName} --mysql-host={this.ServerIpAddress} prepare";
            this.sysbenchLoggingArguments = $"{this.Workload} --threads={this.Threads} --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--mysql-host={this.ServerIpAddress} --time={this.DurationSecs} ";
            this.sysbenchPath = this.PlatformSpecifics.Combine(this.sysbenchDirectory, SysbenchOLTPClientExecutor.SysbenchFileName);
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    SysbenchOLTPMetricsParser parser = new SysbenchOLTPMetricsParser(process.StandardOutput.ToString());
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        toolName: "Sysbench",
                        scenarioName: "OLTP",
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

            int tableCount = int.Parse(this.NumTables);
            int recordCount = int.Parse(this.RecordCount);

            // sysbench has a bug in preparing the 12th table on arm64 architecture

            if (this.CpuArchitecture == Architecture.Arm64 && tableCount > 11)
            {
                throw new WorkloadException(
                        $"The Sysbench OLTP workload does not support a configuration of 12 or more tables on Arm64 architecture." +
                        $"Please reconfigure your parameters and try again.",
                        ErrorReason.PlatformNotSupported);
            }

            if (tableCount != state.TableCount || recordCount != state.RecordCount)
            {
                // only cleanup & prepare it if needed -- ie. if the state table/record counts are different than current

                await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchExecutionArguments + "cleanup", this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);
                
                await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchPrepareArguments, this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);

                // update the state object accordingly

                state.TableCount = tableCount;
                state.RecordCount = recordCount;

                await this.stateManager.SaveStateAsync(nameof(SysbenchOLTPState), state, cancellationToken);
            }

            if (this.DatabaseScenario == SysbenchOLTPScenario.Balanced)
            {
                string diskPaths = state.DiskPathsArgument;

                await this.PrepareBalancedScenarioAsync(diskPaths, telemetryContext, cancellationToken);

                state.DatabaseScenarioInitialized = true;

                await this.stateManager.SaveStateAsync(nameof(SysbenchOLTPState), state, cancellationToken);
            }
        }

        private async Task PrepareBalancedScenarioAsync(string diskPaths, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string balancedScript = "balancedClient.sh";
            string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("sysbencholtp");
            string balancedArguments = $"{this.ServerIpAddress} {this.NumTables} {this.DatabaseName} {diskPaths}";

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
