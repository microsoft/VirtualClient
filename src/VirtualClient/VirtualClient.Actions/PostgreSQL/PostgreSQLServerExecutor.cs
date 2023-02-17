// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            // This is the setup/configuration used to create the database.
            int logicalCores = Environment.ProcessorCount;
            int numberOfVirtualUsers = logicalCores * 10;

            this.WarehouseCount = numberOfVirtualUsers * 5;
            this.NumOfVirtualUsers = numberOfVirtualUsers;
            this.Password = Guid.NewGuid().ToString();
            this.UserName = Environment.MachineName.ToLower();
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
        /// Number of users for each Transaction.
        /// </summary>
        protected int NumOfVirtualUsers { get; set; }

        /// <summary>
        /// Password.
        /// </summary>
        protected string Password { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        protected string UserName { get; set; }

        /// <summary>
        /// Number of Warehouses.
        /// </summary>
        protected int WarehouseCount { get; set; }

        /// <summary>
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetServerOnline(false);
            await base.InitializeAsync(telemetryContext, cancellationToken);

            this.serverState = await this.LocalApiClient.GetOrCreateStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                cancellationToken,
                logger: this.Logger);

            if (!this.serverState.Definition.InitialSetupComplete)
            {
                await this.SetUpConfigurationAsync(cancellationToken);

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

                await this.SetTclScriptParameters(tclCopyPath, cancellationToken);

                this.serverState.Definition.UserName = this.UserName;
                this.serverState.Definition.Password = this.Password;
                this.serverState.Definition.NumOfVirtualUsers = this.NumOfVirtualUsers;
                this.serverState.Definition.WarehouseCount = this.WarehouseCount;
                this.serverState.Definition.InitialSetupComplete = true;

                await this.SaveServerStateAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Creates DB and postgreSQL server.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // We do not want to drop the database on every round of processing.
            if (!this.serverState.Definition.DatabaseCreated || this.ReuseDatabase)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    await this.ExecuteOnLinuxAsync(cancellationToken);
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    await this.ExecuteOnWindowsAsync(cancellationToken);
                }

                this.serverState.Definition.DatabaseCreated = true;
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

        /// <summary>
        /// Powershell script parameters {for server PostgreSQL}.
        /// </summary>
        /// <returns></returns>
        protected async Task SetTclScriptParameters(string tclFilePath, CancellationToken cancellationToken)
        {
            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<VIRTUALUSERS>",
                this.NumOfVirtualUsers.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<WAREHOUSECOUNT>",
                this.WarehouseCount.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<USERNAME>",
                this.UserName,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<PASSWORD>",
                this.Password,
                cancellationToken);
        }

        /// <summary>
        /// SetUp required configuration to run postgreSQL.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task SetUpConfigurationAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                string replaceConfigFileHostAddressWithEmptyString = "sed -i \"s%host  all  all  0.0.0.0/0  md5%%g\" pg_hba.conf";
                string addAddressInConfigFile = @"sed ""1 a host  all  all  0.0.0.0/0  md5"" pg_hba.conf -i";
                string changeListenAddressToAllAddresses = "sed -i \"s/#listen_addresses = 'localhost'/listen_addresses = '*'/g\" postgresql.conf";
                string addPortNumberToConfigFile = $"sed -i \"s/port = .*/port = {PortNumber}/g\" postgresql.conf";
                string changeUserPassword = "-u postgres psql -c \"ALTER USER postgres PASSWORD 'postgres';\"";

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(replaceConfigFileHostAddressWithEmptyString, null, this.PostgreSqlInstallationPath, cancellationToken);
                await this.ExecuteCommandAsync<PostgreSQLExecutor>(addAddressInConfigFile, null, this.PostgreSqlInstallationPath, cancellationToken);
                await this.ExecuteCommandAsync<PostgreSQLExecutor>(changeListenAddressToAllAddresses, null, this.PostgreSqlInstallationPath, cancellationToken);
                await this.ExecuteCommandAsync<PostgreSQLExecutor>(addPortNumberToConfigFile, null, this.PostgreSqlInstallationPath, cancellationToken);
                await this.ExecuteCommandAsync<PostgreSQLExecutor>(changeUserPassword, null, this.PostgreSqlInstallationPath, cancellationToken);
                await this.ExecuteCommandAsync<PostgreSQLExecutor>("systemctl restart postgresql", null, this.PostgreSqlInstallationPath, cancellationToken);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                string addAddressInConfigFileCommandarg = $" -Command \"& {{Add-Content -Path '{this.Combine(this.PostgreSqlInstallationPath, "data", "pg_hba.conf")}' -Value 'host  all  all  0.0.0.0/0  md5'}}\"";
                string restartPostgresqlCommandarg = $"restart -D \"{this.Combine(this.PostgreSqlInstallationPath, "data")}\"";

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                    "powershell",
                    addAddressInConfigFileCommandarg,
                    this.PostgreSqlInstallationPath,
                    cancellationToken);

                await this.ExecuteCommandAsync<PostgreSQLExecutor>(
                    $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "pg_ctl.exe")}",
                    restartPostgresqlCommandarg,
                    this.Combine(this.PostgreSqlInstallationPath, "bin"),
                    cancellationToken);
            }
        }

        private async Task ExecuteOnLinuxAsync(CancellationToken cancellationToken)
        {
            string restartCommand = $"sudo systemctl restart postgresql";
            string dropDBCommand = $"-u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(restartCommand, null, this.PostgreSqlInstallationPath, cancellationToken);
            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(dropDBCommand, null, this.PostgreSqlInstallationPath, cancellationToken);

            string dropUserCommand = $"-u postgres psql -c \"DROP ROLE IF EXISTS {this.UserName};\"";
            string createUserCommand = $"-u postgres psql -c \"CREATE USER {this.UserName} PASSWORD '{this.Password}';\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(dropUserCommand, null, this.PostgreSqlInstallationPath, cancellationToken);
            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(createUserCommand, null, this.PostgreSqlInstallationPath, cancellationToken);
            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"bash {PostgreSQLServerExecutor.CreateDBScriptName}", null, this.HammerDBPackagePath, cancellationToken);
        }

        private async Task ExecuteOnWindowsAsync(CancellationToken cancellationToken)
        {
            string restartCommandArg = $"restart -D \"{this.Combine(this.PostgreSqlInstallationPath, "data")}\"";
            string dropDBCommandArg = $"-U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "pg_ctl.exe")}",
                restartCommandArg,
                this.PostgreSqlInstallationPath,
                cancellationToken);

            Environment.SetEnvironmentVariable("PGPASSWORD", "postgres", EnvironmentVariableTarget.Process);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                dropDBCommandArg,
                this.Combine(this.PostgreSqlInstallationPath, "bin"),
                cancellationToken);

            string dropUserCommand = $"-U postgres -c \"DROP ROLE IF EXISTS {this.UserName};\"";
            string createUserCommand = $"-U postgres -c \"CREATE USER {this.UserName} PASSWORD '{this.Password}';\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                dropUserCommand, 
                this.Combine(this.PostgreSqlInstallationPath, "bin"),
                cancellationToken);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                $"{this.Combine(this.PostgreSqlInstallationPath, "bin", "psql.exe")}",
                createUserCommand,
                this.Combine(this.PostgreSqlInstallationPath, "bin"),
                cancellationToken);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(
                $"{this.Combine(this.HammerDBPackageName, "hammerdbcli.bat")}",
                $"auto {PostgreSQLServerExecutor.CreateDBTclName}",
                this.HammerDBPackagePath,
                cancellationToken);
        }

        private async Task SaveServerStateAsync(CancellationToken cancellationToken)
        { 
            using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                this.serverState,
                cancellationToken))
            {
                response.ThrowOnError<WorkloadException>();
            }
        }
    }
}