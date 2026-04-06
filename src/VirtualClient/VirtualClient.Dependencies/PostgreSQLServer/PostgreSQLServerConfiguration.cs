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
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;

    /// <summary>
    /// Provides functionality for configuring PostgreSQL Server.
    /// </summary>
    [SupportedPlatforms("linux-x64,linux-arm64")]
    public class PostgreSQLServerConfiguration : ExecuteCommand
    {
        private const string PythonCommand = "python3";
        private readonly IStateManager stateManager;
        private string packageDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerConfiguration"/> class.
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
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes("default"));
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
            string stateId = $"{nameof(PostgreSQLServerConfiguration)}-{this.Action}-action-success";
            ConfigurationState configurationState = await this.stateManager.GetStateAsync<ConfigurationState>(stateId, cancellationToken)
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
                        case ConfigurationAction.SetupDatabase:
                            await this.SetupPostgreSQLDatabaseAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                            break;
                    }

                    await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
                }
            }
        }

        private async Task ConfigurePostgreSQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string serverIp = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? IPAddress.Loopback.ToString();

            string directory = await this.GetPostgreSQLDataDirectoryAsync(cancellationToken);

            string arguments = $"{this.packageDirectory}/configure-server.py --dbName {this.DatabaseName} --serverIp {serverIp} --password {this.SuperUserPassword} --port {this.Port} --inMemory {this.SharedMemoryBuffer} --directory {directory}";

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

        private async Task<string> GetPostgreSQLDataDirectoryAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<Disk> filteredDisks = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform);

            // Search ALL disks for a raid0 mount point. After StripeDisks creates an LVM
            // striped volume from the filtered physical disks, the mount point appears on
            // the logical volume device, which is a separate disk entry from the originals.
            string raidAccessPath = disks
                .SelectMany(d => d.Volumes)
                .SelectMany(v => v.AccessPaths)
                .FirstOrDefault(p => p.Contains("raid0", StringComparison.OrdinalIgnoreCase));

            // lshw may not report LVM logical volumes at all. Fall back to reading
            // /proc/mounts which always lists every active mount point.
            if (string.IsNullOrEmpty(raidAccessPath) && this.Platform != PlatformID.Win32NT)
            {
                try
                {
                    string procMounts = await this.SystemManager.FileSystem.File.ReadAllTextAsync("/proc/mounts", cancellationToken)
                        .ConfigureAwait(false);

                    foreach (string line in procMounts.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] parts = line.Split(' ');
                        if (parts.Length >= 2 && parts[1].Contains("raid0", StringComparison.OrdinalIgnoreCase))
                        {
                            raidAccessPath = parts[1];
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    // /proc/mounts may not be available.
                }
            }

            string accessPath = raidAccessPath;

            // Last resort: use the first filtered disk's preferred access path.
            // GetPreferredAccessPath throws when the disk has no eligible volumes
            // (e.g. a raw device consumed by LVM), so we catch and continue.
            if (string.IsNullOrEmpty(accessPath))
            {
                try
                {
                    accessPath = filteredDisks.FirstOrDefault()?.GetPreferredAccessPath(this.Platform);
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

            string directory = this.Combine(accessPath, "postgresql");

            if (!this.SystemManager.FileSystem.Directory.Exists(directory))
            {
                this.SystemManager.FileSystem.Directory.CreateDirectory(directory);
            }

            return directory;
        }

        private async Task SetupPostgreSQLDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string arguments = $"{this.packageDirectory}/setup-database.py --dbName {this.DatabaseName} --password {this.SuperUserPassword} --port {this.Port}";

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
