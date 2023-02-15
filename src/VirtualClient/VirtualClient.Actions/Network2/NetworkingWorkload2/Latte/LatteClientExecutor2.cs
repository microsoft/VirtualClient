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
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes the client side of Latte
    /// </summary>
    public class LatteClientExecutor2 : LatteExecutor2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatteClientExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteClientExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ServerOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.StateConfirmationPollingTimeout = TimeSpan.FromMinutes(5);
            this.ClientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
        }

        /// <summary>
        /// The number of times to poll before waiting on RIO CQ.
        /// </summary>
        public int RioPoll
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RioPoll), 100000);
            }

        }

        /// <summary>
        /// The number of iterations for the network send/receive operations.
        /// </summary>
        public int Iterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Iterations), 100100);
            }
        }

        /// <summary>
        /// Parameter defines the test duration to use in the execution of the networking workload
        /// toolset tests.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(LatteClientExecutor2.TestDuration), 60);
            }

            set
            {
                this.Parameters[nameof(LatteClientExecutor2.TestDuration)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the warmup time to use in the workload toolset tests.
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(LatteClientExecutor2.WarmupTime), 8);
            }

            set
            {
                this.Parameters[nameof(LatteClientExecutor2.WarmupTime)] = value;
            }
        }

        /// <summary>
        /// The starting port for the range of ports that will be used for client/server 
        /// network connections.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 6100);
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
        /// Executes client side of the workload.
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
        /// Resets server to startover.
        /// </summary>
        private async Task ResetServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var resetInstructions = new Instructions(InstructionsType.ClientServerReset, new Dictionary<string, IConvertible>
            {
                ["Type"] = typeof(LatteServerExecutor2).Name
            });

            foreach (var paramter in this.Parameters)
            {
                resetInstructions.Properties.Add(paramter);
            }

            Item<Instructions> instructions = new Item<Instructions>(
                nameof(Instructions),
                resetInstructions);

            await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                .ConfigureAwait(false);

            // Confirm the server has stopped all workloads
            await this.ServerApiClient.PollUntilStateIsDeletedAsync(
                nameof(LatteWorkloadState),
                cancellationToken,
                this.StateConfirmationPollingTimeout).ConfigureAwait(false);
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

                this.Logger.LogTraceMessage($"{this.TypeName}.ServerOnline");

                // 3) Request the server to stop ALL workload processes (Reset)
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Request server to stop all workloads...");

                await this.ResetServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                // 4) Request the server to start the next workload.
                // ===========================================================================

                var startInstructions = new Instructions(InstructionsType.ClientServerStartExecution, new Dictionary<string, IConvertible>
                {
                    ["Type"] = typeof(LatteServerExecutor2).Name
                });

                foreach (var paramter in this.Parameters)
                {
                    startInstructions.Properties.Add(paramter);
                }

                Item<Instructions> instructions = new Item<Instructions>(
                    nameof(Instructions),
                    startInstructions);

                this.Logger.LogTraceMessage($"Synchronization: Request server to start Latte workload...");

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                await this.ServerApiClient.PollForExpectedStateAsync<LatteWorkloadState>(
                    nameof(LatteWorkloadState), (state) => state.Status == ClientServerStatus.ExecutionStarted, this.StateConfirmationPollingTimeout, cancellationToken, this.Logger)
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

                    await this.ServerApiClient.PollUntilStateIsDeletedAsync(
                        nameof(LatteWorkloadState),
                        cancellationToken,
                        this.StateConfirmationPollingTimeout).ConfigureAwait(false);
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
            await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.CaptureMetricsAsync(commandArguments, startTime, endTime, telemetryContext)
                    .ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Returns true if results are found in the results file within the polling/timeout
        /// period specified.
        /// </summary>
        private async Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IFile fileAccess = this.SystemManager.FileSystem.File;
            string resultsContent = null;
            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < pollingTimeout && !cancellationToken.IsCancellationRequested)
            {
                if (fileAccess.Exists(this.ResultsPath))
                {
                    try
                    {
                        resultsContent = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(resultsContent))
                        {
                            this.Logger.LogMessage($"{this.TypeName}.WorkloadOutputFileContents", telemetryContext
                                .AddContext("results", resultsContent));

                            break;
                        }
                    }
                    catch (IOException)
                    {
                        // This can be hit if the application is exiting/cancelling while attempting to read
                        // the results file.
                    }
                }

                await this.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(resultsContent))
            {
                throw new WorkloadResultsException(
                    $"Results not found. The workload '{this.ExecutablePath}' did not produce any valid results.",
                    ErrorReason.WorkloadFailed);
            }
        }

        private Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IProcessProxy process = null;

            EventContext relatedContext = telemetryContext.Clone()
              .AddContext("command", this.ExecutablePath)
              .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        using (process = this.SystemManager.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                this.CleanupTasks.Add(() => process.SafeKill());
                                await process.StartAndWaitAsync(cancellationToken, timeout);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await this.LogProcessDetailsAsync(process, relatedContext, "Latte", logToFile: true);

                                    process.ThrowIfWorkloadFailed();
                                    await this.SystemManager.FileSystem.File.WriteAllTextAsync(this.ResultsPath, process.StandardOutput.ToString());
                                }
                            }
                            catch (TimeoutException exc)
                            {
                                // We give this a best effort but do not want it to prevent the next workload
                                // from executing.
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill();
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill();
                                throw;
                            }
                        }
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        private string GetCommandLineArguments()
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            return $"-so -c -a {serverIPAddress}:{this.Port} -rio -i {this.Iterations} -riopoll {this.RioPoll} -{this.Protocol.ToString().ToLowerInvariant()} " +
            $"-hist -hl 1 -hc 9998 -bl {clientIPAddress}";
        }

        private async Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            IFile fileAccess = this.SystemManager.FileSystem.File;

            if (fileAccess.Exists(this.ResultsPath))
            {
                string resultsContent = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultsContent))
                {
                    MetricsParser parser = new LatteMetricsParser2(resultsContent);
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
                        telemetryContext);
                }
            }
        }
    }
}
