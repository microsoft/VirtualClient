// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
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
    public class MySQLServerConfiguration : VirtualClientComponent
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
            this.DatabaseName = parameters.GetValue<string>(nameof(MySQLServerConfiguration.DatabaseName));
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
        /// The name to use for the MySQL Database to manage.
        /// </summary>
        public string DatabaseName { get; }

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

            if (configurationState == null)
            {
                switch (this.Action)
                {
                    case ConfigurationAction.StartServer:
                        await this.StartMySQLServerAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case ConfigurationAction.CreateDatabase:
                        await this.CreateMySQLDatabase(telemetryContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case ConfigurationAction.RaisedStatementCount:
                        await this.RaiseMySQLMaximumStatementLimit(telemetryContext, cancellationToken).ConfigureAwait(false);
                        break;
                }

                await this.stateManager.SaveStateAsync(stateId, new ConfigurationState(this.Action), cancellationToken);
            }
        }

        private async Task StartMySQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            if (this.Platform == PlatformID.Win32NT)
            {
                // path to file when chocolatey installation of mysql
                await this.ExecuteCommandAsync(manager, $"{WindowsMySQLPackagePath}mysqld.exe", null, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Unix)
            {
                await this.ExecuteCommandAsync(manager, "systemctl start mysql.service", null, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
            }
        }

        private async Task CreateMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            if (this.Platform == PlatformID.Win32NT)
            {
                // need to know how to set up password and start server, get path
                await this.ExecuteCommandAsync(manager, $"{WindowsMySQLPackagePath}mysql.exe --execute=\"DROP DATABASE IF EXISTS {this.DatabaseName};\" --user=root", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync(manager, $"{WindowsMySQLPackagePath}mysql.exe --execute=\"CREATE DATABASE {this.DatabaseName};\" --user=root", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Unix)
            {
                await this.ExecuteCommandAsync(manager, $"mysql --execute=\"DROP DATABASE IF EXISTS {this.DatabaseName};\"", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync(manager, $"mysql --execute=\"CREATE DATABASE {this.DatabaseName};\"", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task RaiseMySQLMaximumStatementLimit(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            if (this.Platform == PlatformID.Win32NT)
            {
                await this.ExecuteCommandAsync(manager, $"{WindowsMySQLPackagePath}mysql.exe --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\" --user=root", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Unix)
            {
                await this.ExecuteCommandAsync(manager, $"mysql --execute=\"SET GLOBAL MAX_PREPARED_STMT_COUNT=100000;\"", null, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ExecuteCommandAsync(ProcessManager manager, string command, string arguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments))
            {
                this.CleanupTasks.Add(() => process.SafeKill());
                await process.StartAndWaitAsync(cancellationToken, null)
                    .ConfigureAwait(false);

                // process.ThrowIfErrored<WorkloadException>(
                //        ProcessProxy.DefaultSuccessCodes,
                //        $"SQL Server installation action '{this.Action}' failed. {process.StandardError}",
                //        ErrorReason.DependencyInstallationFailed);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<MySQLServerConfiguration>(process, telemetryContext);
                }

                process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
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
            public const string StartServer = nameof(ConfigurationAction.StartServer);

            /// <summary>
            /// Creates Temp DB.
            /// </summary>
            public const string CreateDatabase = nameof(ConfigurationAction.CreateDatabase);

            /// <summary>
            /// Increases max_prepared_stmt_count for maximum VM stress.
            /// </summary>
            public const string RaisedStatementCount = nameof(ConfigurationAction.RaisedStatementCount);
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
