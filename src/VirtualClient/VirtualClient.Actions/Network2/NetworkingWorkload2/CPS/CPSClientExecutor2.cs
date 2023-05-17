// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.LatteExecutor2;

    /// <summary>
    /// Executes client side of CPS.
    /// </summary>
    public class CPSClientExecutor2 : CPSExecutor2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CPSClientExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSClientExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ServerOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.StateConfirmationPollingTimeout = TimeSpan.FromMinutes(5);
            this.ClientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);

            this.Parameters.SetIfNotDefined(nameof(this.ConnectionDuration), 0);
            this.Parameters.SetIfNotDefined(nameof(this.DataTransferMode), 1);
            this.Parameters.SetIfNotDefined(nameof(this.DisplayInterval), 10);
            this.Parameters.SetIfNotDefined(nameof(this.ConnectionsPerThread), 100);
            this.Parameters.SetIfNotDefined(nameof(this.MaxPendingRequestsPerThread), 100);
            this.Parameters.SetIfNotDefined(nameof(this.Port), 7201);
            this.Parameters.SetIfNotDefined(nameof(this.WarmupTime), 8);
            this.Parameters.SetIfNotDefined(nameof(this.DelayTime), 0);
            this.Parameters.SetIfNotDefined(nameof(this.ConfidenceLevel), 99);
        }

        /// <summary>
        /// Parameter defines the maximum pending requests per thread.
        /// </summary>
        public int MaxPendingRequestsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.MaxPendingRequestsPerThread), 100);
            }
        }

        /// <summary>
        /// Parameter defines the test duration value for the test in seconds.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.TestDuration), 60);
            }

            set
            {
                this.Parameters[nameof(CPSClientExecutor2.TestDuration)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the test warmup time values in seconds.
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.WarmupTime), 8);
            }

            set
            {
                this.Parameters[nameof(CPSClientExecutor2.WarmupTime)] = value;
            }
        }

        /// <summary>
        /// gets the confidence level used for calculating the confidence intervals.
        /// </summary>
        public double ConfidenceLevel
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConfidenceLevel), 99);
            }
        }

        /// <summary>
        /// Parameter defines the number of connections to use in the workload toolset tests.
        /// </summary>
        public int Connections
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.Connections), 8);
            }

            set
            {
                this.Parameters[nameof(CPSClientExecutor2.Connections)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the port used by first thread of the tool.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.Port), 7201);
            }

            set
            {
                this.Parameters[nameof(CPSClientExecutor2.Port)] = value;
            }
        }

        /// <summary>
        /// ConnectionsPerThread is only for client/sender role.
        /// Parameter defines the number of connections in each sender thread.
        /// </summary>
        public int ConnectionsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.ConnectionsPerThread), 100);
            }
        }

        /// <summary>
        /// Parameter defines duration (in seconds) for each connection.
        /// </summary>
        public int ConnectionDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConnectionDuration), 0);
            }
        }

        /// <summary>
        /// gets test delay time values in seconds.
        /// </summary>
        public int DelayTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DelayTime), 0);
            }
        }

        /// <summary>
        /// The data transfer mode for each connection
        /// </summary>
        public int DataTransferMode
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSClientExecutor2.DataTransferMode), 1);
            }
        }

        /// <summary>
        /// Parameter defines the timeout to use when polling the server-side API for state changes.
        /// </summary>
        protected TimeSpan StateConfirmationPollingTimeout { get; set; }

        /// <summary>
        /// Parameter defines the timeout to use when confirming the server is online.
        /// </summary>
        protected TimeSpan ServerOnlinePollingTimeout { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        private IAsyncPolicy ClientExecutionRetryPolicy { get; set; }

        /// <summary>
        /// Executes client side of workload
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
            {
                await this.ClientExecutionRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.ExecuteClientWorkloadAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (!string.IsNullOrWhiteSpace(this.Results))
            {
                MetricsParser parser = new CPSMetricsParser2(this.Results, this.ConfidenceLevel, this.WarmupTime);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    this.Tool.ToString(),
                    this.Name,
                    startTime,
                    endTime,
                    metrics,
                    string.Empty,
                    commandArguments,
                    this.Tags,
                    telemetryContext,
                    this.Results);
            }
            else
            {
                throw new WorkloadException($"Workload results missing. The CPS workload did not produce any valid results.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates the parameters provided to the executor.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (this.TestDuration <= 0)
            {
                throw new WorkloadException($"'{nameof(this.TestDuration)}' cannot be less than or equal to zero.", ErrorReason.InstructionsNotValid);
            }
            else if (this.WarmupTime >= this.TestDuration)
            {
                throw new WorkloadException($"'{nameof(this.WarmupTime)}' must be less than '{nameof(this.TestDuration)}'.", ErrorReason.InstructionsNotValid);
            }
            else if (this.DelayTime >= this.TestDuration)
            {
                throw new WorkloadException($"'{nameof(this.DelayTime)}' must be less than '{nameof(this.TestDuration)}'.", ErrorReason.InstructionsNotValid);
            }
            else if ((this.DelayTime + this.WarmupTime) >= this.TestDuration)
            {
                throw new WorkloadException($"The sum of the time ranges for '{nameof(this.DelayTime)}' and '{nameof(this.WarmupTime)}' time must be less than '{nameof(this.TestDuration)}',", ErrorReason.InstructionsNotValid);
            }
        }

        private async Task ResetServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var resetInstructions = new Instructions(InstructionsType.ClientServerReset, new Dictionary<string, IConvertible>
            {
                ["Type"] = typeof(CPSServerExecutor2).Name
            });

            foreach (var paramter in this.Parameters)
            {
                resetInstructions.Properties.Add(paramter);
            }

            Item<Instructions> instructions = new Item<Instructions>(nameof(Instructions), resetInstructions);

            await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                .ConfigureAwait(false);

            // Confirm the server has stopped all workloads
            await this.ServerApiClient.PollForStateDeletedAsync(nameof(CPSWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
        }

        private Task ExecuteClientWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteClientWorkload", telemetryContext, async () =>
            {
                this.Logger.LogTraceMessage("Synchronization: Wait for server online...");

                // 1) Confirm server is online.
                // ===========================================================================
                await this.ServerApiClient.PollForHeartbeatAsync(this.ServerOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait(false);

                // 2) Wait for the server to signal the eventing API is online.
                // ===========================================================================
                await this.ServerApiClient.PollForServerOnlineAsync(this.ServerOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait(false);

                this.Logger.LogTraceMessage($"{nameof(CPSClientExecutor2)}.ServerOnline");

                // 3) Request the server to stop ALL workload processes (Reset)
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Request server to stop all workloads...");

                await this.ResetServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                // 4) Request the server to start the next workload.
                // ===========================================================================

                var startInstructions = new Instructions(InstructionsType.ClientServerStartExecution, new Dictionary<string, IConvertible>
                {
                    ["Type"] = typeof(CPSServerExecutor2).Name
                });

                foreach (var paramter in this.Parameters)
                {
                    startInstructions.Properties.Add(paramter);
                }

                Item<Instructions> instructions = new Item<Instructions>(
                    nameof(Instructions),
                    startInstructions);

                this.Logger.LogTraceMessage($"Synchronization: Request server to start CPS workload...");

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                await this.ServerApiClient.PollForExpectedStateAsync<CPSWorkloadState>(
                    nameof(CPSWorkloadState), (state) => state.Status == ClientServerStatus.ExecutionStarted, this.StateConfirmationPollingTimeout, cancellationToken, logger: this.Logger)
                    .ConfigureAwait(false);

                this.Logger.LogTraceMessage("Synchronization: Server workload startup confirmed...");
                this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                // 6) Execute the client workload.
                // ===========================================================================
                try
                {
                    await this.ExecuteClientAsync(telemetryContext, cancellationToken);
                }
                finally
                {
                    await this.DeleteResultsFileAsync().ConfigureAwait(false);

                    this.Logger.LogTraceMessage("Synchronization: Wait for server to stop workload...");
                    await this.ServerApiClient.PollForStateDeletedAsync(nameof(CPSWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
                }
            });
        }

        private async Task ExecuteClientAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EnableInboundFirewallAccessAsync(this.ExecutablePath, this.SystemManager, cancellationToken)
                .ConfigureAwait(false);

            // Note:
            // We found that certain of the workloads do not exit when they are supposed to. We enforce an
            // absolute timeout to ensure we do not waste too much time with a workload that is stuck.
            TimeSpan workloadTimeout = TimeSpan.FromSeconds(this.WarmupTime + (this.TestDuration * 2));

            string commandArguments = this.GetCommandLineArguments();
            DateTime startTime = DateTime.UtcNow;

            await this.ExecuteWorkloadAsync(commandArguments, workloadTimeout, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;

            // There is sometimes a delay in the output of the results to the results
            // file. We will poll for the results for a period of time before giving up.
            // await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), telemetryContext, cancellationToken)
               // .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.CaptureMetricsAsync(commandArguments, startTime, endTime, telemetryContext)
                    .ConfigureAwait(false);
            }

        }

        private string GetCommandLineArguments()
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            return $"-c -r {this.Connections} " +
                $"{clientIPAddress},0,{serverIPAddress},{this.Port},{this.ConnectionsPerThread},{this.MaxPendingRequestsPerThread},{this.ConnectionDuration},{this.DataTransferMode} " +
                $"-i {this.DisplayInterval} -wt {this.WarmupTime} -t {this.TestDuration} " +
                $"{((this.DelayTime != 0) ? $"-ds {this.DelayTime}" : string.Empty)}".Trim();
        }

        private class CPSWorkloadStateEqualityComparer : IEqualityComparer<JObject>
        {
            private CPSWorkloadStateEqualityComparer()
            {
            }

            public static CPSWorkloadStateEqualityComparer Instance { get; } = new CPSWorkloadStateEqualityComparer();

            public bool Equals(JObject x, JObject y)
            {
                CPSWorkloadState xState = x.ToObject<CPSWorkloadState>();
                CPSWorkloadState yState = y.ToObject<CPSWorkloadState>();

                return string.Equals(xState.Status.ToString(), yState.Status.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(JObject obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
