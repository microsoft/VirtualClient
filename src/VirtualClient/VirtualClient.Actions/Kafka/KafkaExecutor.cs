namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Kafka client action
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class KafkaExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public KafkaExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Provides the ability to create API clients for interacting with local as well as remote instances
        /// of the Virtual Client API service.
        /// </summary>
        protected IApiClientManager ApiClientManager { get; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Server IpAddress on which Redis Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManagement { get; set; }

        /// <summary>
        /// Path to Kafka server package.
        /// </summary>
        protected string KafkaPackagePath { get; set; }

        /// <summary>
        /// Command type to be used based on platform.
        /// </summary>
        protected string PlatformSpecificCommandType { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the Redis Memtier workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // await this.ValidatePlatformSupportAsync(cancellationToken);
            await this.EvaluateParametersAsync(cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }

            this.InitializeApiClients();

            DependencyPath kafkaPackage = await this.GetPackageAsync(this.PackageName, cancellationToken);
            this.KafkaPackagePath = kafkaPackage.Path;

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.PlatformSpecificCommandType = "cmd";
                    break;

                case PlatformID.Unix:
                    this.PlatformSpecificCommandType = "bash";
                    break;

                default:
                    throw new WorkloadException(
                        $"The kafka workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            bool isSingleVM = !this.IsMultiRoleLayout();

            if (isSingleVM)
            {
                this.ServerApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            }
            else
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);
                this.ServerIpAddress = serverIPAddress.ToString();

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        internal class KafkaServerState : State
        {
            [JsonConstructor]
            public KafkaServerState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            internal KafkaServerState(IEnumerable<int> ports)
               : base()
            {
                if (ports?.Any() == true)
                {
                    this[nameof(this.Ports)] = string.Join(",", ports);
                }
            }

            /// <summary>
            /// The set of ports on which the Kafka servers are running.
            /// </summary>
            public IEnumerable<int> Ports
            {
                get
                {
                    this.Properties.TryGetValue(nameof(this.Ports), out IConvertible ports);
                    return ports?.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i.Trim()));
                }
            }

            /// <summary>
            /// The port on which the Zookeeper server are running.
            /// </summary>
            public int ZookeeperPort { get; set; }
        }
    }
}