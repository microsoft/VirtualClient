// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.MySqlServerConfiguration
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
        private const string WindowsMySQLPackagePath = "C:\\tools\\mysql\\current\\bin\\";
        private readonly IStateManager stateManager;

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
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), string.Empty);
            }
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseName), "vc-mysqldb");
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

            telemetryContext.AddContext(nameof(configurationState), configurationState);

            if (configurationState == null && !this.SkipInitialize)
            {
                switch (this.Action)
                {
                    case ConfigurationAction.StartServer:
                        await this.StartMySQLServerAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.CreateDatabase:
                        await this.CreateMySQLDatabase(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.RaisedMaxStatementCount:
                        await this.RaiseMySQLMaximumStatementLimit(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.RaisedMaxConnectionCount:
                        await this.RaiseMySQLMaximumConnectionsLimit(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.ConfigureNetwork:
                        await this.ConfigureMySQLNetwork(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.CreateUser:
                        await this.CreateMySQLUser(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.SetInnodbDirectories:
                        await this.SetMySQLInnodbDirectories(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case ConfigurationAction.PrepareInMemoryScenario:
                        await this.PrepareInMemoryScenario(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                }

                await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
            }
        }

        private async Task StartMySQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = string.Empty;

            if (this.Platform == PlatformID.Win32NT)
            {
                command = $"{WindowsMySQLPackagePath}mysqld.exe";
            }
            else if (this.Platform == PlatformID.Unix)
            {
                command = "systemctl start mysql.service";
            }

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed();
                }
            }
        }

        private async Task CreateMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> commands = new List<string>();

            if (this.Platform == PlatformID.Win32NT)
            {
                commands.Add($"{WindowsMySQLPackagePath}mysql.exe --execute=\"DROP DATABASE IF EXISTS {this.DatabaseName};\" --user=root");
                commands.Add($"{WindowsMySQLPackagePath}mysql.exe --execute=\"CREATE DATABASE {this.DatabaseName};\" --user=root");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                commands.Add($"mysql --execute=\"DROP DATABASE IF EXISTS {this.DatabaseName};\"");
                commands.Add($"mysql --execute=\"CREATE DATABASE {this.DatabaseName};\"");
            }

            foreach (string command in commands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            }
        }

        private async Task RaiseMySQLMaximumStatementLimit(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = string.Empty;

            if (this.Platform == PlatformID.Win32NT)
            {
                command = $"{WindowsMySQLPackagePath}mysql.exe --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\" --user=root";
            }
            else if (this.Platform == PlatformID.Unix)
            {
                command = $"mysql --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\"";
            }

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed();
                }
            }
        }

        private async Task RaiseMySQLMaximumConnectionsLimit(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = string.Empty;

            if (this.Platform == PlatformID.Win32NT)
            {
                command = $"{WindowsMySQLPackagePath}mysql.exe --execute=\"SET GLOBAL MAX_CONNECTIONS=1024;\" --user=root";
            }
            else if (this.Platform == PlatformID.Unix)
            {
                command = $"mysql --execute=\"SET GLOBAL MAX_CONNECTIONS=1024;\"";
            }

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed();
                }
            }
        }

        private async Task ConfigureMySQLNetwork(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                List<string> commands = new List<string>();

                if (this.Platform == PlatformID.Unix)
                {
                    commands.Add($"sed -i \"s/.*bind-address.*/bind-address = {serverIPAddress}/\" /etc/mysql/mysql.conf.d/mysqld.cnf");
                    commands.Add($"systemctl restart mysql.service");
                }

                foreach (string command in commands)
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        command,
                        null,
                        Environment.CurrentDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                            process.ThrowIfDependencyInstallationFailed();
                        }
                    }
                }
            }
        }

        private async Task CreateMySQLUser(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> commands = new List<string>();

            List<string> clientIpAddresses = new List<string>
            {
                "localhost"
            };

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> clientInstances = this.GetLayoutClientInstances(ClientRole.Client);

                foreach (ClientInstance instance in clientInstances)
                {
                    IPAddress.TryParse(instance.IPAddress, out IPAddress clientIPAddress);
                    clientIpAddresses.Add(clientIPAddress.ToString());
                }
            }

            if (this.Platform == PlatformID.Win32NT)
            {
                foreach (string clientIp in clientIpAddresses)
                {
                    commands.Add($"{WindowsMySQLPackagePath}mysql.exe --execute=\"DROP USER IF EXISTS '{this.DatabaseName}'@'{clientIp}'\" --user=root");
                    commands.Add($"{WindowsMySQLPackagePath}mysql.exe --execute=\"CREATE USER '{this.DatabaseName}'@'{clientIp}'\" --user=root");
                    commands.Add($"{WindowsMySQLPackagePath}mysql.exe --execute=\"GRANT ALL ON *.* TO '{this.DatabaseName}'@'{clientIp}'\" --user=root");
                }
            }
            else if (this.Platform == PlatformID.Unix)
            {
                foreach (string clientIp in clientIpAddresses)
                {
                    commands.Add($"mysql --execute=\"DROP USER IF EXISTS '{this.DatabaseName}'@'{clientIp}'\"");
                    commands.Add($"mysql --execute=\"CREATE USER '{this.DatabaseName}'@'{clientIp}'\"");
                    commands.Add($"mysql --execute=\"GRANT ALL ON *.* TO '{this.DatabaseName}'@'{clientIp}'\"");
                }
            }

            foreach (string command in commands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            }
        }

        private async Task SetMySQLInnodbDirectories(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string diskPaths = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                string diskFilter = "osdisk:false";

                if (!string.IsNullOrEmpty(this.DiskFilter))
                {
                    diskFilter += string.Concat("&", this.DiskFilter);
                }

                IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
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

                foreach (Disk disk in disksToTest)
                {
                    if (disk.GetPreferredAccessPath(this.Platform) != "/mnt")
                    {
                        diskPaths += $"{disk.GetPreferredAccessPath(this.Platform)} ";
                    }
                }

                string innodbDirectoriesScript = "set-mysql-innodb-directories.sh";
                string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("mysqlserverconfiguration");

                await this.SystemManager.MakeFilesExecutableAsync(
                    scriptsDirectory,
                    this.Platform,
                    cancellationToken);

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.PlatformSpecifics.Combine(scriptsDirectory, innodbDirectoriesScript),
                    diskPaths,
                    scriptsDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerConfiguration", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            }
        }

        private async Task PrepareInMemoryScenario(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // server's job is to configure buffer size, in memory script updates the mysql config file

                List<string> commands = new List<string>();

                MemoryInfo memoryInfo = await this.SystemManager.GetMemoryInfoAsync(cancellationToken);
                long totalMemoryKiloBytes = memoryInfo.TotalMemory;
                int bufferSizeInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes / 1024);

                if (this.Platform == PlatformID.Unix)
                {
                    commands.Add($"sed -i \"s|.*key_buffer_size.*|key_buffer_size = ${bufferSizeInMegaBytes}M|\" /etc/mysql/mysql.conf.d/mysqld.cnf");
                    commands.Add($"systemctl restart mysql.service");
                }

                foreach (string command in commands) 
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    null,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfDependencyInstallationFailed();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Supported MySQL Server configuration actions.
        /// </summary>
        internal class ConfigurationAction
        {
            /// <summary>
            /// Setup the required configurations of the SQL Server.
            /// </summary>
            public const string StartServer = nameof(StartServer);

            /// <summary>
            /// Creates Temp DB.
            /// </summary>
            public const string CreateDatabase = nameof(CreateDatabase);

            /// <summary>
            /// Increases max_prepared_stmt_count for maximum VM stress.
            /// </summary>
            public const string RaisedMaxStatementCount = nameof(RaisedMaxStatementCount);

            /// <summary>
            /// Increases max_conections for maximum VM stress.
            /// </summary>
            public const string RaisedMaxConnectionCount = nameof(RaisedMaxConnectionCount);

            /// <summary>
            /// Allow network connections to the server.
            /// </summary>
            public const string ConfigureNetwork = nameof(ConfigureNetwork);

            /// <summary>
            /// Creates a user on the server.
            /// </summary>
            public const string CreateUser = nameof(CreateUser);

            /// <summary>
            /// Sets Innodb Directories.
            /// </summary>
            public const string SetInnodbDirectories = nameof(SetInnodbDirectories);

            /// <summary>
            /// Increases MySQL memory buffer.
            /// </summary>
            public const string PrepareInMemoryScenario = nameof(PrepareInMemoryScenario);
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
