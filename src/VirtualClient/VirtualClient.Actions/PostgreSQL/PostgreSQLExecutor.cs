// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
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
    /// PostgreSQL Executor
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLExecutor : VirtualClientComponent
    {
        internal const string CreateDBTclName = "createDB.tcl";
        internal const string RunTransactionsTclName = "runTransactions.tcl";

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public PostgreSQLExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
        /// Defines the name of the benchmark (e.g. tpcc).
        /// </summary>
        public string Benchmark
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Benchmark));
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
        /// Defines the name of the HammerDB package.
        /// </summary>
        public string HammerDBPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.HammerDBPackageName));
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
        /// The file path where Hammer DB package is downloaded.
        /// </summary>
        protected string HammerDBPackagePath { get; set; }

        /// <summary>
        /// Poatgresql Workload package path.
        /// </summary>
        protected string PostgreSqlPackagePath { get; set; }

        /// <summary>
        /// Workload package path.
        /// </summary>
        protected string PostgreSqlInstallationPath { get; set; }

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
            await this.ValidatePlatformSupportedAsync(cancellationToken);
            await PostgreSQLExecutor.OpenFirewallPortsAsync(this.Port, this.SystemManagement.FirewallManager, cancellationToken);

            this.InitializeApiClients();

            DependencyPath postgreSqlPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            DependencyPath hammerDBPackage = await this.GetPlatformSpecificPackageAsync(this.HammerDBPackageName, cancellationToken);

            // The *.vcpkg definition is expected to contain definitions specific to each platform/architecture
            // where the PostgreSQL application is installed.
            // 
            // e.g.
            // "metadata": {
            //   "installationPath-linux-x64": "/etc/postgresql/14/main",
            //   "installationPath-linux-arm64": "/etc/postgresql/14/main",
            //   "installationPath-windows-x64": "C:\\Program Files\\PostgreSQL\\14",
            //   "installationPath-windows-arm64": "C:\\Program Files\\PostgreSQL\\14",
            // }
            string metadataKey = $"{PackageMetadata.InstallationPath}-{this.PlatformArchitectureName}";
            if (!postgreSqlPackage.Metadata.TryGetValue(metadataKey, out IConvertible installationPath))
            {
                throw new WorkloadException(
                    $"Missing installation path. The '{this.PackageName}' package registration is missing the required '{metadataKey}' " +
                    $"metadata definition. This is required in order to execute PostgreSQL operations from the location where the software is installed.",
                    ErrorReason.DependencyNotFound);
            }

            this.HammerDBPackagePath = hammerDBPackage.Path;
            this.PostgreSqlPackagePath = postgreSqlPackage.Path;
            this.PostgreSqlInstallationPath = installationPath.ToString();

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;
                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);

                this.Role = clientInstance.Role;
                this.ThrowIfRoleNotSupported(this.Role);
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

                    await this.InitializeExecutablesAsync(cancellationToken);

                    if (!isMultiRole || this.IsInRole(ClientRole.Server))
                    {
                        using (var serverExecutor = this.CreateServerExecutor())
                        {
                            await serverExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(PostgreSQLExecutor)}.ServerExecutionCompleted", telemetryContext);
                        }
                    }

                    if (!isMultiRole || this.IsInRole(ClientRole.Client))
                    {
                        // After database creation completes. Runs threads querying database.
                        using (var clientExecutor = this.CreateClientExecutor())
                        {
                            await clientExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(PostgreSQLExecutor)}.ClientExecutionCompleted", telemetryContext);
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
        protected void CaptureMetrics(IEnumerable<Metric> results, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (results != null)
            {
                this.Logger.LogMetrics(
                    "PostgreSQL-HammerDB",
                    "TPC-C" + this.Scenario,
                    startTime,
                    endTime,
                    results.ToList(),
                    null,
                    null,
                    this.Tags,
                    telemetryContext);
            }
        }

        /// <summary>
        /// Creates PostgreSQL server instance.
        /// </summary>
        /// <returns></returns>
        protected virtual VirtualClientComponent CreateServerExecutor()
        {
            return new PostgreSQLServerExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Creates PostgreSQL client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateClientExecutor()
        {
            return new PostgreSQLClientExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Initializes the workload executables on the system (e.g. attributes them as executable).
        /// </summary>
        protected async Task InitializeExecutablesAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                // https://stackoverflow.com/questions/40779757/connect-postgresql-to-hammerdb

                string scriptsDirectory = this.PlatformSpecifics.GetScriptPath(this.PackageName);

                await this.SystemManagement.MakeFileExecutableAsync(
                    this.Combine(this.HammerDBPackagePath, "hammerdbcli"),
                    this.Platform,
                    cancellationToken);

                await this.SystemManagement.MakeFilesExecutableAsync(
                    this.Combine(this.HammerDBPackagePath, "bin"),
                    this.Platform,
                    cancellationToken);

                await this.SystemManagement.MakeFileExecutableAsync(
                    this.Combine(this.PostgreSqlPackagePath, "ubuntu", "configure.sh"),
                    this.Platform,
                    cancellationToken);

                await this.SystemManagement.MakeFileExecutableAsync(
                    this.Combine(scriptsDirectory, "inmemory.sh"),
                    this.Platform,
                    cancellationToken);

                await this.SystemManagement.MakeFileExecutableAsync(
                    this.Combine(scriptsDirectory, "balanced.sh"),
                    this.Platform,
                    cancellationToken);

                // The path to the PostgreSQL 'bin' folder is expected to exist in the PATH environment variable
                // for the HammerDB toolset to work correctly.
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(this.PostgreSqlInstallationPath, "bin"), append: true);

                // The path to the HammerDB 'bin' folder is expected to exist in the PATH environment variable
                // for the HammerDB toolset to work correctly.
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(this.HammerDBPackagePath, "bin"), append: true);

                // Add the path to the HammerDB 'lib' folder to the LD_LIBRARY_PATH variable so that the *.so files can
                // be found.
                this.SetEnvironmentVariable(EnvironmentVariable.LD_LIBRARY_PATH, this.Combine(this.HammerDBPackagePath, "lib"), append: true);
            }
        }

        /// <summary>
        /// Validates the component can be executed.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.CpuArchitecture != Architecture.X64)
            {
                throw new WorkloadException(
                    $"The current architecture '{this.CpuArchitecture}' is not supported for running the HammerDB workload " +
                    $"against a PostgreSQL server.",
                    ErrorReason.PlatformNotSupported);
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

        private void InitializeApiClients()
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

        private async Task ValidatePlatformSupportedAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        break;

                    default:
                        throw new WorkloadException(
                            $"The PostgreSQL benchmark workload is not supported by the Virtual Client on the current Linux distro " +
                            $"'{distroInfo.LinuxDistribution}' through Virtual Client.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        /// <summary>
        /// State information provided by the server role executor.
        /// </summary>
        internal class PostgreSQLServerState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSQLServerState"/> object.
            /// </summary>
            public PostgreSQLServerState()
                : base()
            {
                this.DatabaseInitialized = false;
                this.WarehouseCount = -1;
                this.UserCount = -1;
                this.BalancedScenarioInitialized = false;
                this.InMemoryScenarioInitialized = false;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSQLServerState"/> object.
            /// </summary>
            [JsonConstructor]
            public PostgreSQLServerState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            /// <summary>
            /// True if the PostgreSQL database was created.
            /// </summary>
            public bool DatabaseInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(this.DatabaseInitialized));
                }

                set
                {
                    this[nameof(this.DatabaseInitialized)] = value;
                }
            }

            /// <summary>
            /// The number of virtual users to use for running parallel OLTP operations against the database
            /// as part of the TPC-C operations.
            /// </summary>
            public int UserCount
            {
                get
                {
                    return this.Properties.GetValue<int>(nameof(this.UserCount));
                }

                set
                {
                    this[nameof(this.UserCount)] = value;
                }
            }

            /// <summary>
            /// Password to use for accessing the PostgreSQL server and target database.
            /// </summary>
            public string Password
            {
                get
                {
                    return this.Properties.GetValue<string>(nameof(this.Password));
                }

                set
                {
                    this[nameof(this.Password)] = value;
                }
            }

            /// <summary>
            /// Username to use for accessing the PostgreSQL server and target database.
            /// </summary>
            public string UserName
            {
                get
                {
                    return this.Properties.GetValue<string>(nameof(this.UserName));
                }

                set
                {
                    this[nameof(this.UserName)] = value;
                }
            }

            /// <summary>
            /// The number of warehouses to create as part of the TPC-C operations.
            /// </summary>
            public int WarehouseCount
            {
                get
                {
                    return this.Properties.GetValue<int>(nameof(this.WarehouseCount));
                }

                set
                {
                    this[nameof(this.WarehouseCount)] = value;
                }
            }

            /// <summary>
            /// True if the balanced scenario has been initialized.
            /// </summary>
            public bool BalancedScenarioInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(this.BalancedScenarioInitialized));
                }

                set
                {
                    this[nameof(this.BalancedScenarioInitialized)] = value;
                }
            }

            /// <summary>
            /// True if the in-memory scenario has been initialized.
            /// </summary>
            public bool InMemoryScenarioInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(this.InMemoryScenarioInitialized));
                }

                set
                {
                    this[nameof(this.InMemoryScenarioInitialized)] = value;
                }
            }
        }
    }
}