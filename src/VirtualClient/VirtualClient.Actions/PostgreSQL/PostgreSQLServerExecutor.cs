// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
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
        // Maintained in the HammerDB package that we use.
        private const string CreateDBScriptName = "createDBScript.sh";
        private const string CreateDBTclName = "createDB.tcl";
        private Item<PostgreSQLServerState> serverState;
        private string defaultPassword;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
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
        /// The client username to use for accessing the database to execute transactions.
        /// </summary>
        protected string ClientUsername { get; } = "virtualclient";

        /// <summary>
        /// The client password to use for accessing the database to execute transactions.
        /// </summary>
        protected string ClientPassword { get; } = Guid.NewGuid().ToString().ToLowerInvariant();

        /// <summary>
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetServerOnline(false);
            return base.InitializeAsync(telemetryContext, cancellationToken);
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
                if (!this.serverState.Definition.DatabaseInitialized || this.ReuseDatabase)
                {
                    await this.ConfigureDatabaseServerAsync(telemetryContext, cancellationToken);
                    await this.ConfigureWorkloadAsync(cancellationToken);
                    await this.CreateDatabaseAsync(cancellationToken);
                    await this.SaveServerStateAsync(cancellationToken);
                }

                this.SetServerOnline(true);

                // Keep the server-running if we are in a multi-role/system topology. That will mean that the
                // server is running on a system separate from the client system.
                if (this.IsMultiRoleLayout())
                {
                    this.Logger.LogMessage($"{this.TypeName}.KeepServerAlive", telemetryContext);
                    await this.WaitAsync(cancellationToken);
                }
            }
            finally
            {
                this.SetServerOnline(false);
            }
        }

        private async Task ConfigureDatabaseServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string pghbaConfigPath = this.Combine(this.PostgreSqlPackagePath, "pg_hba.conf");
            string postgressqlConfigPath = this.Combine(this.PostgreSqlPackagePath, "postgresql.conf");
            string postgressqlTempConfigPath = this.Combine(this.PostgreSqlPackagePath, "postgresql_temp.conf");
            string pghbaConfigCopyToPath = null;
            string postgressqlConfigCopyToPath = null;

            // The PostgreSQL files are copied to protected folders that require elevated/sudo privileges. In order to 
            // keep the moving parts to a minimum, we are using a temp file here that we can easily update before copying
            // it in full to PostgreSQL installation directory.
            this.FileSystem.File.Copy(postgressqlConfigPath, postgressqlTempConfigPath, true);

            await this.FileSystem.File.ReplaceInFileAsync(
                postgressqlTempConfigPath,
                @"\{Port\}",
                this.Port.ToString(),
                cancellationToken);

            if (this.Platform == PlatformID.Unix)
            {
                // 1) Set the configuration files for the PostgreSQL server.
                //
                // e.g.
                // /etc/postgresql/14/main
                pghbaConfigCopyToPath = this.Combine(this.PostgreSqlInstallationPath, "pg_hba.conf");
                postgressqlConfigCopyToPath = this.Combine(this.PostgreSqlInstallationPath, "postgresql.conf");

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "cp",
                    $"--force '{pghbaConfigPath}' '{pghbaConfigCopyToPath}'",
                    this.PostgreSqlPackagePath,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                    }
                }

                // Note that we are copying the 'temp' file instance of the config to the PostgreSQL installation folder
                // here. This ensures that we do not hit permissions issues.
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "cp",
                    $"--force '{postgressqlTempConfigPath}' '{postgressqlConfigCopyToPath}'",
                    this.PostgreSqlPackagePath,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                    }
                }

                // 2) Set the password for the PostgreSQL server.
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                     "psql -c \"ALTER USER postgres PASSWORD 'postgres';\"",
                     this.PostgreSqlInstallationPath,
                     telemetryContext,
                     cancellationToken,
                     runElevated: true,
                     username: "postgresql"))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                    }
                }

                // 3) Restart the PostgreSQL service/daemon.
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                     "systemctl restart postgresql",
                     this.PostgreSqlInstallationPath,
                     telemetryContext,
                     cancellationToken,
                     runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                    }
                }

                ////// 1) Update the database server host configuration.
                ////string pg_hba_configPath = this.Combine(this.PostgreSqlInstallationPath, "pg_hba.conf");
                ////string[] pg_hba_configContents = await this.FileSystem.File.ReadAllLinesAsync(pg_hba_configPath);
                ////string[] pg_hba_hostConfig = new string[]
                ////{
                ////    "host  all  all  0.0.0.0/0  md5"
                ////};

                ////// This simply replaces any existing sections marked with the new host config. If the section
                ////// does not exist, the host config will be appended.
                ////IEnumerable<string> pg_hba_updatedConfig = pg_hba_configContents.AddOrReplaceSectionContentAsync(
                ////    pg_hba_hostConfig,
                ////    "# Virtual Client Section Begin",
                ////    "# Virtual Client Section End");

                ////await this.FileSystem.File.WriteAllLinesAsync(pg_hba_configPath, pg_hba_updatedConfig, cancellationToken);

                ////// 2) Update the database server configuration.
                ////string postgresql_configPath = this.Combine(this.PostgreSqlInstallationPath, "postgresql.conf");

                ////string[] postgresql_configContents = await this.FileSystem.File.ReadAllLinesAsync(pg_hba_configPath);
                ////string[] postgresql_hostConfig = new string[]
                ////{
                ////    "host  all  all  0.0.0.0/0  md5"
                ////};

                ////if (!currentConfigContents.Contains(pg_hba_hostConfig))
                ////{
                ////    StringBuilder pg_hba_config = new StringBuilder()
                ////        .AppendLine("# [VirtualClient:Begin]")
                ////        .AppendLine("1 a host  all  all  0.0.0.0/0  md5")
                ////        .AppendLine("# [VirtualClient:End]");

                ////    string replaceConfigFileHostAddressWithEmptyString = $"sed -i \"{pg_hba_hostConfig}\" pg_hba.conf";
                ////    string addAddressInConfigFile = @"sed ""1 a host  all  all  0.0.0.0/0  md5"" pg_hba.conf -i";
                ////    string changeListenAddressToAllAddresses = "sed -i \"s/#listen_addresses = 'localhost'/listen_addresses = '*'/g\" postgresql.conf";
                ////    string addPortNumberToConfigFile = $"sed -i \"s/port = .*/port = {PortNumber}/g\" postgresql.conf";
                ////    string changeUserPassword = "-u postgres psql -c \"ALTER USER postgres PASSWORD 'postgres';\"";

                ////    // 1) Replace the config file host address with an empty string.
                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                ////        $"sed -i \"{pg_hba_hostConfig}\" pg_hba.conf",
                ////        null,
                ////        this.PostgreSqlInstallationPath,
                ////        cancellationToken);

                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                ////        @"sed ""1 a host  all  all  0.0.0.0/0  md5"" pg_hba.conf -i",
                ////        null,
                ////        this.PostgreSqlInstallationPath,
                ////        cancellationToken);

                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                ////        "sed -i \"s/#listen_addresses = 'localhost'/listen_addresses = '*'/g\" postgresql.conf",
                ////        null,
                ////        this.PostgreSqlInstallationPath,
                ////        cancellationToken);

                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>(addPortNumberToConfigFile, null, this.PostgreSqlInstallationPath, cancellationToken);
                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>(changeUserPassword, null, this.PostgreSqlInstallationPath, cancellationToken);
                ////    await this.ExecuteCommandAsync<PostgreSQLExecutor>("systemctl restart postgresql", null, this.PostgreSqlInstallationPath, cancellationToken);
                ////}
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                // 1) Set the configuration files for the PostgreSQL server.
                //
                // e.g.
                // /etc/postgresql/14/main
                pghbaConfigCopyToPath = this.Combine(this.PostgreSqlInstallationPath, "data", "pg_hba.conf");
                postgressqlConfigCopyToPath = this.Combine(this.PostgreSqlInstallationPath, "data", "postgresql.conf");

                this.FileSystem.File.Copy(pghbaConfigPath, pghbaConfigCopyToPath, true);
                this.FileSystem.File.Copy(postgressqlConfigPath, postgressqlConfigCopyToPath, true);

                await this.FileSystem.File.ReplaceInFileAsync(postgressqlConfigCopyToPath, @"\{Port\}", this.Port.ToString(), cancellationToken);

                ////// 1) Update the database server configuration.
                ////string configPath = this.Combine(this.PostgreSqlInstallationPath, "data", "pg_hba.conf");
                ////string currentConfigContents = await this.FileSystem.File.ReadAllTextAsync(configPath);
                ////string hostConfig = "'host  all  all  0.0.0.0/0  md5'";

                ////await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                ////    "powershell",
                ////    $" -Command \"& {{Add-Content -Path '{this.Combine(this.PostgreSqlInstallationPath, "data", "pg_hba.conf")}' -Value {hostConfig}}}\"",
                ////    this.PostgreSqlInstallationPath,
                ////    cancellationToken);

                // 2) Restart the PostgreSQL service/daemon.
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                     $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "pg_ctl.exe")}",
                     $"restart -D \"{this.Combine(this.PostgreSqlInstallationPath, "data")}\"",
                     this.Combine(this.PostgreSqlInstallationPath, "bin"),
                     telemetryContext,
                     cancellationToken,
                     runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                    }
                }

                ////await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                ////    $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "pg_ctl.exe")}",
                ////    $"restart -D \"{this.Combine(this.PostgreSqlInstallationPath, "data")}\"",
                ////    this.Combine(this.PostgreSqlInstallationPath, "bin"),
                ////    cancellationToken);
            }
        }

        private Task ConfigureWorkloadAsync(CancellationToken cancellationToken)
        {
            // Each time that we run we copy the script file and the TCL file into the root HammerDB directory
            // alongside the benchmarking toolsets (e.g. hammerdbcli).
            string scriptPath = this.Combine(this.HammerDBPackagePath, "postgresql", PostgreSQLServerExecutor.CreateDBScriptName);
            string tclPath = this.Combine(this.HammerDBPackagePath, "postgresql", PostgreSQLServerExecutor.CreateDBTclName);
            string scriptCopyPath = null;
            string tclCopyPath = null;

            if (!this.FileSystem.File.Exists(tclPath))
            {
                throw new DependencyException(
                    $"Required script file missing. The script file required in order to create the database '{PostgreSQLServerExecutor.CreateDBTclName}' " +
                    $"does not exist in the HammerDB package.",
                    ErrorReason.DependencyNotFound);
            }

            if (this.Platform == PlatformID.Unix)
            {
                if (!this.FileSystem.File.Exists(scriptPath))
                {
                    throw new DependencyException(
                        $"Required script file missing. The script file required in order to create the database '{PostgreSQLServerExecutor.CreateDBScriptName}' " +
                        $"does not exist in the HammerDB package.",
                        ErrorReason.DependencyNotFound);
                }

                scriptCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLServerExecutor.CreateDBScriptName);
                tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLServerExecutor.CreateDBTclName);

                this.FileSystem.File.Copy(scriptPath, scriptCopyPath, true);
                this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLServerExecutor.CreateDBTclName);
                this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
            }

            return this.SetTclScriptParametersAsync(tclCopyPath, cancellationToken);
        }

        private async Task CreateDatabaseAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                // 1) Drop the TPCC database if it exists. This happens on a fresh start ONLY.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"-u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"",
                    null,
                    this.PostgreSqlInstallationPath,
                    cancellationToken);

                // 2) Drop the user used to access the database.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"-u postgres psql -c \"DROP ROLE IF EXISTS {this.ClientUsername};\"",
                    null,
                    this.PostgreSqlInstallationPath,
                    cancellationToken);

                // 3) Create the user anew so that the password matches the current/expected one.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"-u postgres psql -c \"CREATE USER {this.ClientUsername} PASSWORD '{this.ClientPassword}';\"",
                    null,
                    this.PostgreSqlInstallationPath,
                    cancellationToken);

                // 4) Create the TPCC database and populate it with data.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"bash {PostgreSQLServerExecutor.CreateDBScriptName}",
                    null,
                    this.HammerDBPackagePath,
                    cancellationToken);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                this.SetEnvironmentVariable("PGPASSWORD", "postgres", EnvironmentVariableTarget.Process);

                // 1) Drop the TPCC database if it exists. This happens on a fresh start ONLY.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                    $"-U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"",
                    this.Combine(this.PostgreSqlInstallationPath, "bin"),
                    cancellationToken);

                // 2) Drop the user used to access the database.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                    $"-U postgres -c \"DROP ROLE IF EXISTS {this.ClientUsername};\"",
                    this.Combine(this.PostgreSqlInstallationPath, "bin"),
                    cancellationToken);

                // 3) Create the user anew so that the password matches the current/expected one.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                    $"-U postgres -c \"CREATE USER {this.ClientUsername} PASSWORD '{this.ClientPassword}';\"",
                    this.Combine(this.PostgreSqlInstallationPath, "bin"),
                    cancellationToken);

                // 4) Create the TPCC database and populate it with data.
                await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                    $"{this.Combine(this.HammerDBPackagePath, "hammerdbcli.bat")}",
                    $"auto {PostgreSQLServerExecutor.CreateDBTclName}",
                    this.HammerDBPackagePath,
                    cancellationToken);
            }
        }

        private async Task SaveServerStateAsync(CancellationToken cancellationToken)
        {
            this.serverState.Definition.DatabaseInitialized = true;
            this.serverState.Definition.WarehouseCount = this.WarehouseCount;
            this.serverState.Definition.UserCount = this.UserCount;
            this.serverState.Definition.UserName = this.ClientUsername;
            this.serverState.Definition.Password = this.ClientPassword;

            using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                this.serverState,
                cancellationToken))
            {
                response.ThrowOnError<WorkloadException>();
            }
        }

        private async Task SetTclScriptParametersAsync(string tclFilePath, CancellationToken cancellationToken)
        {
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
        }
    }
}