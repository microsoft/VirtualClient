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
    using Newtonsoft.Json.Linq;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Server side execution
    /// </summary>
    public class CPSServerExecutor2 : CPSExecutor2
    {
        private static readonly object LockObject = new object();
        private BackgroundWorkloadServer backgroundWorkloadServer;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPSServerExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSServerExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// Parameter defines the maximum pending requests per thread.
        /// </summary>
        public int MaxPendingRequestsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MaxPendingRequestsPerThread), 100);
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
        /// Parameter defines the test duration value for the test in seconds.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration), 60);
            }
        }

        /// <summary>
        /// Parameter defines the test warmup time values in seconds.
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.WarmupTime), 8);
            }
        }

        /// <summary>
        /// Parameter defines the number of connections to use in the workload toolset tests.
        /// </summary>
        public int Connections
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Connections), 8);
            }
        }

        /// <summary>
        /// Parameter defines the port used by first thread of the tool.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 7201);
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
                return this.Parameters.GetValue<int>(nameof(this.ConnectionsPerThread), 100);
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
        /// The data transfer mode for each connection
        /// </summary>
        public int DataTransferMode
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DataTransferMode), 1);
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
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
            {
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
        /// Executes based on instructions from client.  
        /// </summary>
        protected Task ExecuteServerBasedOnInstructionsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            lock (CPSServerExecutor2.LockObject)
            {
                try
                {
                    return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServerBasedOnInstructions", telemetryContext, async () =>
                    {
                        EventContext relatedContext = EventContext.Persisted();

                        if (this.TypeOfInstructions == InstructionsType.ClientServerReset)
                        {
                            this.Logger.LogTraceMessage($"Synchronization: Stopping all workloads...");

                            this.StopServerTool(telemetryContext);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            this.Logger.LogTraceMessage($"Synchronization: Workloads Stopped.");
                        }
                        else if (this.TypeOfInstructions == InstructionsType.ClientServerStartExecution)
                        {
                            this.Logger.LogTraceMessage($"Synchronization: Starting {this.Tool} workload...");

                            CPSWorkloadState serverState = new CPSWorkloadState(ClientServerStatus.Ready);
                            Item<CPSWorkloadState> serverStateInstance = new Item<CPSWorkloadState>(nameof(CPSWorkloadState), serverState);

                            this.StopServerTool(telemetryContext);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            this.LocalApiClient.CreateStateAsync<CPSWorkloadState>(
                                    nameof(CPSWorkloadState),
                                    serverState,
                                    cancellationToken).GetAwaiter().GetResult();

                            this.StartServerTool(this.Tool.ToString(), relatedContext, cancellationToken);

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
                Item<CPSWorkloadState> state = await this.LocalApiClient.GetStateAsync<CPSWorkloadState>(
                    nameof(CPSWorkloadState),
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
                        DateTime startTime = DateTime.UtcNow;
                        List<Task> workloadTasks = new List<Task>
                        {
                            this.ConfirmProcessRunningAsync(state, telemetryContext, cancellationToken),
                            this.ExecuteWorkloadAsync(commandArguments, workloadTimeout, telemetryContext, cancellationToken)
                        };

                        await Task.WhenAll(workloadTasks).ConfigureAwait(false);
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
                    finally
                    {
                        await this.LocalApiClient.DeleteStateAsync(nameof(CPSWorkloadState), cancellationToken)
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
                    nameof(CPSWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private void StopServerTool(EventContext telemetryContext)
        {
            try
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
            catch
            {
                // Do not crash the application
            }
        }

        private string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"-s -r {this.Connections} {serverIPAddress},{this.Port} -i {this.DisplayInterval} -wt {this.WarmupTime} -t {this.TestDuration} " +
                $"{((this.DelayTime != 0) ? $"-ds {this.DelayTime}" : string.Empty)} ";
        }

        private async Task ConfirmProcessRunningAsync(Item<CPSWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                DateTime timeout = DateTime.UtcNow.AddMinutes(2);

                while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
                {
                    if (this.IsProcessRunning(this.ProcessName))
                    {
                        state.Definition.Status = ClientServerStatus.ExecutionStarted;
                        await this.LocalApiClient.UpdateStateAsync<CPSWorkloadState>(
                            nameof(CPSWorkloadState),
                            state,
                            cancellationToken).ConfigureAwait(false);

                        this.Logger.LogTraceMessage($"Synchronization: CPS workload confirmed running...", telemetryContext);
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
