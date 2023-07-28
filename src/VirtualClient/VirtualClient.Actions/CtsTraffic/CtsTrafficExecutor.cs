// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// CtsTraffic Executor
    /// </summary>
    [WindowsCompatible]
    public class CtsTrafficExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CtsTrafficExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CtsTrafficExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.SupportedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ClientRole.Client,
                ClientRole.Server
            };

            this.SystemManagement = dependencies.GetService<ISystemManagement>();
            this.FileSystem = this.Dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Parameter defines the port number on which the CtsTraffic server will run on.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 4444);
            }
        }

        /// <summary>
        /// Index of NumaNode.
        /// </summary>
        public int NumaNode
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.NumaNode), 0);
            }
        }

        /// <summary>
        /// Buffer in bytes.
        /// </summary>
        public int BufferInBytes
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.BufferInBytes), 65536);
            }
        }

        /// <summary>
        /// Pattern used while executing workload (e.g. push, pull, pushpull, duplex)
        /// </summary>
        public string Pattern 
        {
            get
            {
                 return this.Parameters.GetValue<string>(nameof(this.Pattern), "Duplex");
            }
        }

        /// <summary>
        /// Total bytes to be transferred while running workload.
        /// </summary>
        public string BytesToTransfer 
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BytesToTransfer), "0x400000000");
            }
        }

        /// <summary>
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        protected IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Workload package path.
        /// </summary>
        protected string CtsTrafficPackagePath { get; set; }

        /// <summary>
        /// Results Folder.
        /// </summary>
        protected string ResultsFolder { get; set; }

        /// <summary>
        /// Workload Exe Path.
        /// </summary>
        protected string CtsTrafficExe { get; set; }

        /// <summary>
        /// Executable to run benchmark on specified numanode.
        /// </summary>
        protected string ProcessInNumaNodeExe { get; set; }

        /// <summary>
        /// Status outfile name.
        /// </summary>
        protected string StatusFileName { get; set; }

        /// <summary>
        /// Connections output filename.
        /// </summary>
        protected string ConnectionsFileName { get; set; }

        /// <summary>
        /// Error output filename.
        /// </summary>
        protected string ErrorFileName { get; set; }

        /// <summary>
        /// Server's Cancellation Token Source.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Enables file system interactions.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// The role of the current Virtual Client instance. Supported roles = Client or Server
        /// </summary>
        protected string Role { get; private set; }

        /// <summary>
        /// Provides methods for managing system requirements.
        /// </summary>
        protected ISystemManagement SystemManagement { get; }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ValidatePlatformSupportedAsync(cancellationToken);

            await CtsTrafficExecutor.OpenFirewallPortsAsync(this.Port, this.SystemManagement.FirewallManager, cancellationToken);

            this.InitializeApiClients();

            DependencyPath ctsTrafficPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.CtsTrafficPackagePath = ctsTrafficPackage.Path;

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;
                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);

                this.Role = clientInstance.Role;
                this.ThrowIfRoleNotSupported(this.Role);
            }

            this.CtsTrafficExe = this.Combine(this.CtsTrafficPackagePath, "ctsTraffic.exe");
            this.ProcessInNumaNodeExe = this.Combine(this.CtsTrafficPackagePath, "StartProcessInNumaNode.exe");
            this.ResultsFolder = this.Combine(this.CtsTrafficPackagePath, "Results");
            this.ConnectionsFileName = this.Combine(this.ResultsFolder, "Connections.csv");
            this.StatusFileName = this.Combine(this.ResultsFolder, "Status.csv");
            this.ErrorFileName = this.Combine(this.ResultsFolder, "Errors.txt");

            if (!this.FileSystem.File.Exists(this.CtsTrafficExe))
            {
                throw new DependencyException(
                    $"Required executable -{this.CtsTrafficExe} missing.",
                    ErrorReason.DependencyNotFound);
            }

            if (!this.FileSystem.File.Exists(this.ProcessInNumaNodeExe))
            {
                throw new DependencyException(
                    $"Required executable -{this.ProcessInNumaNodeExe} missing.",
                    ErrorReason.DependencyNotFound);
            }

            if (!this.FileSystem.Directory.Exists(this.ResultsFolder))
            {
                this.FileSystem.Directory.CreateDirectory(this.ResultsFolder);
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            bool isMultiRole = this.IsMultiRoleLayout();

            using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    CancellationToken serverCancellationToken = this.ServerCancellationSource.Token;

                    if (!isMultiRole || this.IsInRole(ClientRole.Server))
                    {
                        using (var serverExecutor = this.CreateServerExecutor())
                        {
                            await serverExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(CtsTrafficExecutor)}.ServerExecutionCompleted", telemetryContext);
                        }
                    }

                    if (!isMultiRole || this.IsInRole(ClientRole.Client))
                    {
                        // After database creation completes. Runs threads querying database.
                        using (var clientExecutor = this.CreateClientExecutor())
                        {
                            await clientExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(CtsTrafficExecutor)}.ClientExecutionCompleted", telemetryContext);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
            }
        }

        /// <summary>
        /// Logs the list of results provide
        /// </summary>
        protected async Task CaptureMetricsAsync(IProcessProxy process, string commandArgs, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (this.FileSystem.File.Exists(this.StatusFileName))
                {
                    string contents = await this.LoadResultsAsync(this.StatusFileName, cancellationToken);

                    await this.LogProcessDetailsAsync(process, telemetryContext, $"{this.TypeName}-{this.Role}", contents.AsArray(), logToFile: true);

                    CtsTrafficMetricsParser parser = new CtsTrafficMetricsParser(contents);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        "CtsTraffic",
                        $"CtsTraffic-{this.Role}",
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        null,
                        commandArgs,
                        this.Tags,
                        telemetryContext);

                    await this.FileSystem.File.DeleteAsync(this.StatusFileName);
                }               
            }
        }

        /// <summary>
        /// Creates CtsTraffic server instance.
        /// </summary>
        /// <returns></returns>
        protected virtual VirtualClientComponent CreateServerExecutor()
        {
            return new CtsTrafficServerExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Creates CtsTraffic client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateClientExecutor()
        {
            return new CtsTrafficClientExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            this.LocalApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            bool isSingleVM = !this.IsMultiRoleLayout();

            if (isSingleVM)
            {
                this.ServerApiClient = this.LocalApiClient;
            }
            else
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverInstance);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        private static Task OpenFirewallPortsAsync(int port, IFirewallManager firewallManager, CancellationToken cancellationToken)
        {
            return firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "CtsTraffic: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { port })
                },
                cancellationToken);
        }

        private void ValidatePlatformSupportedAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                // Add logger
            }
        }

        /// <summary>
        /// CtsTraffic Possible States.
        /// </summary>
        internal class CtsTrafficServerState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CtsTrafficServerState"/> object.
            /// </summary>
            public CtsTrafficServerState()
                : base()
            {
                this.ServerSetupCompleted = false;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CtsTrafficServerState"/> object.
            /// </summary>
            [JsonConstructor]
            public CtsTrafficServerState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            /// <summary>
            /// True if Phase 1 Server Set Up completed for CtsTraffic Execution.
            /// </summary>
            public bool ServerSetupCompleted
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(this.ServerSetupCompleted));
                }

                set
                {
                    this[nameof(this.ServerSetupCompleted)] = value;
                }
            }
        }
    }
}