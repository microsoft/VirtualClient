// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// SockPerf Server Executor.
    /// </summary>
    public class SockPerfServerExecutor2 : SockPerfExecutor2
    {
        private static readonly object LockObject = new object();
        private BackgroundWorkloadServer backgroundWorkloadServer;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfServerExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfServerExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// Parameter defines the port used by first thread of the tool.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 5001);
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
        /// get test run duration value in seconds.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration));
            }
        }

        /// <summary>
        /// get test mode value (ping-pong or under-load)
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
                return this.Parameters.GetValue<int>(nameof(this.WarmupTime), 8);
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
        /// The type of the instructions that are passed to server.(e.g. ClientServerReset)
        /// </summary>
        public InstructionsType TypeOfInstructions
        {
            get
            {
                return (InstructionsType)Enum.Parse(typeof(InstructionsType), this.Parameters.GetValue<string>(nameof(this.TypeOfInstructions)), true);
            }
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServer", telemetryContext, async () =>
            {
                // The current model uses an event handler to subscribe to events that are processed by the 
                // Events API. Event handlers have a signature that is may be too strict to 
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    await this.ExecuteServerBasedOnInstructionsAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Checks if process is running.
        /// </summary>
        /// <param name="processName">The name of process.</param>
        /// <returns></returns>
        protected virtual bool IsProcessRunning(string processName)
        {
            bool success = false;
            List<Process> processlist = new List<Process>(Process.GetProcesses());

            if (processlist.Where(
                process => string.Equals(
                process.ProcessName,
                processName,
                StringComparison.OrdinalIgnoreCase)).Count() != 0)
            {
                success = true;
            }

            return success;

        }

        /// <summary>
        /// Executes based on instructions from client.  
        /// </summary>
        protected Task ExecuteServerBasedOnInstructionsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            lock (SockPerfServerExecutor2.LockObject)
            {
                try
                {
                    return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServerBasedOnInstructions", telemetryContext, async () =>
                    {
                        EventContext relatedContext = EventContext.Persisted();

                        if (this.TypeOfInstructions == InstructionsType.ClientServerReset)
                        {
                            this.Logger.LogTraceMessage($"Synchronization: Stopping all workloads...");

                            this.StopServerTool(telemetryContext, cancellationToken);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            this.Logger.LogTraceMessage($"Synchronization: Workloads Stopped.");
                        }
                        else if (this.TypeOfInstructions == InstructionsType.ClientServerStartExecution)
                        {
                            this.Logger.LogTraceMessage($"Synchronization: Starting {this.Tool} workload...");

                            SockPerfWorkloadState serverState = new SockPerfWorkloadState(ClientServerStatus.Ready);
                            Item<SockPerfWorkloadState> serverStateInstance = new Item<SockPerfWorkloadState>(nameof(SockPerfWorkloadState), serverState);

                            this.StopServerTool(telemetryContext, cancellationToken);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            this.LocalApiClient.CreateStateAsync<SockPerfWorkloadState>(
                                    nameof(SockPerfWorkloadState),
                                    serverState,
                                    cancellationToken).GetAwaiter().GetResult();

                            this.StartServerTool(this.Tool.ToString(), telemetryContext, cancellationToken);
                        }

                        await Task.Delay(500).ConfigureAwait(false);
                    });
                }
                catch
                {
                    // We should not surface exceptions that cause the eventing system
                    // issues.
                }
            }

            return Task.CompletedTask;
        }

        private void StartServerTool(string toolName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                this.Logger.LogMessage($"{this.TypeName}.StartServerWorkload", telemetryContext, () =>
                {
                    CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    this.backgroundWorkloadServer = new BackgroundWorkloadServer
                    {
                        Name = toolName,
                        CancellationSource = cancellationSource,
                        BackgroundTask = this.ExecuteServerAsync(telemetryContext, cancellationSource.Token)
                    };
                });
            }
            catch
            {
                // Do not crash the application
            }
        }

        private Task ExecuteServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The server-side logic to execute the server workload CANNOT block. It must return immediately.
            return Task.Run(async () =>
            {
                Item<SockPerfWorkloadState> state = await this.LocalApiClient.GetStateAsync<SockPerfWorkloadState>(
                    nameof(SockPerfWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (state != null)
                {
                    await this.EnableInboundFirewallAccessAsync(this.ExecutablePath, this.SystemManager, cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        await this.DeleteResultsFileAsync().ConfigureAwait(false);

                        // Note:
                        // We found that certain of the workloads do not exit when they are supposed to. We enforce an
                        // absolute timeout to ensure we do not waste too much time with a workload that is stuck.
                        // Update based on if we want to get it from client
                        TimeSpan workloadTimeout = TimeSpan.FromSeconds(this.WarmupTime + (this.TestDuration * 2));

                        string commandArguments = this.GetCommandLineArguments();

                        this.Logger.LogTraceMessage($"Command: {commandArguments}");

                        DateTime startTime = DateTime.UtcNow;
                        List<Task> workloadTasks = new List<Task>
                        {
                            this.ConfirmProcessRunningAsync(state, telemetryContext, cancellationToken),
                            this.ExecuteWorkloadAsync(commandArguments, workloadTimeout, telemetryContext, cancellationToken)
                        };

                        await Task.WhenAll(workloadTasks).ConfigureAwait(false);
                        DateTime endTime = DateTime.UtcNow;
                    }
                    finally
                    {
                        await this.LocalApiClient.DeleteStateAsync(nameof(SockPerfWorkloadState), cancellationToken)
                            .ConfigureAwait(false);

                        this.ServerCancellationSource?.Cancel();
                        this.ServerCancellationSource?.Dispose();
                    }
                }
            });
        }

        private Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ResetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(
                    nameof(SockPerfWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private void StopServerTool(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.backgroundWorkloadServer != null)
            {
                this.Logger.LogMessage($"{this.TypeName}.StopServerTool", telemetryContext, () =>
                {
                    Console.WriteLine($"[{DateTime.Now}] {this.backgroundWorkloadServer.Name}: Stopping...");
                    this.backgroundWorkloadServer.CancellationSource?.Cancel();
                    this.backgroundWorkloadServer.BackgroundTask.GetAwaiter().GetResult();
                    this.backgroundWorkloadServer.BackgroundTask.Dispose();
                });
            }
        }

        private async Task ConfirmProcessRunningAsync(Item<SockPerfWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                DateTime timeout = DateTime.UtcNow.AddMinutes(2);

                while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
                {
                    if (this.IsProcessRunning(this.ProcessName))
                    {
                        state.Definition.Status = ClientServerStatus.ExecutionStarted;
                        await this.LocalApiClient.UpdateStateAsync<SockPerfWorkloadState>(
                            nameof(SockPerfWorkloadState),
                            state,
                            cancellationToken).ConfigureAwait(false);

                        this.Logger.LogTraceMessage($"Synchronization: {this.Tool} workload confirmed running...", telemetryContext);
                        break;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Task.Delay options are cancelled.
            }
        }

        private Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IProcessProxy process = null;

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext, async () =>
            {
                using (BackgroundProfiling profiling = BackgroundProfiling.Begin(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        using (process = this.SystemManager.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                if (!process.Start())
                                {
                                    // ************** Server will throw 137 sometimes
                                    // PORT =  8201 # TCP sockperf: ERROR: Message received was larger than expected, message ignored. 
                                    // ************** Need investigation
                                    List<int> successCodes = new List<int>() { 0, 137 };
                                    process.ThrowIfErrored<WorkloadException>(successCodes, errorReason: ErrorReason.WorkloadFailed);
                                }

                                // Run the server slightly longer than the test duration.
                                TimeSpan serverWaitTime = TimeSpan.FromSeconds(this.TestDuration + 10);
                                await this.WaitAsync(serverWaitTime, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, telemetryContext.AddError(exc));
                                process.SafeKill();
                                throw;
                            }
                            finally
                            {
                                this.Logger.LogProcessDetails<SockPerfServerExecutor2>(process, telemetryContext);
                                this.CleanupTasks.Add(() => process.SafeKill());
                            }
                        }
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        private string GetCommandLineArguments()
        {
            // sockperf server -i 10.0.1.1 -p 8201 --tcp
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().PrivateIPAddress;
            string protocolParam = this.Protocol.ToString().ToLowerInvariant() == "tcp" ? "--tcp" : string.Empty;

            return $"server -i {serverIPAddress} -p {this.Port} {protocolParam}".Trim();
        }

        /// <summary>
        /// Used to track and managed the execution of the background server-side workload
        /// process over a long running period of time.
        /// </summary>
        internal class BackgroundWorkloadServer
        {
            /// <summary>
            /// Name of background task
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Background Task
            /// </summary>
            public Task BackgroundTask { get; set; }

            /// <summary>
            /// Cancellation Token
            /// </summary>
            public CancellationTokenSource CancellationSource { get; set; }
        }
    }
}
