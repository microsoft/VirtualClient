// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies;

    /// <summary>
    /// PostgreSQL Executor
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Port number on which PostgreSQL server is hosted.
        /// </summary>
        protected const int PortNumber = 5432;

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
        }

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
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        public IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        public IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// The file path where logs will be written.
        /// </summary>
        public string HammerDBPackagePath { get; set; }

        /// <summary>
        /// Workload package path.
        /// </summary>
        public string WorkloadPackagePath { get; set; }

        /// <summary>
        /// Workload package path.
        /// </summary>
        public string PostgreSQLInstallationPath { get; set; }

        /// <summary>
        /// Server's Cancellation Token Source.
        /// </summary>
        public CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Enables file system interactions.
        /// </summary>
        protected IFileSystem FileSystem { get; set; }

        /// <summary>
        /// The role of the current Virtual Client instance. Supported roles = Client or Server
        /// </summary>
        protected string Role { get; private set; }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ThrowIfPlatformIsNotSupported();

            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.InitializeApiClients();

            PostgreSQLExecutor.OpenFirewallPorts(this.SystemManager.FirewallManager, PortNumber, cancellationToken);

            this.FileSystem = this.Dependencies.GetService<IFileSystem>();
            ISystemManagement systemManager = this.Dependencies.GetService<ISystemManagement>();

            DependencyPath postgreSQLInstallationPath = await systemManager.PackageManager.GetPackageAsync(PostgresqlInstallation.PostgresqlPackage, cancellationToken).ConfigureAwait(false);

            if (postgreSQLInstallationPath == null)
            {
                throw new DependencyException(
                    $"The '{PostgresqlInstallation.PostgresqlPackage}' workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.PostgreSQLInstallationPath = postgreSQLInstallationPath.Path;

            DependencyPath hammerDBPackage = await systemManager.PackageManager.GetPackageAsync(HammerDbInstallation.HammerDbPackage, cancellationToken).ConfigureAwait(false);

            if (hammerDBPackage == null)
            {
                throw new DependencyException(
                    $"The '{HammerDbInstallation.HammerDbPackage}' workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.HammerDBPackagePath = hammerDBPackage.Path;

            DependencyPath workloadPackage = await systemManager.PackageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);
            
            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The '{this.PackageName}' workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.WorkloadPackagePath = workloadPackage.Path;

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
            this.StartTime = DateTime.UtcNow;
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

                    // Keep the server-running if we are in a multi-role/system topology and this
                    // instance of VC is the Server role.
                    if (isMultiRole && this.IsInRole(ClientRole.Server))
                    {
                        this.Logger.LogMessage($"{nameof(PostgreSQLExecutor)}.KeepServerAlive", telemetryContext);
                        await this.WaitAsync(serverCancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.

                    this.Logger.LogMessage($"{nameof(PostgreSQLExecutor)}.Canceled", telemetryContext);
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
                    using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments, workingDirectory))
                    {
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                        process.RedirectStandardOutput = true;
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<TExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();
                    }

                    return output;
                }).ConfigureAwait(false);
            }

            return output;
        }

        /// <summary>
        /// Deletes the existing states.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await this.ServerApiClient.DeleteStateAsync(
                                                            nameof(PostgreSQLState),
                                                            CancellationToken.None).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.NoContent)
            {
                response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
            }

            HttpResponseMessage parameterResponse = await this.ServerApiClient.DeleteStateAsync(
                                                    nameof(PostgreSQLParameters),
                                                    CancellationToken.None).ConfigureAwait(false);

            if (parameterResponse.StatusCode != HttpStatusCode.NoContent)
            {
                parameterResponse.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
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
        /// Logs the list of results provide
        /// </summary>
        protected void LogTestResults(IEnumerable<Metric> results, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (results != null)
            {
                this.Logger.LogMetrics(
                    "PostgreSQL",
                    "PostgreSQL",
                    startTime,
                    endTime,
                    new List<Metric>(results),
                    "Database",
                    this.Parameters.ToString(),
                    this.Tags,
                    telemetryContext);
            }
        }

        /// <summary>
        /// SetUp required configuration to run postgreSQL.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task SetUpConfigurationAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                string replaceConfigFileHostAddressWithEmptyString = "sed -i \"s%host  all  all  0.0.0.0/0  md5%%g\" pg_hba.conf";
                string addAddressInConfigFile = @"sed ""1 a host  all  all  0.0.0.0/0  md5"" pg_hba.conf -i";
                string changeListenAddressToAllAddresses = "sed -i \"s/#listen_addresses = 'localhost'/listen_addresses = '*'/g\" postgresql.conf";
                string addPortNumberToConfigFile = $"sed -i \"s/port = .*/port = {PortNumber}/g\" postgresql.conf";
                string changeUserPassword = "-u postgres psql -c \"ALTER USER postgres PASSWORD 'postgres';\"";

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(replaceConfigFileHostAddressWithEmptyString, null, this.PostgreSQLInstallationPath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(addAddressInConfigFile, null, this.PostgreSQLInstallationPath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(changeListenAddressToAllAddresses, null, this.PostgreSQLInstallationPath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(addPortNumberToConfigFile, null, this.PostgreSQLInstallationPath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(changeUserPassword, null, this.HammerDBPackagePath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>("systemctl restart postgresql", null, this.HammerDBPackagePath, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                string addAddressInConfigFileCommandarg = $" -Command \"& {{Add-Content -Path '{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "data", "pg_hba.conf")}' -Value 'host  all  all  0.0.0.0/0  md5'}}\"";

                string restartPostgresqlCommandarg = $"restart -D \"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "data")}\"";

                await this.ExecuteCommandAsync<PostgreSQLExecutor>("powershell", addAddressInConfigFileCommandarg, this.PostgreSQLInstallationPath, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>($"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin", "pg_ctl.exe")}", restartPostgresqlCommandarg, this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin"), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private static void OpenFirewallPorts(IFirewallManager firewallManager, int apiPort, CancellationToken cancellationToken)
        {
            firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "PostgreSQL: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { PortNumber })
                },
                cancellationToken).GetAwaiter().GetResult();
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

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
            }
        }

        private void ThrowIfPlatformIsNotSupported()
        {
            if (this.Platform != PlatformID.Unix && this.Platform != PlatformID.Win32NT)
            {
                throw new WorkloadException(
                    $"'{this.Platform.ToString()}' is not currently supported, only {PlatformID.Unix}-{Architecture.X64}, {PlatformID.Win32NT}-{Architecture.X64} is currently supported",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The PostgreSQL benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()} through Virtual Client.  Supported distros include:" +
                            $" Ubuntu ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }
    }

    /// <summary>
    /// PostgreSQL Possible States.
    /// </summary>
    public class PostgreSQLState
    {
        /// <summary>
        /// Creating Database for PostgreSQL execution.
        /// </summary>
        public const string CreatingDB = nameof(PostgreSQLState.CreatingDB);

        /// <summary>
        /// Created Database for PostgreSQL execution.
        /// </summary>
        public const string DBCreated = nameof(PostgreSQLState.DBCreated);
    }

    /// <summary>
    /// Parameters required for running the PostgreSQL client.
    /// </summary>
    public class PostgreSQLParameters
    {
        /// <summary>
        /// Username for the postgreSQL database server.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// PostgreSQL Server Password for passing the queries from Client to Server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// No.of Warehouses.
        /// </summary>
        public long WarehouseCount { get; set; }

        /// <summary>
        /// Number of users for sending transactions from client to server.
        /// </summary>
        public int NumOfVirtualUsers { get; set; }

    }
}