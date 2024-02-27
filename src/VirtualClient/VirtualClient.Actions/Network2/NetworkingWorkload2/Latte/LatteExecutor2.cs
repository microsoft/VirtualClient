// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Latte Executor
    /// </summary>
    [WindowsCompatible]
    public class LatteExecutor2 : VirtualClientComponent
    {
        private const string OutputFileName = "latte-results.txt";
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="LatteExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();

            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
                .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        public IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the target self-hosted instance of the
        /// Virtual Client API (i.e. the server-side instance).
        /// </summary>
        public IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        public ISystemManagement SystemManager
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        /// Parameter defines the communication protocol (UDP, TCP) to use in the workload.
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                return (ProtocolType)Enum.Parse(typeof(ProtocolType), this.Parameters.GetValue<string>(nameof(this.Protocol)), true);
            }

            set
            {
                this.Parameters[nameof(LatteClientExecutor2.Protocol)] = value;
            }
        }

        /// <summary>
        ///  Name of the tool (NTttcp,CPS,Latte,SockPerf).
        /// </summary>
        protected string Tool { get; set; }

        /// <summary>
        /// Process name of the tool.
        /// </summary>
        protected string ProcessName { get; set; }

        /// <summary>
        /// Name of the scenario.
        /// </summary>
        protected string Name { get; set; }

        /// <summary>
        /// Tool executable path.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the metrics/results.
        /// </summary>
        protected string ResultsPath { get; set; }

        /// <summary>
        /// The retry policy to apply to the startup of the Latte workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Intialize Latte.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Protocol != ProtocolType.Tcp && this.Protocol != ProtocolType.Udp)
            {
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the Latte workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{nameof(LatteExecutor2)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            ClientInstance clientInstance = this.GetLayoutClientInstance(this.AgentId);
            string layoutIPAddress = clientInstance.IPAddress;

            this.Logger.LogTraceMessage($"Layout-Defined IP Address: {layoutIPAddress}");
            this.Logger.LogTraceMessage($"Layout-Defined Role: {clientInstance.Role}");

            this.ThrowIfLayoutNotDefined();
            this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("package", workloadPackage);

            string role = clientInstance.Role;
            this.InitializeApiClients();

            // e.g.
            // Latte_TCP Client
            this.Name = $"{this.Scenario} {role}";
            this.ProcessName = "latte";
            this.Tool = "Latte";
            string resultsDir = this.PlatformSpecifics.Combine(workloadPackage.Path, this.Scenario);
            this.fileSystem.Directory.CreateDirectory(resultsDir);

            this.ResultsPath = this.PlatformSpecifics.Combine(resultsDir, LatteExecutor2.OutputFileName);
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "latte.exe");
        }

        /// <summary>
        /// Executes the Latte workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.IsInRole(ClientRole.Client) && !this.IsInRole(ClientRole.Server))
            {
                throw new NotSupportedException($"The role is not supported for {this.TypeName}." +
                    $" Environment layout should contain only {ClientRole.Client} or {ClientRole.Server} as roles");
            }

            using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    CancellationToken serverCancellationToken = this.ServerCancellationSource.Token;

                    if (this.IsInRole(ClientRole.Server))
                    {
                        using (var serverExecutor = this.CreateWorkloadServer())
                        {
                            await serverExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(LatteExecutor2)}.ServerExecutionCompleted", telemetryContext);
                        }
                    }
                    else if (this.IsInRole(ClientRole.Client))
                    {
                        using (var clientExecutor = this.CreateWorkloadClient())
                        {
                            await clientExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(LatteExecutor2)}.ClientExecutionCompleted", telemetryContext);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    this.Logger.LogMessage($"{nameof(LatteExecutor2)}.Canceled", telemetryContext);
                }
            }
        }

        /// <summary>
        /// Get new CPS client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadClient()
        {
            return new LatteClientExecutor2(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Get new Networking workload server instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadServer()
        {
            return new NetworkingWorkloadProxy(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Enable the firewall rule for the tool executable.
        /// </summary>
        protected async Task EnableInboundFirewallAccessAsync(string exePath, ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            if (exePath != null)
            {
                FirewallEntry firewallEntry = new FirewallEntry(
                    $"Virtual Client: Allow {exePath}",
                    "Allows client and server instances of the Virtual Client to communicate via the self-hosted API service.",
                    exePath);

                await systemManagement.FirewallManager.EnableInboundAppAsync(firewallEntry, cancellationToken)
                    .ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Delete Results File
        /// </summary>
        /// <returns></returns>
        protected async Task DeleteResultsFileAsync()
        {
            if (this.SystemManager.FileSystem.File.Exists(this.ResultsPath))
            {
                await this.SystemManager.FileSystem.File.DeleteAsync(this.ResultsPath)
                    .ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Returns true/false whether the workload should execute on the system/platform.
        /// </summary>
        /// <returns></returns>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Win32NT;

            return isSupported;
        }

        private void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            this.LocalApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);

            if (this.IsInRole(ClientRole.Client))
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // It is important that we reuse the API client. The HttpClient created underneath will need to use a
                // new connection from the connection pool typically for each instance created. Especially for the case with
                // this workload that is testing network resources, we need to be very cognizant of our usage of TCP connections.
                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverInstance.IPAddress, serverIPAddress);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        /// <summary>
        /// Latte State Class.
        /// </summary>
        internal class LatteWorkloadState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LatteWorkloadState"/> class.
            /// </summary>
            public LatteWorkloadState(ClientServerStatus status, IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
                this.Status = status;
            }

            /// <summary>
            /// An identifier for the status of state (e.g. ClientServerReset).
            /// </summary>
            [JsonProperty(PropertyName = "status", Required = Required.Always)]
            [System.Text.Json.Serialization.JsonConverter(typeof(StringEnumConverter))]
            public ClientServerStatus Status { get; set; }

        }
    }
}
