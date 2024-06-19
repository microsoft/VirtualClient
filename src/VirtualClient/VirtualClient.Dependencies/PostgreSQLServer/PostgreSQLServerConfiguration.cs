// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Numerics.Distributions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;
    using static VirtualClient.Dependencies.MySqlServer.MySQLServerConfiguration;

    /// <summary>
    /// Provides functionality for configuring PostgreSQL Server.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLServerConfiguration : ExecuteCommand
    {
        private const string PythonCommand = "python3";
        private readonly IStateManager stateManager;
        private string packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySQLServerConfiguration"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PostgreSQLServerConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            this.SystemManager = dependencies.GetService<ISystemManagement>();
            this.SystemManager.ThrowIfNull(nameof(this.SystemManager));
            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// The specifed action that controls the execution of the dependency.
        /// </summary>
        public string Action
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Action));
            }
        }

        /// <summary>
        /// The specifed action that controls the execution of the dependency.
        /// </summary>
        public bool SkipInitialize
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.SkipInitialize), false);
            }
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DiskFilter
        {
            get
            {
                // and 256G
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "osdisk:false&sizegreaterthan:256g");
            }
        }

        /// <summary>
        /// Database Name to create and utilize
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseName));
            }
        }

        /// <summary>
        /// Parameter defines the port to use for the PostgreSQL Server.
        /// </summary>
        public string Port
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Port), out IConvertible port);
                return port?.ToString();
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
        /// Shared Buffer Size for PostgreSQL
        /// </summary>
        public string SharedMemoryBuffer
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.SharedMemoryBuffer), out IConvertible sharedMemoryBuffer);
                return sharedMemoryBuffer?.ToString();
            }
        }

        /// <summary>
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Executes PostgreSQL configuration steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            string stateId = $"{nameof(PostgreSQLServerConfiguration)}-{this.Action}-action-success";
            ConfigurationState configurationState = await this.stateManager.GetStateAsync<ConfigurationState>($"{nameof(ConfigurationState)}", cancellationToken)
                .ConfigureAwait(false);

            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            workloadPackage.ThrowIfNull(this.PackageName);

            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.packageDirectory = package.Path;

            telemetryContext.AddContext(nameof(configurationState), configurationState);

            if (!this.SkipInitialize)
            {
                if (configurationState == null)
                {
                    switch (this.Action)
                    {
                        case ConfigurationAction.ConfigureServer:
                            await this.ConfigurePostgreSQLServerAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                        case ConfigurationAction.DistributeDatabase:
                            await this.DistributePostgreSQLDatabaseAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                    }

                    await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
                }
            }
        }

        private async Task ConfigurePostgreSQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string serverIp = this.GetServerIpAddress();

            string arguments = $"{this.packageDirectory}/configure-server.py --dbName {this.DatabaseName} --password {this.SuperUserPassword} --port {this.Port} --sharedMemoryBuffer {this.SharedMemoryBuffer} --serverIp {serverIp}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
               "python3",
               arguments,
               this.packageDirectory,
               telemetryContext,
               cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task DistributePostgreSQLDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string innoDbDirs = await this.GetPostgreSQLInnodbDirectoriesAsync(cancellationToken);

            string arguments = $"{this.packageDirectory}/distribute-database.py --dbName {this.DatabaseName} --directories {innoDbDirs} --password {this.SuperUserPassword}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    PythonCommand,
                    arguments,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task<string> GetPostgreSQLInnodbDirectoriesAsync(CancellationToken cancellationToken)
        {
            string diskPaths = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (disks?.Any() != true)
                {
                    throw new WorkloadException(
                        "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                foreach (Disk disk in disksToTest)
                {
                    diskPaths += $"{disk.GetPreferredAccessPath(this.Platform)};";
                }
            }

            return diskPaths;
        }

        private string GetServerIpAddress()
        {
            string serverIPAddress = IPAddress.Loopback.ToString();

            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIP);
                serverIPAddress = serverIP.ToString();
            }

            return serverIPAddress;
        }

        /// <summary>
        /// Supported PostgreSQL Server configuration actions.
        /// </summary>
        internal class ConfigurationAction
        {
            /// <summary>
            /// Setup the required configurations of the SQL Server.
            /// </summary>
            public const string ConfigureServer = nameof(ConfigureServer);

            /// <summary>
            /// Creates Database on PostgreSQL server and Users on Server and any Clients.
            /// </summary>
            public const string SetupDatabase = nameof(SetupDatabase);

            /// <summary>
            /// Distributes existing database to disks on the system
            /// </summary>
            public const string DistributeDatabase = nameof(DistributeDatabase);

        }

        internal class ConfigurationState
        {
            [JsonConstructor]
            public ConfigurationState(string action)
            {
                this.Action = action;
            }

            [JsonProperty("action")]
            public string Action { get; }
        }
    }
}
