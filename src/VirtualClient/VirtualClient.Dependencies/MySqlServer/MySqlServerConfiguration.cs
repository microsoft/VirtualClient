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
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "Logical");
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
            string stateId = $"{nameof(MySQLServerConfiguration)}-{this.Action}-action-success";
            ConfigurationState configurationState = await this.stateManager.GetStateAsync<ConfigurationState>(stateId, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext(nameof(configurationState), configurationState);

            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            workloadPackage.ThrowIfNull(this.PackageName);

            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.packageDirectory = package.Path;

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
                        case ConfigurationAction.SetGlobalVariables:
                            await this.SetMySQLGlobalVariableAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                    }

                    await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
                }
            }
        }

        private async Task ConfigureMySQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string serverIp = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? IPAddress.Loopback.ToString();

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

        private async Task<string> GetMySQLInnodbDirectoriesAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<Disk> filteredDisks = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform);

            this.Logger.LogTraceMessage($"{this.TypeName}: Total disks discovered: {disks.Count()}. Disks after filtering ('{this.DiskFilter}'): {filteredDisks.Count()}.");

            string accessPath = filteredDisks
                .SelectMany(d => d.Volumes)
                .SelectMany(v => v.AccessPaths)
                .FirstOrDefault();

            // No logical volume found — fall back to the biggest non-OS physical disk.
            if (string.IsNullOrEmpty(accessPath))
            {
                const string physicalDiskFilter = "OsDisk:false&BiggestSize";
                IEnumerable<Disk> physicalDisks = DiskFilters.FilterDisks(disks, physicalDiskFilter, this.Platform);

                this.Logger.LogTraceMessage($"{this.TypeName}: No logical volume found. Falling back to physical disk filter ('{physicalDiskFilter}'): {physicalDisks.Count()} disk(s).");

                try
                {
                    accessPath = physicalDisks.FirstOrDefault()?.GetPreferredAccessPath(this.Platform);
                }
                catch (Exception)
                {
                    // The disk may not have any eligible volumes.
                }
            }

            if (string.IsNullOrEmpty(accessPath))
            {
                throw new DependencyException(
                    "Expected disks not found. Given the parameters defined for the profile action/step or those passed " +
                    "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                    "of the existing disks.",
                    ErrorReason.DependencyNotFound);
            }

            string mysqlPath = this.Combine(accessPath, "mysql");

            if (!this.SystemManager.FileSystem.Directory.Exists(mysqlPath))
            {
                this.Logger.LogTraceMessage($"{this.TypeName}: Creating MySQL InnoDB directory '{mysqlPath}'.");
                this.SystemManager.FileSystem.Directory.CreateDirectory(mysqlPath);
            }

            this.Logger.LogTraceMessage($"{this.TypeName}: MySQL InnoDB directory resolved to '{mysqlPath}'.");

            return mysqlPath;
        }
        
        private async Task<string> GetMySQLInMemoryCapacityAsync(CancellationToken cancellationToken)
        {
            MemoryInfo memoryInfo = await this.SystemManager.GetMemoryInfoAsync(cancellationToken);
            long totalMemoryKiloBytes = memoryInfo.TotalMemory;
            int bufferSizeInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes / 1024);
            
            return bufferSizeInMegaBytes.ToString();
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
