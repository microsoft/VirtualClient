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
    public class SysbenchClientExecutor : SysbenchExecutor
    {
        private string sysbenchExecutionArguments;
        private string sysbenchLoggingArguments;

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
                    if (this.Benchmark == BenchmarkName.OLTP)
                    {
                        await this.RunOLTPWorkloadAsync(telemetryContext, cancellationToken);
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

            this.sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --benchmark {this.Benchmark} --workload {this.Workload} --threadCount {threadCount} --tableCount {tableCount} --recordCount {recordCount} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--hostIpAddress {this.ServerIpAddress} --durationSecs {this.Duration.TotalSeconds}";

            string command = "python3";
            string script = $"{this.SysbenchPackagePath}/run-workload.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
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

        private async Task RunTPCCWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.Scenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int warehouseCount = GetWarehouseCount(this.DatabaseScenario, this.WarehouseCount);

            this.sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --benchmark {this.Benchmark} --workload tpcc --threadCount {threadCount} --tableCount {tableCount} --warehouses {warehouseCount} ";
            this.sysbenchExecutionArguments = this.sysbenchLoggingArguments + $"--hostIpAddress {this.ServerIpAddress} --durationSecs {this.Duration.TotalSeconds}";

            string command = "python3";
            string script = $"{this.SysbenchPackagePath}/run-workload.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
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
    }
}
