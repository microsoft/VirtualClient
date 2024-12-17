// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// PostgreSQL Executor
    /// </summary>
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
        /// Parameter defines the SuperUser Password for PostgreSQL Server.
        /// </summary>
        public string SuperUserPassword
        {
            get
            {
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(this.ExperimentId));
                return Convert.ToBase64String(hashBytes);
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
        public string VirtualUsers
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HammerDBExecutor.VirtualUsers), out IConvertible virtualUsers);
                return virtualUsers?.ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The warehouse count passed to HammerDB.
        /// </summary>
        public string WarehouseCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.WarehouseCount), out IConvertible warehouseCount);
                return warehouseCount?.ToString();
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
        /// The workload option passed to HammerDB.
        /// </summary>
        public string SQLServer
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HammerDBClientExecutor.SQLServer), out IConvertible sqlServer);
                return sqlServer?.ToString();
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

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            HammerDBState state = await this.stateManager.GetStateAsync<HammerDBState>(nameof(HammerDBState), cancellationToken)
               ?? new HammerDBState();

            if (state.DatabaseCreated != 2)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.{this.Scenario}", telemetryContext.Clone(), async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.PrepareSQLDatabase(telemetryContext, cancellationToken);
                    }
                });
                state.DatabaseCreated++;
                await this.stateManager.SaveStateAsync<HammerDBState>(nameof(HammerDBState), state, cancellationToken);
            }
        }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(cancellationToken)
                .ConfigureAwait(false);

            await HammerDBExecutor.OpenFirewallPortsAsync(this.Port, this.SystemManager.FirewallManager, cancellationToken);

            DependencyPath hammerDBPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.HammerDBPackagePath = hammerDBPackage.Path;

            await this.InitializeExecutablesAsync(telemetryContext, cancellationToken);

            this.InitializeApiClients(cancellationToken);

            await this.Logger.LogMessageAsync($"{this.TypeName}.ConfigureHammerDBFile", telemetryContext.Clone(), async () =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.ConfigureCreateHammerDBFile(telemetryContext, cancellationToken);
                }
            });

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
                state.HammerDBInitialized = true;

                this.SystemManager.MakeFileExecutableAsync(this.Combine(this.HammerDBPackagePath, "hammerdbcli"), this.Platform, cancellationToken);

                this.SystemManager.MakeFileExecutableAsync(this.Combine(this.HammerDBPackagePath, "bin", "tclsh8.6"), this.Platform, cancellationToken);

                // Add the path to the HammerDB 'lib' folder to the LD_LIBRARY_PATH variable so that the *.so files can
                // be found.
                this.SetEnvironmentVariable(EnvironmentVariable.LD_LIBRARY_PATH, this.Combine(this.HammerDBPackagePath, "lib"), append: true);
            }

            await this.stateManager.SaveStateAsync<HammerDBState>(nameof(HammerDBState), state, cancellationToken);
        }

        private async Task PrepareSQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "python3";

            string arguments = $"{this.HammerDBPackagePath}/populate-database.py --createDBTCLPath {this.CreateDBTclName}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
                arguments,
                this.HammerDBPackagePath,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "HammerDB", logToFile: true);
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                }
            }
        }

        private async Task ConfigureCreateHammerDBFile(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = $"python3";
            string arguments = $"{this.HammerDBPackagePath}/configure-workload-generator.py --workload {this.Workload} --sqlServer {this.SQLServer} --port {this.Port}" +
                    $" --virtualUsers {this.VirtualUsers} --warehouseCount {this.WarehouseCount} --password {this.SuperUserPassword} --dbName {this.DatabaseName} --hostIPAddress {this.ServerIpAddress}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
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
            if (this.CpuArchitecture == System.Runtime.InteropServices.Architecture.X64)
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
                                $"The HammerDB workload generator is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution}. Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)}," +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                                ErrorReason.LinuxDistributionNotSupported);
                        }
                    }
                }
            }
            else
            {
                throw new WorkloadException(
                    $"The HammerDB workload generator is not supported on the current architecture.",
                    ErrorReason.ProcessorArchitectureNotSupported);
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

            public int DatabaseCreated
            {
                get
                {
                    return this.Properties.GetValue<int>(nameof(HammerDBState.DatabaseCreated), 0);
                }

                set
                {
                    this.Properties[nameof(HammerDBState.DatabaseCreated)] = value;
                }
            }
        }
    }
}