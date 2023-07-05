// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// PostgreSQL Server executor.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLServerExecutor : PostgreSQLExecutor
    {
        private readonly IAsyncPolicy stabilizationRetryPolicy;
        private Item<PostgreSQLServerState> serverState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.stabilizationRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(2, (retries) => this.StabilizationWait);
        }

        /// <summary>
        /// Parameters defines whether the database should be reused on subsequent runs of the 
        /// Virtual Client. The database can take hours to create and is in a reusable state on subsequent
        /// runs. This profile parameter allows the user to indicate the preference.
        /// </summary>
        public bool ReuseDatabase
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.ReuseDatabase));
            }
        }

        /// <summary>
        /// Parameter defines the password to use for the PostgreSQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string Password
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Password), out IConvertible password);
                return password?.ToString();
            }
        }

        /// <summary>
        /// Parameter defines the scenario to use for the PostgreSQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string StressScenario
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.StressScenario), PostgreSQLScenario.Default);
            }
        }

        /// <summary>
        /// Number of virtual users to use for executing transactions against the database.
        /// Default = # logical cores/vCPUs x 10.
        /// </summary>
        public int UserCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.UserCount), Environment.ProcessorCount * 10);
            }
        }

        /// <summary>
        /// Number of warehouses to create in the database to use for executing transactions. The more
        /// warehouses, the larger the size of the database. Default = # virtual users x 5.
        /// </summary>
        public int WarehouseCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.WarehouseCount), this.UserCount * 5);
            }
        }

        /// <summary>
        /// The username to use for accessing the database to execute transactions.
        /// </summary>
        protected string ClientUsername { get; } = "postgres";

        /// <summary>
        /// The client password to use for accessing the database to execute transactions.
        /// </summary>
        protected string ClientPassword { get; set; }

        /// <summary>
        /// A period of time to wait after having created the database and modified the PostgreSQL
        /// server configuration to allow the services to come online with stability.
        /// </summary>
        protected TimeSpan StabilizationWait { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetServerOnline(false);
            await base.InitializeAsync(telemetryContext, cancellationToken);

            this.ClientPassword = this.Password;
            if (string.IsNullOrWhiteSpace(this.Password))
            {
                // Use the default that is defined within the PostgreSQL package. We use the
                // same password for both the user account as well as the 
                this.ClientPassword = await this.GetServerCredentialAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Creates DB and postgreSQL server.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                this.serverState = await this.LocalApiClient.GetOrCreateStateAsync<PostgreSQLServerState>(
                    nameof(PostgreSQLServerState),
                    cancellationToken,
                    logger: this.Logger);

                // We do not want to drop the database on every round of processing necessarily because
                // it takes a lot of time to rebuild the DB. We allow the user to request either reusing the
                // existing database (the default) or to start from scratch (i.e. ReuseDatabase = false).
                if (!this.serverState.Definition.DatabaseInitialized || !this.ReuseDatabase)
                {
                    await this.ConfigureDatabaseServerAsync(telemetryContext, cancellationToken);
                    await this.ConfigureWorkloadAsync(cancellationToken);

                    await this.stabilizationRetryPolicy.ExecuteAsync(async () =>
                    {
                        // We've notice the PostgreSQL services need a bit of time to come back online properly
                        // before we can issue calls against the database.
                        await Task.Delay(this.StabilizationWait, cancellationToken);

                        await this.CreateDatabaseAsync(telemetryContext, cancellationToken);
                        await this.ConfigureDatabaseForScenariosAsync(telemetryContext, cancellationToken);
                        await this.SaveServerStateAsync(cancellationToken);

                        // Same reasoning as above.
                        await Task.Delay(this.StabilizationWait, cancellationToken);
                    });
                }

                this.SetServerOnline(true);

                // Keep the server-running if we are in a multi-role/system topology. That will mean that the
                // server is running on a system separate from the client system.
                if (this.IsMultiRoleLayout())
                {
                    this.Logger.LogMessage($"{this.TypeName}.KeepServerAlive", telemetryContext);
                    await this.WaitAsync(cancellationToken);
                    this.SetServerOnline(false);
                }
            }
            catch
            {
                this.SetServerOnline(false);
                throw;
            }
        }

        private async Task ConfigureDatabaseServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    string workingDirectory = null;
                    string configScript = "configure.sh";
                    LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                    switch (distroInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            workingDirectory = this.Combine(this.PostgreSqlPackagePath, "ubuntu");
                            break;
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        this.Combine(workingDirectory, configScript),
                        this.Port.ToString(),
                        workingDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
                else
                {
                    string configScript = "configure.cmd";
                    string workingDirectory = this.PostgreSqlPackagePath;

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        this.Combine(workingDirectory, configScript),
                        $"{this.Port}",
                        workingDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
            }
        }

        private async Task ConfigureWorkloadAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // Each time that we run we copy the script file and the TCL file into the root HammerDB directory
                // alongside the benchmarking toolsets (e.g. hammerdbcli).
                string tclPath = this.Combine(this.HammerDBPackagePath, "benchmarks", this.Benchmark.ToLowerInvariant(), "postgresql", PostgreSQLServerExecutor.CreateDBTclName);
                string tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLServerExecutor.CreateDBTclName);

                if (!this.FileSystem.File.Exists(tclPath))
                {
                    throw new DependencyException(
                        $"Required script file missing. The script file required in order to create the database '{PostgreSQLServerExecutor.CreateDBTclName}' " +
                        $"does not exist in the HammerDB package.",
                        ErrorReason.DependencyNotFound);
                }

                this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
                await this.SetTclScriptParametersAsync(tclCopyPath, cancellationToken);
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Not Applicable.")]
        private async Task CreateDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                EventContext relatedContext = telemetryContext.Clone();

                await this.Logger.LogMessageAsync($"{this.TypeName}.CreateDatabase", telemetryContext, async () =>
                {
                    if (this.Platform == PlatformID.Unix)
                    {
                        // 1) Drop the database if it exists. This happens on a fresh start ONLY.
                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                           "psql",
                           $"-c \"DROP DATABASE IF EXISTS {this.DatabaseName};\"",
                           this.PostgreSqlInstallationPath,
                           relatedContext,
                           cancellationToken,
                           runElevated: true,
                           username: this.ClientUsername))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext);
                                process.ThrowIfWorkloadFailed();
                            }
                        }

                        // 2) Create the database. DO NOT run this command as elevated. HammerDB creates files that should not be owned
                        //    by the 'root' user or permissions issues will happen.
                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                            "bash",
                            $"-c \"{this.Combine(this.HammerDBPackagePath, "hammerdbcli")} auto {PostgreSQLServerExecutor.CreateDBTclName}\"",
                            this.HammerDBPackagePath,
                            relatedContext,
                            cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }
                        }
                    }
                    else if (this.Platform == PlatformID.Win32NT)
                    {
                        // this.SetEnvironmentVariable("PGPASSWORD", this.ClientPassword, EnvironmentVariableTarget.Process);

                        Action<IProcessProxy> setEnvironmentVariables = (process) =>
                        {
                            string existingPath = process.StartInfo.EnvironmentVariables[EnvironmentVariable.PATH];

                            process.StartInfo.EnvironmentVariables["PGPASSWORD"] = this.ClientPassword;
                            process.StartInfo.EnvironmentVariables[EnvironmentVariable.PATH] = string.Join(';', new string[]
                            {
                                this.Combine(this.HammerDBPackagePath, "bin"),
                                this.Combine(this.PostgreSqlInstallationPath, "bin"),
                                existingPath
                            });
                        };

                        // 1) Drop the database if it exists. This happens on a fresh start ONLY.
                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                           this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe"),
                           $"-U {this.ClientUsername} -c \"DROP DATABASE IF EXISTS {this.DatabaseName};\"",
                           this.Combine(this.PostgreSqlInstallationPath, "bin"),
                           relatedContext,
                           cancellationToken,
                           beforeExecution: setEnvironmentVariables))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext);
                                process.ThrowIfWorkloadFailed();
                            }
                        }

                        // 2) Create the database.
                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                            this.Combine(this.HammerDBPackagePath, "hammerdbcli.bat"),
                            $"auto {PostgreSQLServerExecutor.CreateDBTclName}",
                            this.HammerDBPackagePath,
                            relatedContext,
                            cancellationToken,
                            beforeExecution: setEnvironmentVariables))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }
                        }
                    }
                });
            }
        }

        private async Task ConfigureDatabaseForScenariosAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        { 
            if (!cancellationToken.IsCancellationRequested)
            {
                switch (this.StressScenario)
                {
                    // If Balanced Scenario: after creating the database, run balanced script to distribute
                    // database and/or individual tables on available disks.

                    case PostgreSQLScenario.Balanced:

                        if (!this.serverState.Definition.BalancedScenarioInitialized)
                        {
                            await this.PrepareBalancedScenarioAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);

                            this.serverState.Definition.BalancedScenarioInitialized = true;
                        }

                        break;

                    // If InMemory Scenario: before running configure script, modify postgres.conf
                    // to allow for higher in-memory buffer capacity.

                    case PostgreSQLScenario.InMemory:

                        if (!this.serverState.Definition.InMemoryScenarioInitialized)
                        {
                            await this.PrepareInMemoryScenarioAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);

                            this.serverState.Definition.InMemoryScenarioInitialized = true;
                        }

                        break;

                    case PostgreSQLScenario.Default:
                        break;
                }
            }
        }

        private async Task PrepareInMemoryScenarioAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    string inMemoryScript = "inmemory.sh";
                    string workingDirectory = null;

                    LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                    switch (distroInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            workingDirectory = this.Combine(this.PostgreSqlPackagePath, "ubuntu");
                            break;
                    }

                    MemoryInfo memoryInfo = await this.SystemManagement.GetMemoryInfoAsync(cancellationToken);
                    long totalMemoryKiloBytes = memoryInfo.TotalMemory;
                    int bufferSizeInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes * 0.75 / 1024);

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        this.Combine(workingDirectory, inMemoryScript),
                        $"{bufferSizeInMegaBytes}",
                        workingDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
            }
        }

        private async Task PrepareBalancedScenarioAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string diskPathsArgument = string.Empty;
                string diskFilter = "osdisk:false";

                IEnumerable<Disk> disks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (disks?.Any() != true)
                {
                    throw new WorkloadException(
                        "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, diskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                if (await this.CreateMountPointsAsync(disksToTest, cancellationToken).ConfigureAwait(false))
                {
                    // Refresh the disks to pickup the mount point changes.
                    await Task.Delay(1000).ConfigureAwait(false);
                    IEnumerable<Disk> updatedDisks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                    disksToTest = DiskFilters.FilterDisks(updatedDisks, diskFilter, this.Platform).ToList();
                }

                foreach (Disk disk in disksToTest)
                {
                    if (disk.GetPreferredAccessPath(this.Platform) != "/mnt")
                    {
                        diskPathsArgument += $"{disk.GetPreferredAccessPath(this.Platform)} ";
                    }
                }

                if (this.Platform == PlatformID.Unix)
                {
                    string balancedScript = "balanced.sh";
                    string workingDirectory = null;

                    LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                    switch (distroInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            workingDirectory = this.Combine(this.PostgreSqlPackagePath, "ubuntu");
                            break;
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        this.Combine(workingDirectory, balancedScript),
                        diskPathsArgument,
                        workingDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks on which to create the mount points.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        private async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                {
                    string newMountPoint = volume.GetDefaultMountPoint();
                    this.Logger.LogTraceMessage($"Create Mount Point: {newMountPoint}");

                    EventContext relatedContext = EventContext.Persisted().Clone()
                        .AddContext(nameof(volume), volume)
                        .AddContext("mountPoint", newMountPoint);

                    await this.Logger.LogMessageAsync($"{this.TypeName}.CreateMountPoint", relatedContext, async () =>
                    {
                        string newMountPoint = volume.GetDefaultMountPoint();
                        if (!this.SystemManagement.FileSystem.Directory.Exists(newMountPoint))
                        {
                            this.SystemManagement.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                        }

                        await this.SystemManagement.DiskManager.CreateMountPointAsync(volume, newMountPoint, cancellationToken)
                            .ConfigureAwait(false);

                        mountPointsCreated = true;

                    }).ConfigureAwait(false);
                }
            }

            return mountPointsCreated;
        }

        private async Task<string> GetServerCredentialAsync(CancellationToken cancellationToken)
        {
            string fileName = "superuser.txt";
            string path = this.Combine(this.PostgreSqlPackagePath, fileName);
            if (!this.FileSystem.File.Exists(path))
            {
                throw new DependencyException(
                    $"Required file '{fileName}' missing in package '{this.PostgreSqlPackagePath}'. The PostgreSQL server cannot be configured. " +
                    $"As an alternative, you can supply the '{nameof(this.Password)}' parameter on the command line.",
                    ErrorReason.DependencyNotFound);
            }

            return (await this.FileSystem.File.ReadAllTextAsync(path, cancellationToken)).Trim();
        }

        private async Task SaveServerStateAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.serverState.Definition.DatabaseInitialized = true;
                this.serverState.Definition.WarehouseCount = this.WarehouseCount;
                this.serverState.Definition.UserCount = this.UserCount;
                this.serverState.Definition.UserName = this.ClientUsername;
                this.serverState.Definition.Password = this.ClientPassword;

                using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync<PostgreSQLServerState>(nameof(PostgreSQLServerState), this.serverState, cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }
            }
        }

        private async Task SetTclScriptParametersAsync(string tclFilePath, CancellationToken cancellationToken)
        {
            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<PORT>",
                this.Port.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<VIRTUALUSERS>",
                this.UserCount.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<WAREHOUSECOUNT>",
                this.WarehouseCount.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<USERNAME>",
                this.ClientUsername,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<PASSWORD>",
                this.ClientPassword,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<SUPERUSERPWD>",
                this.ClientPassword,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<DATABASENAME>",
                this.DatabaseName,
                cancellationToken);
        }

        /// <summary>
        /// Defines the Postgresql benchmark scenario.
        /// </summary>
        internal class PostgreSQLScenario
        {
            public const string Balanced = nameof(Balanced);

            public const string InMemory = nameof(InMemory);

            public const string Default = nameof(Default);
        }
    }
}