// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
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
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.HammerDBExecutor;

    /// <summary>
    /// PostgreSQL Executor
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class HammerDBExecutor : VirtualClientComponent
    {
        private readonly IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDBExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HammerDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.SupportedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ClientRole.Client,
                ClientRole.Server
            };

            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// Defines the name of the createDB TCL file.
        /// </summary>
        public string CreateDBTclName
        {
            get
            {
                return "createDB.tcl";
            }
        }

        /// <summary>
        /// Defines the name of the runTransactions TCL file.
        /// </summary>
        public string RunTransactionsTclName
        {
            get
            {
                return "runTransactions.tcl";
            }
        }

        /// <summary>
        /// Defines the name of the PostgreSQL database to create/use for the transactions.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseName));
            }
        }

        /// <summary>
        /// Parameter defines the scenario to use for the PostgreSQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string DatabaseScenario
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseScenario), HammerDBScenario.Balanced);
            }
        }

        /// <summary>
        /// Parameter defines the SuperUser Password for PostgreSQL Server.
        /// </summary>
        public int SuperUserPassword
        {
            get
            {
                return this.ExperimentId.GetHashCode();
            }
        }

        /// <summary>
        /// Parameter defines the port number on which the PostgreSQL server will run on.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// Parameter defines the number of virtual users.
        /// </summary>
        public int VirtualUsers
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.VirtualUsers));
            }
        }

        /// <summary>
        /// The warehouse count passed to HammerDB.
        /// </summary>
        public int? WarehouseCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HammerDBExecutor.WarehouseCount), out IConvertible warehouseCount);
                return warehouseCount?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int? Threads
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HammerDBClientExecutor.Threads), out IConvertible threads);
                return threads?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The workload option passed to HammerDB.
        /// </summary>
        public string Workload
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HammerDBClientExecutor.Workload), out IConvertible workload);
                return workload?.ToString();
            }
        }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Server's Cancellation Token Source.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Server IpAddress on which MySQL Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// The file path where Hammer DB package is downloaded.
        /// </summary>
        protected string HammerDBPackagePath { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(cancellationToken)
                .ConfigureAwait(false);

            await HammerDBExecutor.OpenFirewallPortsAsync(this.Port, this.SystemManager.FirewallManager, cancellationToken);

            DependencyPath hammerDBPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            this.HammerDBPackagePath = hammerDBPackage.Path;

            await this.InitializeExecutablesAsync(telemetryContext, cancellationToken);

            this.InitializeApiClients(cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();

                if (!this.IsMultiRoleLayout())
                {
                    this.ServerIpAddress = IPAddress.Loopback.ToString();
                    this.ServerApiClient = clientManager.GetOrCreateApiClient(this.ServerIpAddress, IPAddress.Loopback);
                }
                else
                {
                    ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                    IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                    this.ServerIpAddress = serverIPAddress.ToString();
                    this.ServerApiClient = clientManager.GetOrCreateApiClient(this.ServerIpAddress, serverIPAddress);
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
                }
            }
        }

        /// <summary>
        /// Initializes the workload executables on the system (e.g. attributes them as executable).
        /// </summary>
        protected async Task InitializeExecutablesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // store state with initialization status & record/table counts, if does not exist already

            HammerDBState state = await this.stateManager.GetStateAsync<HammerDBState>(nameof(HammerDBState), cancellationToken)
                ?? new HammerDBState();

            if (!state.HammerDBInitialized)
            {
                LinuxDistributionInfo distributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);
                string distribution = distributionInfo.LinuxDistribution.ToString();

                string arguments = $"{this.HammerDBPackagePath}/configure-workload-generator.py --createDBTCLPath {this.CreateDBTclName} --port {this.Port}" +
                    $" --virtualUsers {this.VirtualUsers} --warehouseCount {this.WarehouseCount} --password {this.SuperUserPassword} --databaseName {this.DatabaseName}";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "python3",
                    arguments,
                    this.HammerDBPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "HammerDBExecutor", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }

                state.HammerDBInitialized = true;

                // The path to the HammerDB 'bin' folder is expected to exist in the PATH environment variable
                // for the HammerDB toolset to work correctly.
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(this.HammerDBPackagePath, "bin"), append: true);

                // Add the path to the HammerDB 'lib' folder to the LD_LIBRARY_PATH variable so that the *.so files can
                // be found.
                this.SetEnvironmentVariable(EnvironmentVariable.LD_LIBRARY_PATH, this.Combine(this.HammerDBPackagePath, "lib"), append: true);
            }

            await this.stateManager.SaveStateAsync<HammerDBState>(nameof(HammerDBState), state, cancellationToken);
        }

        private static Task OpenFirewallPortsAsync(int port, IFirewallManager firewallManager, CancellationToken cancellationToken)
        {
            return firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "PostgreSQL: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { port })
                },
                cancellationToken);
        }

        private async Task CheckDistroSupportAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    switch (linuxDistributionInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            break;
                        default:
                            throw new WorkloadException(
                            $"The PostgreSQL TPCC workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution}. Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)}," +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                            ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
        }

        internal class HammerDBState : State
        {
            public HammerDBState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool HammerDBInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HammerDBState.HammerDBInitialized), false);
                }

                set
                {
                    this.Properties[nameof(HammerDBState.HammerDBInitialized)] = value;
                }
            }

            public bool DatabasePopulated
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HammerDBState.DatabasePopulated), false);
                }

                set
                {
                    this.Properties[nameof(HammerDBState.DatabasePopulated)] = value;
                }
            }
        }

        /// <summary>
        /// Defines the HammerDB scenario.
        /// </summary>
        internal class HammerDBScenario
        {
            public const string Balanced = nameof(Balanced);

            public const string InMemory = nameof(InMemory);

            public const string Configure = nameof(Configure);
        }
    }
}