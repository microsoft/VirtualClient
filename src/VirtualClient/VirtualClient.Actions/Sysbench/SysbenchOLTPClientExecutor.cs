// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Sysbench Client workload executor.
    /// </summary>
    public class SysbenchOLTPClientExecutor : SysbenchOLTPExecutor
    {
        private const string SysbenchFileName = "src/sysbench";
        private static readonly string[] SingleTableWorkloads =
        [
            "select_random_points",
            "select_random_ranges"
        ];

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
        public TimeSpan Duration
        {
            get
            { 
                return this.Parameters.GetTimeSpanValue(nameof(SysbenchOLTPClientExecutor.Duration), TimeSpan.FromMinutes(5));
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

                if (this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.NumTables), out IConvertible tables))
                {
                    numTables = tables.ToInt32(CultureInfo.InvariantCulture);
                }
                
                if (SysbenchOLTPClientExecutor.SingleTableWorkloads.Contains(this.Workload, StringComparer.OrdinalIgnoreCase))
                {
                    numTables = 1;
                }

                return numTables;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool PopulateDatabase
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.PopulateDatabase), false);
            }
        }

        /// <summary>
        /// Number of records per table.
        /// </summary>
        public int RecordCount
        {
            get
            {
                int numRecords = 1;

                // default formulaic setup of the database
                // records & threads depend on the core count
                if (this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.RecordCount), out IConvertible recordCount))
                {
                    numRecords = recordCount.ToInt32(CultureInfo.InvariantCulture);
                }
                else
                {
                    CpuInfo cpuInfo = this.SystemManager.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
                    int coreCount = cpuInfo.LogicalProcessorCount;

                    int recordCountExponent = this.DatabaseScenario == SysbenchOLTPScenario.Balanced 
                        ? (int)Math.Log2(coreCount)
                        : (int)Math.Log2(coreCount) + 2;

                    numRecords = (int)Math.Pow(10, recordCountExponent);
                }

                return numRecords;
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int Threads
        {
            get
            {
                // Sysbench default number of threads
                int numThreads = 1;

                // default formulaic setup of the database threads depend on the core count
                if (this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.Threads), out IConvertible threads) && threads != null)
                {
                    numThreads = threads.ToInt32(CultureInfo.InvariantCulture);
                }
                else
                {
                    CpuInfo cpuInfo = this.SystemManager.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
                    return cpuInfo.LogicalProcessorCount;
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
            List<Task> clientWorkloadTasks = new List<Task>();

            SysbenchOLTPState state = await this.stateManager.GetStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), cancellationToken)
               ?? new SysbenchOLTPState();

            if (!state.SysbenchInitialized)
            {
                // install sysbench using repo scripts
                await this.InitializeSysbench(state, cancellationToken).ConfigureAwait(false);
            }

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

                            // 3) Execute the client workload.
                            // ===========================================================================
                            if (this.PopulateDatabase)
                            {
                                this.Logger.LogTraceMessage("Synchronization: Populate database...");
                                await this.PrepareMySQLDatabase(state, telemetryContext, cancellationToken);
                            }
                            else
                            {
                                this.Logger.LogTraceMessage("Synchronization: Start client workload...");
                                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                                    .ConfigureAwait(false);
                            }
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
                        if (this.PopulateDatabase)
                        {
                            this.Logger.LogTraceMessage("Synchronization: Populate database...");
                            await this.PrepareMySQLDatabase(state, telemetryContext, cancellationToken);
                        }
                        else
                        {
                            await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
                        }
                    }
                }));
            }

            await Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            this.sysbenchDirectory = this.GetPackagePath(this.PackageName);
            this.sysbenchPrepareArguments = $@"oltp_common --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} --mysql-host={this.ServerIpAddress} prepare";
            this.sysbenchLoggingArguments = $"{this.Workload} --threads={this.Threads} --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--mysql-host={this.ServerIpAddress} --time={this.Duration.TotalSeconds} ";
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
                            scenarioName: this.MetricScenario ?? this.Scenario,
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

        private async Task InitializeSysbench(SysbenchOLTPState state, CancellationToken cancellationToken)
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

            state.SysbenchInitialized = true;
            await this.stateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken);
        }

        private async Task PrepareMySQLDatabase(SysbenchOLTPState state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // int numTables = -1;
            // int recordCount = -1;

            // use mysql-client tool to get table/record counts from the mysql server
            // table count output is like so:
            // +--------------+
            // | FOUND_ROWS() |
            // +--------------+
            // |            0 |
            // +--------------+
            //
            // record count output is like so:
            // +----------+
            // | COUNT(*) |
            // +----------+
            // |        0 |
            // +----------+

            ////string getMySQLTableCountCommand = $"mysql -u {this.DatabaseName} -h {this.ServerIpAddress} {this.DatabaseName} --execute=\"USE {this.DatabaseName}; SHOW tables; SELECT FOUND_ROWS();\"";

            ////// note that sysbench standardizes table names; database name is up to the user, but each table name will always be "sbtest1, sbtest2, ..."
            ////// if at least one table exists, we can take a look at its record count to reliably obtain the record counts for all tables
            ////string getMySQLRecordCountCommand = $"mysql -u {this.DatabaseName} -h {this.ServerIpAddress} {this.DatabaseName} --execute=\"USE {this.DatabaseName}; SELECT COUNT(*) FROM sbtest1;\"";

            ////string result = await this.ExecuteCommandAsync<SysbenchOLTPServerExecutor>(getMySQLTableCountCommand, null, Environment.CurrentDirectory, cancellationToken)
            ////    .ConfigureAwait(false);

            ////Match match = Regex.Match(result, "[1-9][0-9]*");

            ////if (match.Success)
            ////{
            ////    numTables = Convert.ToInt32(match.Value);

            ////    result = await this.ExecuteCommandAsync<SysbenchOLTPServerExecutor>(getMySQLRecordCountCommand, null, Environment.CurrentDirectory, cancellationToken)
            ////        .ConfigureAwait(false);

            ////    match = Regex.Match(result, "[1-9][0-9]*|0");
            ////    recordCount = match.Success ? Convert.ToInt32(match.Value) : -1;
            ////}

            if (this.PopulateDatabase && !state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    if (this.DatabaseScenario == SysbenchOLTPScenario.Balanced)
                    {
                        if (string.IsNullOrWhiteSpace(state.DiskPathsArgument))
                        {
                            throw new WorkloadException(
                                $"Database data disk paths not defined for MySQL server. In a 'Balanced' scenario, the database is distributed " +
                                $"across a set of disks on the MySQL server system. The server is expected to provide the set of disk paths but did not.",
                                ErrorReason.DependencyNotFound);

                        }

                        await this.PrepareBalancedScenarioAsync(state.DiskPathsArgument, telemetryContext, cancellationToken);
                    }
                    else
                    {
                        await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchExecutionArguments + "cleanup", this.sysbenchDirectory, cancellationToken)
                            .ConfigureAwait(false);

                        await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(this.sysbenchPath, this.sysbenchPrepareArguments, this.sysbenchDirectory, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken);
                });
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
