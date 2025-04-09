// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes the client side of NTttcp.
    /// </summary>
    public class NTttcpClientExecutor2 : NTttcpExecutor2
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpClientExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpClientExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ServerOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.StateConfirmationPollingTimeout = TimeSpan.FromMinutes(5);
            this.ClientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);

            this.Parameters.SetIfNotDefined(nameof(this.ThreadCount), 1);
            this.Parameters.SetIfNotDefined(nameof(this.TestDuration), 60);
            this.Parameters.SetIfNotDefined(nameof(this.Port), 5001);
        }

        /// <summary>
        /// get number of concurrent threads to use.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), 1);
            }
        }

        /// <summary>
        /// Parameter defines the network buffer size for client to use in the workload toolset 
        /// tests.
        /// </summary>
        public string BufferSizeClient
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.BufferSizeClient), out IConvertible bufferSizeClient);
                return bufferSizeClient?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.BufferSizeClient)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the network buffer size for server to use in the workload toolset 
        /// tests.
        /// </summary>
        public string BufferSizeServer
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.BufferSizeServer), out IConvertible bufferSizeServer);
                return bufferSizeServer?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.BufferSizeServer)] = value;
            }
        }

        /// <summary>
        /// get test run duration value in seconds.
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
                this.Parameters.TryGetValue(nameof(NTttcpClientExecutor2.ThreadsPerServerPort), out IConvertible threadsPerServerPort);
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
                this.Parameters.TryGetValue(nameof(NTttcpClientExecutor2.ConnectionsPerThread), out IConvertible connectionsPerThread);
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
                return this.Parameters.GetValue<int>(nameof(NTttcpClientExecutor2.WarmupTime), 8);
            }

            set
            {
                this.Parameters[nameof(NTttcpClientExecutor2.WarmupTime)] = value;
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
                this.Parameters.TryGetValue(nameof(NTttcpClientExecutor2.ReceiverMultiClientMode), out IConvertible receiverMultiClientMode);
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
                this.Parameters.TryGetValue(nameof(NTttcpClientExecutor2.SenderLastClient), out IConvertible senderLastClient);
                return senderLastClient?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Parameter defines the timeout to use when polling the server-side API for state changes.
        /// </summary>
        public TimeSpan StateConfirmationPollingTimeout { get; set; }

        /// <summary>
        /// Parameter defines the timeout to use when confirming the server is online.
        /// </summary>
        public TimeSpan ServerOnlinePollingTimeout { get; set; }

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
                ["Type"] = typeof(NTttcpServerExecutor2).Name
            });

            foreach (var paramter in this.Parameters)
            {
                resetInstructions.Properties.Add(paramter);
            }

            Item<Instructions> instructions = new Item<Instructions>(nameof(Instructions), resetInstructions);

            await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                .ConfigureAwait(false);

            this.Logger.LogTraceMessage($"Synchronization: Wait for server online...");

            // Confirm the server has stopped all workloads
            await this.ServerApiClient.PollForStateDeletedAsync(nameof(NTttcpWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
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
                $"-l {this.BufferSizeClient} " +
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
                $"-b {this.BufferSizeClient} " +
                $"-x {this.ResultsPath} " +
                $"-p {this.Port} " +
                $"{(this.Protocol.ToString().ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Client) && this.SenderLastClient == true) ? "-L" : string.Empty)} " +
                $"{(((this.Role == ClientRole.Client) && this.ReceiverMultiClientMode == true) ? "-M" : string.Empty)} " +
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

            return command;
        }

        private Task ExecuteClientWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Guid requestId = Guid.NewGuid();
            telemetryContext.AddClientRequestId(requestId);

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
                this.Logger.LogTraceMessage($"Synchronization: Request server to start NTttcp workload...");

                var startInstructions = new Instructions(InstructionsType.ClientServerStartExecution, new Dictionary<string, IConvertible>
                {
                    ["Type"] = typeof(NTttcpServerExecutor2).Name
                });

                foreach (var paramter in this.Parameters)
                {
                    startInstructions.Properties.Add(paramter);
                }

                Item<Instructions> instructions = new Item<Instructions>(
                    nameof(Instructions),
                    startInstructions);

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                await this.ServerApiClient.PollForExpectedStateAsync<NTttcpWorkloadState>(
                    nameof(NTttcpWorkloadState), (state) => state.Status == ClientServerStatus.ExecutionStarted, this.StateConfirmationPollingTimeout, cancellationToken, logger: this.Logger)
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
                    await this.ServerApiClient.PollForStateDeletedAsync(nameof(NTttcpWorkloadState), this.StateConfirmationPollingTimeout, cancellationToken);
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
    }
}
