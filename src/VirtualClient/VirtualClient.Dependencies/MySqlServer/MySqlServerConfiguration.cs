// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.MySqlServer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for MySQL
    /// </summary>
    public class MySQLServerConfiguration : ExecuteCommand
    {
        private const string PythonCommand = "python3";
        private readonly IStateManager stateManager;
        private string packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySQLServerConfiguration"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MySQLServerConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                return this.Parameters.GetValue<string>(nameof(this.DatabaseName), "vc-mysqldb");
            }
        }

        /// <summary>
        /// Global variable name to set
        /// </summary>
        public string Variables
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Variables), string.Empty);
            }
        }

        /// <summary>
        /// Denotes if In-Memory scenario will be utilized
        /// </summary>
        public bool InMemory
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.InMemory), false);
            }
        }

        /// <summary>
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Installs MySQL
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            string stateId = $"{nameof(MySQLServerConfiguration)}-{this.Action}-action-success";
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
                            await this.ConfigureMySQLServerAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                        case ConfigurationAction.CreateDatabase:
                            await this.CreateMySQLServerDatabaseAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                        case ConfigurationAction.DistributeDatabase:
                            await this.DistributeMySQLDatabaseAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                    }

                    await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
                }
                else if (this.Action == ConfigurationAction.SetGlobalVariables)
                {
                    await this.SetMySQLGlobalVariableAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task ConfigureMySQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string serverIp = this.GetServerIpAddress();
            string innoDbDirs = await this.GetMySQLInnodbDirectoriesAsync(cancellationToken);

            string arguments = $"{this.packageDirectory}/configure.py --serverIp {serverIp} --innoDbDirs \"{innoDbDirs}\"";

            if (this.InMemory)
            {
                string inMemoryMB = await this.GetMySQLInMemoryCapacityAsync(cancellationToken);
                arguments += $" --inMemory {inMemoryMB}";
            }

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                PythonCommand,
                arguments,
                Environment.CurrentDirectory,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task CreateMySQLServerDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string arguments = $"{this.packageDirectory}/setup-database.py --dbName {this.DatabaseName}";

            if (this.IsMultiRoleLayout())
            {
                string clientIps = this.GetClientIpAddresses();
                arguments += $" --clientIps \"{clientIps}\"";
            }

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                PythonCommand,
                arguments,
                Environment.CurrentDirectory,
                telemetryContext,
                cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task SetMySQLGlobalVariableAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string arguments = $"{this.packageDirectory}/set-global-variables.py --variables \"{this.Variables}\"";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    PythonCommand,
                    arguments,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task DistributeMySQLDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string innoDbDirs = await this.GetMySQLInnodbDirectoriesAsync(cancellationToken);

            string arguments = $"{this.packageDirectory}/distribute-database.py --dbName {this.DatabaseName} --directories \"{innoDbDirs}\"";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    PythonCommand,
                    arguments,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        private async Task<string> GetMySQLInnodbDirectoriesAsync(CancellationToken cancellationToken)
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
                    if (disk.GetPreferredAccessPath(this.Platform) != "/mnt")
                    {
                        diskPaths += $"{disk.GetPreferredAccessPath(this.Platform)};";
                    }
                }
            }

            return diskPaths;
        }
        
        private async Task<string> GetMySQLInMemoryCapacityAsync(CancellationToken cancellationToken)
        {
            MemoryInfo memoryInfo = await this.SystemManager.GetMemoryInfoAsync(cancellationToken);
            long totalMemoryKiloBytes = memoryInfo.TotalMemory;
            int bufferSizeInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes / 1024);
            
            return bufferSizeInMegaBytes.ToString();
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

        private string GetClientIpAddresses()
        {
            string clientIpAddresses = string.Empty;

            IEnumerable<ClientInstance> clientInstances = this.GetLayoutClientInstances(ClientRole.Client);

            foreach (ClientInstance instance in clientInstances)
            {
                IPAddress.TryParse(instance.IPAddress, out IPAddress clientIPAddress);
                clientIpAddresses += clientIPAddress.ToString() + ';';
            }

            return clientIpAddresses;
        }

        /// <summary>
        /// Supported MySQL Server configuration actions.
        /// </summary>
        internal class ConfigurationAction
        {
            /// <summary>
            /// Setup the required configurations of the SQL Server.
            /// </summary>
            public const string ConfigureServer = nameof(ConfigureServer);

            /// <summary>
            /// Creates Database on MySQL server and Users on Server and any Clients.
            /// </summary>
            public const string CreateDatabase = nameof(CreateDatabase);

            /// <summary>
            /// Sets global variables to user-specified value.
            /// ie. "MAX_PREPARED_STMT_COUNT=1000000;MAX_CONNECTIONS=1024"
            /// </summary>
            public const string SetGlobalVariables = nameof(SetGlobalVariables);

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
