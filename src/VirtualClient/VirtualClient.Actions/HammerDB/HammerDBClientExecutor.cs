// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
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
    /// The HammerDB Client workload executor.
    /// </summary>
    public class HammerDBClientExecutor : HammerDBExecutor
    {
        private string hammerDBExecutionArguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDBClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public HammerDBClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// The total time of execution option passed to HammerDB.
        /// </summary>
        public TimeSpan Duration
        {
            get
            { 
                return this.Parameters.GetTimeSpanValue(nameof(HammerDBClientExecutor.Duration), TimeSpan.FromMinutes(5));
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

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            this.hammerDBExecutionArguments = $"--runTransactionsTCLFilePath {this.RunTransactionsTclName}";
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "HammerDB",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string text = process.StandardOutput.ToString();

                if (!string.IsNullOrEmpty(text)) 
                {
                    try
                    {
                        HammerDBMetricsParser parser = new HammerDBMetricsParser(text);
                        IList<Metric> metrics = parser.Parse();

                        this.Logger.LogMetrics(
                            toolName: "HammerDB",
                            scenarioName: this.MetricScenario ?? this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            metrics,
                            null,
                            null,
                            this.Tags,
                            telemetryContext);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadException($"Failed to parse HammerDB output.", exc, ErrorReason.InvalidResults);
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
                    string command = "python3";
                    string script = $"{this.HammerDBPackagePath}/run-workload.py";

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        command, 
                        script + " " + this.hammerDBExecutionArguments, 
                        this.HammerDBPackagePath, 
                        telemetryContext, 
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "HammerDB", logToFile: true);
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);

                            this.CaptureMetrics(process, telemetryContext, cancellationToken);
                        }
                    }
                }
            });
        }
    }
}
