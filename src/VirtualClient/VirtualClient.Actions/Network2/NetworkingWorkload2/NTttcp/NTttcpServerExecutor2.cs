// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
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

    /// <summary>
    /// Execute NTttcp server side.
    /// </summary>
    public class NTttcpServerExecutor2 : NTttcpExecutor2
    {
        private static readonly object LockObject = new object();
        private BackgroundWorkloadServer backgroundWorkloadServer;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpServerExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpServerExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// Get number of concurrent threads to use.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), 1);
            }
        }

        /// <summary>
        /// Get buffer size value in bytes for Client.(e.g. 4K,64K,1400)
        ///  Where 4K = 4*1024 = 4096 bytes.
        /// </summary>
        public string BufferSizeClient
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BufferSizeClient)).ToString();
            }
        }

        /// <summary>
        /// Get buffer size value in bytes for Server.(e.g. 4K,64K,1400)
        ///  Where 4K = 4*1024 = 4096 bytes.
        /// </summary>
        public string BufferSizeServer
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BufferSizeServer)).ToString();
            }
        }

        /// <summary>
        /// Get test run duration value in seconds.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration), 60);
            }
        }

        /// <summary>
        /// ThreadsPerServerPort is only for client/sender role.
        /// The ThreadsPerServerPort gets the number of threads per each server port.
        /// </summary>
        public int? ThreadsPerServerPort
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ThreadsPerServerPort), out IConvertible threadsPerServerPort);
                return threadsPerServerPort?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// ConnectionsPerThread is only for client/sender role.
        /// The ConnectionsPerThread gets the number of connections in each sender thread.
        /// </summary>
        public int? ConnectionsPerThread
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ConnectionsPerThread), out IConvertible connectionsPerThread);
                return connectionsPerThread?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// DevInterruptsDifferentiator gets the differentiator.
        /// Used for getting number of interrupts for the devices specified by the differentiator.
        /// Examples for differentiator: Hyper-V PCIe MSI, mlx4, Hypervisor callback interrupts,etc.
        /// </summary>
        public string DevInterruptsDifferentiator
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DevInterruptsDifferentiator), out IConvertible differentiator);
                return differentiator?.ToString();
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
                return this.Parameters.GetValue<int>(nameof(this.Port), 5001);
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

            set
            {
                this.Parameters[nameof(this.WarmupTime)] = value;
            }
        }

        /// <summary>
        /// ReceiverMultiClientMode is only for server/receiver role.
        /// The ReceiverMultiClientMode tells server to work in multi-client mode.
        /// </summary>
        public bool? ReceiverMultiClientMode
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ReceiverMultiClientMode), out IConvertible receiverMultiClientMode);
                return receiverMultiClientMode?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// SenderLastClient is only for client/sender role.
        /// The SenderLastClient indicates that this is the last client when test with multi-client mode.
        /// </summary>
        public bool? SenderLastClient
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.SenderLastClient), out IConvertible senderLastClient);
                return senderLastClient?.ToBoolean(CultureInfo.InvariantCulture);
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
            lock (NTttcpServerExecutor2.LockObject)
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

                            NTttcpWorkloadState serverState = new NTttcpWorkloadState(ClientServerStatus.Ready);
                            Item<NTttcpWorkloadState> serverStateInstance = new Item<NTttcpWorkloadState>(nameof(NTttcpWorkloadState), serverState);

                            this.StopServerTool(telemetryContext, cancellationToken);
                            this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                            HttpResponseMessage response = this.LocalApiClient.CreateStateAsync<NTttcpWorkloadState>(
                                    nameof(NTttcpWorkloadState),
                                    serverState,
                                    cancellationToken).GetAwaiter().GetResult();

                            this.StartServerTool(this.Tool.ToString(), telemetryContext, cancellationToken);
                        }

                        await Task.Delay(5).ConfigureAwait(false);
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
                this.Logger.LogMessage($"{this.TypeName}.StartServerTool", telemetryContext, () =>
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
                Item<NTttcpWorkloadState> state = await this.LocalApiClient.GetStateAsync<NTttcpWorkloadState>(
                    nameof(NTttcpWorkloadState),
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
                    finally
                    {
                        await this.LocalApiClient.DeleteStateAsync(nameof(NTttcpWorkloadState), cancellationToken)
                            .ConfigureAwait(false);

                        this.ServerCancellationSource?.Cancel();
                        this.ServerCancellationSource?.Dispose();
                    }
                }
                else
                {
                    this.Logger.LogTraceMessage("State is null");
                }
            });
        }

        private Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteWorkloadState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(
                    nameof(NTttcpWorkloadState),
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

        private async Task ConfirmProcessRunningAsync(Item<NTttcpWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                DateTime timeout = DateTime.UtcNow.AddMinutes(2);

                while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
                {
                    if (this.IsProcessRunning(this.ProcessName))
                    {
                        state.Definition.Status = ClientServerStatus.ExecutionStarted;
                        await this.LocalApiClient.UpdateStateAsync(
                            nameof(NTttcpWorkloadState),
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

        private string GetWindowsSpecificCommandLine()
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"{((this.Role == ClientRole.Client) ? "-s" : "-r")} " +
                $"-m {this.ThreadCount},*,{serverIPAddress} " +
                $"-wu {NTttcpExecutor2.DefaultWarmupTime.TotalSeconds} " +
                $"-cd {NTttcpExecutor2.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration} " +
                $"-l {this.BufferSizeServer} " +
                $"-p {this.Port} " +
                $"-xml {this.ResultsPath} " +
                $"{(this.Protocol.ToString().ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{((this.Role == ClientRole.Client) ? $"-nic {clientIPAddress}" : string.Empty)}".Trim();
        }

        private string GetLinuxSpecificCommandLine()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"{((this.Role == ClientRole.Client) ? "-s" : "-r")} " +
                $"-V " +
                $"-m {this.ThreadCount},*,{serverIPAddress} " +
                $"-W {NTttcpExecutor2.DefaultWarmupTime.TotalSeconds} " +
                $"-C {NTttcpExecutor2.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration} " +
                $"-b {this.BufferSizeServer} " +
                $"-x {this.ResultsPath} " +
                $"-p {this.Port} " +
                $"{(this.Protocol.ToString().ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Client) && this.SenderLastClient == true) ? "-L" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Server) && this.ReceiverMultiClientMode == true) ? "-M" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Client) && this.ThreadsPerServerPort != null) ? $"-n {this.ThreadsPerServerPort}" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Client) && this.ConnectionsPerThread != null) ? $"-l {this.ConnectionsPerThread}" : string.Empty)} " +
                $"{((this.DevInterruptsDifferentiator != null) ? $"--show-dev-interrupts {this.DevInterruptsDifferentiator}" : string.Empty)}".Trim();
        }

        private string GetCommandLineArguments()
        {
            string command = null;
            if (this.Platform == PlatformID.Win32NT)
            {
                command = this.GetWindowsSpecificCommandLine();
            }
            else if (this.Platform == PlatformID.Unix)
            {
                command = this.GetLinuxSpecificCommandLine();
            }

            this.Logger.LogTraceMessage($"Command: {command}");

            return command;
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
