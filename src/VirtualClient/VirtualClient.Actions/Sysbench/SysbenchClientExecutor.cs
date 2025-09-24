// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Sysbench Client workload executor.
    /// </summary>
    public class SysbenchClientExecutor : SysbenchExecutor
    {
        private string sysbenchExecutionArguments;
        private string sysbenchLoggingArguments;
        private string sysbenchPrepareArguments;
        private string packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public SysbenchClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
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
                return this.Parameters.GetTimeSpanValue(nameof(SysbenchClientExecutor.Duration), TimeSpan.FromMinutes(5));
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
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

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
                        await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
                    }
                }));
            }

            await Task.WhenAll(clientWorkloadTasks);
            return;
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
                        SysbenchMetricsParser parser = new SysbenchMetricsParser(text);
                        IList<Metric> metrics = parser.Parse();
                        string sysbenchVersion = null;

                        var sysbenchVersionMetric = metrics.FirstOrDefault();
                        if (sysbenchVersionMetric != null && sysbenchVersionMetric.Metadata.TryGetValue("sysbench_version", out var versionValue))
                        {
                            sysbenchVersion = versionValue?.ToString();
                        }

                        if (!string.IsNullOrEmpty(sysbenchVersion))
                        {
                            this.MetadataContract.Add("sysbench_version", sysbenchVersion, MetadataContract.DependenciesCategory);
                        }

                        this.MetadataContract.Apply(telemetryContext);

                        this.Logger.LogMetrics(
                            toolName: "Sysbench",
                            scenarioName: this.MetricScenario ?? this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            metrics,
                            null,
                            scenarioArguments: this.sysbenchLoggingArguments,
                            this.Tags,
                            telemetryContext,
                            toolVersion: sysbenchVersion);
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
                    if (this.DatabaseSystem == "MySQL")
                    {
                        string mysqlVersion = await this.GetMySQLVersionAsync(telemetryContext, cancellationToken);

                        this.MetadataContract.Add("mysql_version", mysqlVersion, MetadataContract.DependenciesCategory);
                        this.MetadataContract.Apply(telemetryContext);
                    }

                    if (this.Benchmark == BenchmarkName.OLTP)
                    {
                        if (this.Action == ClientAction.TruncateDatabase)
                        {
                            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
                            workloadPackage.ThrowIfNull(this.PackageName);

                            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
                            this.packageDirectory = package.Path;

                            await this.TruncateMySQLDatabaseAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        }
                        else if (this.Action == ClientAction.PopulateDatabase)
                        {
                            await this.PrepareOLTPMySQLDatabase(telemetryContext, cancellationToken);
                        }
                        else if (this.Action == ClientAction.Cleanup)
                        {
                            await this.CleanUpDatabase(telemetryContext, cancellationToken);
                        }
                        else
                        {
                            await this.RunOLTPWorkloadAsync(telemetryContext, cancellationToken);
                        }
                    }
                    else if (this.Benchmark == BenchmarkName.TPCC)
                    {
                        await this.RunTPCCWorkloadAsync(telemetryContext, cancellationToken);
                    }
                    else
                    {
                        throw new DependencyException(
                            $"The '{this.Benchmark}' benchmark is not supported with the Sysbench workload. Supported options include: \"OLTP, TPCC\".", 
                            ErrorReason.NotSupported);
                    }
                }
            });
        }

        private async Task RunOLTPWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int recordCount = GetRecordCount(this.SystemManager, this.DatabaseScenario, this.RecordCount);

            this.sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --workload {this.Workload} --threadCount {threadCount} --tableCount {tableCount} --recordCount {recordCount} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--hostIpAddress {this.ServerIpAddress} --durationSecs {this.Duration.TotalSeconds} --password {this.SuperUserPassword}";

            string script = $"{this.SysbenchPackagePath}/run-workload.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                SysbenchExecutor.PythonCommand,
                script + this.sysbenchExecutionArguments,
                this.SysbenchPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);

                    this.CaptureMetrics(process, telemetryContext, cancellationToken);
                }
            }
        }

        private async Task CleanUpDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);

            string serverIp = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? "localhost";

            string sysbenchCleanupArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --tableCount {tableCount}  --hostIpAddress {serverIp}";

            string script = $"{this.SysbenchPackagePath}/cleanup-database.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                SysbenchExecutor.PythonCommand,
                script + sysbenchCleanupArguments,
                this.SysbenchPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);
                }
            }
        }

        private async Task RunTPCCWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.Scenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int warehouseCount = GetWarehouseCount(this.DatabaseScenario, this.WarehouseCount);

            this.sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --workload tpcc --threadCount {threadCount} --tableCount {tableCount} --warehouses {warehouseCount} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--hostIpAddress {this.ServerIpAddress} --durationSecs {this.Duration.TotalSeconds} --password {this.SuperUserPassword}";

            string script = $"{this.SysbenchPackagePath}/run-workload.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                SysbenchExecutor.PythonCommand,
                script + this.sysbenchExecutionArguments,
                this.SysbenchPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);

                    this.CaptureMetrics(process, telemetryContext, cancellationToken);
                }
            }
        }

        private async Task TruncateMySQLDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string arguments = $"{this.packageDirectory}/truncate-tables.py --dbName {this.DatabaseName}";

            string serverIps = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? "localhost";

            arguments += $" --clientIps \"{serverIps}\"";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                SysbenchExecutor.PythonCommand,
                arguments,
                Environment.CurrentDirectory,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task PrepareOLTPMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int recordCount = GetRecordCount(this.SystemManager, this.DatabaseScenario, this.RecordCount);

            this.sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --threadCount {threadCount} --tableCount {tableCount} --recordCount {recordCount}";
            this.sysbenchPrepareArguments = $"{this.sysbenchLoggingArguments} --password {this.SuperUserPassword}";

            string serverIp = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? "localhost";

            this.sysbenchPrepareArguments += $" --host \"{serverIp}\"";

            string arguments = $"{this.SysbenchPackagePath}/populate-database.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                SysbenchExecutor.PythonCommand,
                arguments + this.sysbenchPrepareArguments,
                this.SysbenchPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);

                    this.AddMetric(this.sysbenchLoggingArguments, process, telemetryContext, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Returns MySQL Version.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> GetMySQLVersionAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            { 
                IProcessProxy mysqlversionprocess = await this.ExecuteCommandAsync("sudo", $"mysql -u {this.DatabaseName} -h {this.ServerIpAddress} -e \"SELECT VERSION();\"", Environment.CurrentDirectory, telemetryContext, cancellationToken);
                string mysqlVersion = mysqlversionprocess.StandardOutput.ToString();

                Regex regex = new Regex(@"(\d+\.\d+\.\d+)");
                Match match = regex.Match(mysqlVersion);

                return match.Success ? match.Groups[1].Value : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
