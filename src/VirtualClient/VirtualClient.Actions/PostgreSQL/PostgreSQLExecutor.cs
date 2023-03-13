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
    using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                return this.Parameters.GetValue<int>(nameof(this.Port), 5432);
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
        /// The file path where logs will be written.
        /// </summary>
        protected string HammerDBPackagePath { get; set; }

        /// <summary>
        /// Workload package path.
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
        /// Executes the commands.
        /// </summary>
        /// <param name="command">Command that needs to be executed</param>
        /// <param name="arguments">any extra arguments for the command</param>
        /// <param name="workingDirectory">The directory where we want to execute the command</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>String output of the command.</returns>
        protected async Task<string> ExecuteCommandAsync<TExecutor>(string command, string arguments, string workingDirectory, CancellationToken cancellationToken)
            where TExecutor : VirtualClientComponent
        {
            string output = string.Empty;
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{command}' '{arguments}'  at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", command);

                await this.Logger.LogMessageAsync($"{typeof(TExecutor).Name}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL");
                            process.ThrowIfWorkloadFailed();
                        }

                        output = process.StandardOutput.ToString();
                    }

                    return output;
                }).ConfigureAwait(false);
            }

            return output;
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
        /// Logs the list of results provide
        /// </summary>
        protected void CaptureMetrics(IEnumerable<Metric> results, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (results != null)
            {
                this.Logger.LogMetrics(
                    "PostgreSQL",
                    "TPC-C",
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
        }
    }
}