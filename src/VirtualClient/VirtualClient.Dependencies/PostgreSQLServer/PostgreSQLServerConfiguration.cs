// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for configuring PostgreSQL Server.
    /// </summary>
    [SupportedPlatforms("linux-x64,linux-arm64")]
    public class PostgreSQLServerConfiguration : ExecuteCommand
    {
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
        /// Whether to skip configuration even if the action has not completed previously.
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

        /// <summary>
        /// Reads the explicit PostgreSQL password file first, otherwise uses non-empty PGPASSWORD,
        /// otherwise reads Password.json in the platform-specific PostgreSQL package.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The password to supply only to the database child process environment.</returns>
        protected async Task<string> GetPostgreSQLPasswordAsync(CancellationToken cancellationToken)
        {
            string passwordFile = this.Parameters.GetValue<string>("PostgreSQLPasswordFile", string.Empty);
            if (string.IsNullOrWhiteSpace(passwordFile))
            {
                string environmentPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
                if (!string.IsNullOrEmpty(environmentPassword))
                {
                    return environmentPassword;
                }

                DependencyPath package = await this.GetPlatformSpecificPackageAsync("postgresql", cancellationToken).ConfigureAwait(false);
                passwordFile = this.Combine(package.Path, "Password.json");
            }

            IFileSystem fileSystem = this.SystemManager.FileSystem;
            string password = null;
            try
            {
                if (fileSystem.File.Exists(passwordFile))
                {
                    string contents = await fileSystem.File.ReadAllTextAsync(passwordFile, cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(contents))
                    {
                        JToken value = JObject.Parse(contents)["Password"];
                        if (value?.Type == JTokenType.String)
                        {
                            password = value.Value<string>();
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is JsonException || exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new DependencyException(
                    "Unable to read the PostgreSQL password file. Check its permissions and JSON format.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new DependencyException(
                    "The selected PostgreSQL password file must exist and contain a non-empty string named 'Password'.",
                    ErrorReason.InvalidProfileDefinition);
            }

            return password;
        }

        private async Task ConfigurePostgreSQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string password = await this.GetPostgreSQLPasswordAsync(cancellationToken).ConfigureAwait(false);

            string serverIp = (this.GetLayoutClientInstances(ClientRole.Server, false) ?? Enumerable.Empty<ClientInstance>())
                                    .FirstOrDefault()?.IPAddress
                                    ?? IPAddress.Loopback.ToString();

            string directory = await this.GetPostgreSQLDataDirectoryAsync(cancellationToken);

            string arguments = $"{this.packageDirectory}/configure-server.py --dbName {this.DatabaseName} --serverIp {serverIp} --port {this.Port} --inMemory {this.SharedMemoryBuffer} --directory {directory}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
               "python3",
               arguments,
               this.packageDirectory,
               telemetryContext,
               cancellationToken,
               beforeExecution: childProcess => childProcess.EnvironmentVariables["PGPASSWORD"] = password))
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

            string accessPath = filteredDisks
                .SelectMany(d => d.Volumes)
                .SelectMany(v => v.AccessPaths)
                .FirstOrDefault();

            // lshw typically does not report LVM logical volumes (/dev/dm-*, /dev/mapper/*).
            // Check /proc/mounts directly for any LVM-backed mount point as a fallback.
            if (string.IsNullOrEmpty(accessPath) && this.Platform != PlatformID.Win32NT)
            {
                accessPath = await this.FindLvmMountPathAsync(cancellationToken).ConfigureAwait(false);
            }

            // No logical volume found — fall back to the biggest non-OS physical disk.
            if (string.IsNullOrEmpty(accessPath))
            {
                const string physicalDiskFilter = "OsDisk:false&BiggestSize";
                IEnumerable<Disk> physicalDisks = DiskFilters.FilterDisks(disks, physicalDiskFilter, this.Platform);

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

            string directory = this.Combine(accessPath, "postgresql");

            if (!this.SystemManager.FileSystem.Directory.Exists(directory))
            {
                this.SystemManager.FileSystem.Directory.CreateDirectory(directory);
            }

            return directory;
        }

        /// <summary>
        /// Reads /proc/mounts to find a mount point backed by an LVM device (/dev/mapper/* or /dev/dm-*).
        /// Returns the mount path (e.g. /home/user/mnt_raid0) or null if none is found.
        /// </summary>
        private async Task<string> FindLvmMountPathAsync(CancellationToken cancellationToken)
        {
            try
            {
                string procMounts = await this.SystemManager.FileSystem.File.ReadAllTextAsync("/proc/mounts", cancellationToken)
                    .ConfigureAwait(false);

                foreach (string line in procMounts.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length >= 2
                        && (parts[0].StartsWith("/dev/mapper/", StringComparison.OrdinalIgnoreCase)
                            || parts[0].StartsWith("/dev/dm-", StringComparison.OrdinalIgnoreCase)))
                    {
                        this.Logger.LogTraceMessage($"{this.TypeName}: Found LVM mount from /proc/mounts: device='{parts[0]}', path='{parts[1]}'.");
                        return parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogTraceMessage($"{this.TypeName}: Could not read /proc/mounts: {ex.Message}");
            }

            return null;
        }

        private async Task SetupPostgreSQLDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string password = await this.GetPostgreSQLPasswordAsync(cancellationToken).ConfigureAwait(false);

            string arguments = $"{this.packageDirectory}/setup-database.py --dbName {this.DatabaseName} --port {this.Port}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
               "python3",
               arguments,
               this.packageDirectory,
               telemetryContext,
               cancellationToken,
               beforeExecution: childProcess => childProcess.EnvironmentVariables["PGPASSWORD"] = password))
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
            /// Configures PostgreSQL server access, credentials, memory, and data directory.
            /// </summary>
            public const string ConfigureServer = nameof(ConfigureServer);

            /// <summary>
            /// Runs the PostgreSQL database setup script.
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
