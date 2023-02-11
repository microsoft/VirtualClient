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
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Latte server executor.
    /// </summary>
    [WindowsCompatible]
    public class LatteServerExecutor2 : LatteExecutor2
    {
        private static readonly object LockObject = new object();
        private BackgroundWorkloadServer backgroundWorkloadServer;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LatteServerExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteServerExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
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
        /// Checks if the process exists.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>true if process name exists.</returns>
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
        /// executes based on instructions from client.  
        /// </summary>
        protected Task ExecuteServerBasedOnInstructionsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            lock (LatteServerExecutor2.LockObject)
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

                            LatteWorkloadState serverState = new LatteWorkloadState(ClientServerStatus.Ready);
                            Item<LatteWorkloadState> serverStateInstance = new Item<LatteWorkloadState>(nameof(LatteWorkloadState), serverState);

                            this.StopServerTool(telemetryContext, cancellationToken);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            this.LocalApiClient.CreateStateAsync<LatteWorkloadState>(
                                    nameof(LatteWorkloadState),
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
                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext(nameof(toolName), toolName.ToString());

                this.Logger.LogMessage($"{this.TypeName}.StartServerWorkload", relatedContext, () =>
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
                EventContext relatedContext = telemetryContext.Clone();

                Item<LatteWorkloadState> state = await this.LocalApiClient.GetStateAsync<LatteWorkloadState>(
                    nameof(LatteWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (state != null)
                {
                    relatedContext.AddContext("initialState", state);

                    await this.EnableInboundFirewallAccessAsync(this.ExecutablePath, this.SystemManager, cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        await this.DeleteResultsFileAsync().ConfigureAwait(false);

                        string commandArguments = this.GetCommandLineArguments();
                        this.Logger.LogTraceMessage($"Command: {commandArguments}");

                        DateTime startTime = DateTime.UtcNow;
                        List<Task> workloadTasks = new List<Task>
                        {
                            this.ConfirmProcessRunningAsync(state, relatedContext, cancellationToken),
                            this.ExecuteWorkloadAsync(commandArguments, relatedContext, cancellationToken)
                        };

                        await Task.WhenAll(workloadTasks).ConfigureAwait(false);
                        DateTime endTime = DateTime.UtcNow;
                    }
                    finally
                    {
                        await this.LocalApiClient.DeleteStateAsync(nameof(LatteWorkloadState), cancellationToken)
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
                    nameof(LatteWorkloadState),
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

        private async Task ConfirmProcessRunningAsync(Item<LatteWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                DateTime timeout = DateTime.UtcNow.AddMinutes(2);

                while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
                {
                    if (this.IsProcessRunning(this.ProcessName))
                    {
                        state.Definition.Status = ClientServerStatus.ExecutionStarted;
                        await this.LocalApiClient.UpdateStateAsync<LatteWorkloadState>(
                            nameof(LatteWorkloadState),
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

        private Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
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
                                using (process = this.SystemManager.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                                {
                                    if (!process.Start())
                                    {
                                        await this.LogProcessDetailsAsync(process, relatedContext, "Latte");
                                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                                    }

                                    this.CleanupTasks.Add(() => process.SafeKill());

                                    // Run the server slightly longer than the test duration.
                                    TimeSpan serverWaitTime = TimeSpan.FromMilliseconds(this.Iterations * .5);
                                    await this.WaitAsync(serverWaitTime, cancellationToken);
                                    await this.LogProcessDetailsAsync(process, relatedContext, "Latte", logToFile: true);
                                }
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
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
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"-a {serverIPAddress}:{this.Port} -rio -i {this.Iterations} -riopoll {this.RioPoll} -{this.Protocol.ToString().ToLowerInvariant()}";
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
