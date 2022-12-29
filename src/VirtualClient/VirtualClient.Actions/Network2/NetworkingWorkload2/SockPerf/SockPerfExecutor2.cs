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
    using Microsoft.Extensions.Logging;
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
    /// SockPerf Executor
    /// </summary>
    [UnixCompatible]
    public class SockPerfExecutor2 : VirtualClientComponent
    {
        private const string OutputFileName = "sockperf-results.txt";
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();

            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
               .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// The type of the protocol that should be used for the workload. (e.g. TCP,UDP)
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                return (ProtocolType)Enum.Parse(typeof(ProtocolType), this.Parameters.GetValue<string>(nameof(this.Protocol)), true);
            }
        }

        /// <summary>
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        protected IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the target self-hosted instance of the
        /// Virtual Client API (i.e. the server-side instance).
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Name of the scenario.
        /// </summary>
        protected string Name { get; set; }

        /// <summary>
        ///  Path to the executable of powershell7.
        /// </summary>
        protected string Tool { get; set; }

        /// <summary>
        /// Tool executable path.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Powershell script path. 
        /// </summary>
        protected string PowerShellScriptPath { get; set; }

        /// <summary>
        /// Path to the metrics/results.
        /// </summary>
        protected string ResultsPath { get; set; }

        /// <summary>
        /// Process name of the tool.
        /// </summary>
        protected string ProcessName { get; set; }

        /// <summary>
        /// The retry policy to apply to the startup of the SockPerf workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Server Cancellation Source
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        protected ISystemManagement SystemManager
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        /// The role of the current Virtual Client instance. Supported roles = Client or Server
        /// </summary>
        protected string Role { get; set; }

        /// <summary>
        /// Intialize SockPerf.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // string protocol = this.Protocol.ToString().ToLowerInvariant();

            if (this.Protocol != ProtocolType.Tcp && this.Protocol != ProtocolType.Udp)
            {
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the SockPerf workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{this.TypeName}' steps.",
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

            this.Role = clientInstance.Role;

            this.InitializeApiClients();

            // e.g.
            // SockPerf_TCP_Ping_Pong Client, SockPerf_TCP_Ping_Pong Server
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "sockperf";
            this.Tool = "SockPerf";

            string resultsDir = this.PlatformSpecifics.Combine(workloadPackage.Path, this.Scenario);
            this.fileSystem.Directory.CreateDirectory(resultsDir);

            this.ResultsPath = this.PlatformSpecifics.Combine(resultsDir, SockPerfClientExecutor2.OutputFileName);
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "sockperf");
            await this.SystemManager.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken)
                .ConfigureAwait(false);

        }

        /// <summary>
        /// Executes the SockPerf workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Role != ClientRole.Client && this.Role != ClientRole.Server)
            {
                throw new NotSupportedException($"The role: {this.Role} is not supported for {this.TypeName}." +
                    $" Environment layout should contain only {ClientRole.Client} or {ClientRole.Server} as roles");
            }

            using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    CancellationToken serverCancellationToken = this.ServerCancellationSource.Token;

                    if (this.Role == ClientRole.Server)
                    {
                        using (var serverExecutor = this.CreateWorkloadServer())
                        {
                            await serverExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{this.TypeName}.ServerExecutionCompleted", telemetryContext);
                        }
                    }
                    else if (this.Role == ClientRole.Client)
                    {
                        using (var clientExecutor = this.CreateWorkloadClient())
                        {
                            await clientExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{this.TypeName}.ClientExecutionCompleted", telemetryContext);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    this.Logger.LogMessage($"{this.TypeName}.Canceled", telemetryContext);
                }
            }
        }

        /// <summary>
        /// Get new CPS client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadClient()
        {
            return new SockPerfClientExecutor2(this.Dependencies, this.Parameters);
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
            bool isSupported = this.Platform == PlatformID.Unix;

            return isSupported;
        }

        private void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            this.LocalApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);

            if (this.Role == ClientRole.Client)
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // It is important that we reuse the API client. The HttpClient created underneath will need to use a
                // new connection from the connection pool typically for each instance created. Especially for the case with
                // this workload that is testing network resources, we need to be very cognizant of our usage of TCP connections.
                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverInstance.IPAddress, serverIPAddress);
            }
        }

        /// <summary>
        /// SockPerf Possible States.
        /// </summary>
        internal class SockPerfWorkloadState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SockPerfWorkloadState"/> class.
            /// </summary>
            public SockPerfWorkloadState(ClientServerStatus status, IDictionary<string, IConvertible> properties = null)
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
