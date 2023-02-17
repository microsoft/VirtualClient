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
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.LatteExecutor2;
    using static VirtualClient.Actions.NTttcpExecutor2;

    /// <summary>
    /// Executes the client side of SockPerf.
    /// </summary>
    public class SockPerfClientExecutor2 : SockPerfExecutor2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfClientExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfClientExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ServerOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.StateConfirmationPollingTimeout = TimeSpan.FromMinutes(5);
            this.ClientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);

            this.Parameters.SetIfNotDefined(nameof(this.Port), 6100);
            this.Parameters.SetIfNotDefined(nameof(this.MessagesPerSecond), "max");
            this.Parameters.SetIfNotDefined(nameof(this.ConfidenceLevel), 99);
        }

        /// <summary>
        /// Parameter defines the port used by first thread of the tool.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SockPerfClientExecutor2.Port), 5001);
            }

            set
            {
                this.Parameters[nameof(SockPerfClientExecutor2.Port)] = value;
            }
        }

        /// <summary>
        /// Get message size value in bytes.
        /// </summary>
        public int MessageSize
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MessageSize));
            }
        }

        /// <summary>
        /// Get test run duration value in seconds.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration));
            }
        }

        /// <summary>
        /// Get test mode value (ping-pong or under-load)
        /// </summary>
        public string TestMode
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TestMode)).ToString();
            }
        }

        /// <summary>
        /// Parameter defines the warmup time to use in the workload toolset tests.
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SockPerfClientExecutor2.WarmupTime), 8);
            }

            set
            {
                this.Parameters[nameof(SockPerfClientExecutor2.WarmupTime)] = value;
            }
        }

        /// <summary>
        /// get messages-per-second value
        /// </summary>
        public string MessagesPerSecond
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.MessagesPerSecond), "max").ToString();
            }
        }

        /// <summary>
        /// gets test delay time values in seconds.
        /// </summary>
        public int DelayTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DelayTime));
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

        private async Task ResetServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var resetInstructions = new Instructions(InstructionsType.ClientServerReset, new Dictionary<string, IConvertible>
            {
                ["Type"] = typeof(SockPerfServerExecutor2).Name
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
            await this.ServerApiClient.PollForStateDeletedAsync(nameof(SockPerfWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
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
                    ["Type"] = typeof(SockPerfServerExecutor2).Name
                });

                foreach (var paramter in this.Parameters)
                {
                    startInstructions.Properties.Add(paramter);
                }

                Item<Instructions> instructions = new Item<Instructions>(
                    nameof(Instructions),
                    startInstructions);

                this.Logger.LogTraceMessage($"Synchronization: Request server to start {this.Tool} workload...");

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                await this.ServerApiClient.PollForExpectedStateAsync<SockPerfWorkloadState>(
                    nameof(SockPerfWorkloadState), (state) => state.Status == ClientServerStatus.ExecutionStarted, this.StateConfirmationPollingTimeout, cancellationToken, this.Logger)
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
                    await this.ServerApiClient.PollForStateDeletedAsync(nameof(SockPerfWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
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
            await this.WaitForResultsAsync(TimeSpan.FromMinutes(4), telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogMetricsAsync(commandArguments, startTime, endTime, telemetryContext)
                    .ConfigureAwait(false);
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
                                    if (process.IsErrored())
                                    {
                                        await this.LogProcessDetailsAsync(process, relatedContext, "SockPerf", logToFile: true);
                                        process.ThrowIfWorkloadFailed();
                                    }

                                    await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), relatedContext, cancellationToken);

                                    string results = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath);
                                    await this.LogProcessDetailsAsync(process, relatedContext, "SockPerf", results: results.AsArray(), logToFile: true);
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

        private async Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IFile fileAccess = this.SystemManager.FileSystem.File;
            string resultsContent = null;
            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < pollingTimeout && !cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine(this.ResultsPath);

                if (fileAccess.Exists(this.ResultsPath))
                {
                    try
                    {
                        resultsContent = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                            .ConfigureAwait(false);

                        // Console.WriteLine(resultsContent);

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

                await this.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(resultsContent))
            {
                throw new WorkloadResultsException(
                    $"Results not found. The workload '{this.ExecutablePath}' did not produce any valid results.",
                    ErrorReason.WorkloadFailed);
            }
        }

        private string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string protocolParam = this.Protocol.ToString().ToLowerInvariant() == "tcp" ? "--tcp" : string.Empty;

            // sockperf under-load -i 10.0.1.1 -p 8201 -t 60 --pps=max --full-rtt --msg-size 64 --client_ip 10.0.1.0
            return $"{this.TestMode} " +
                $"-i {serverIPAddress} " +
                $"-p {this.Port} {protocolParam} " +
                $"-t {this.TestDuration} " +
                $"{(this.MessagesPerSecond.ToLowerInvariant() == "max" ? "--mps=max" : $"--mps {this.MessagesPerSecond}")} " +
                $"--full-rtt --msg-size {this.MessageSize} " +
                $"--client_ip {clientIPAddress} " +
                $"--full-log {this.ResultsPath}";
        }

        private async Task LogMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            IFile fileAccess = this.SystemManager.FileSystem.File;

            if (fileAccess.Exists(this.ResultsPath))
            {
                string resultsContent = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultsContent))
                {
                    MetricsParser parser = new SockPerfMetricsParser2(resultsContent, this.ConfidenceLevel);
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
                        EventContext.Persisted());
                }
            }
        }
    }
}
