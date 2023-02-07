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
    using Newtonsoft.Json.Linq;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// PostgreSQL Server executor.
    /// Creates a Database.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLServerExecutor : PostgreSQLExecutor
    {
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
        /// Path to server script
        /// </summary>
        protected string ServerScriptPath { get; set; }

        /// <summary>
        /// Number of Warehouses.
        /// </summary>
        protected long WarehouseCount { get; set; }

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
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.SetUpConfigurationAsync(cancellationToken)
                .ConfigureAwait(false);

            if (this.Platform == PlatformID.Unix)
            {
                this.ServerScriptPath = this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "createDBScript.sh");
                if (this.FileSystem.File.Exists(this.ServerScriptPath))
                {
                    this.FileSystem.File.Copy(
                        this.ServerScriptPath,
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDBScript.sh"),
                        true);
                }

                if (this.FileSystem.File.Exists(this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "createDB.tcl")))
                {
                    this.FileSystem.File.Copy(
                        this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "createDB.tcl"),
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                        true);
                }
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                if (this.FileSystem.File.Exists(this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "createDB.tcl")))
                {
                    this.FileSystem.File.Copy(
                        this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "createDB.tcl"),
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                        true);
                }
            }
        }

        /// <summary>
        /// Creates DB and postgreSQL server.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken);

            State state = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.CreatingDB
            });

            HttpResponseMessage response = await this.LocalApiClient.GetStateAsync(nameof(PostgreSQLState), cancellationToken)
                    .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                response = await this.LocalApiClient.CreateStateAsync(
                                    nameof(PostgreSQLState), JObject.FromObject(state), cancellationToken)
                                    .ConfigureAwait(false);

                string result = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);

                response.ThrowOnError<WorkloadException>();
            }

            if (this.Platform == PlatformID.Unix)
            {
                await this.ExecuteOnLinuxAsync(cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT) 
            {
                await this.ExecuteOnWindowsAsync(cancellationToken).ConfigureAwait(false);
            }

            await this.CreateOrUpdateClientParameters(cancellationToken).ConfigureAwait(false);

            response = await this.LocalApiClient.GetStateAsync(nameof(PostgreSQLState), cancellationToken)
                .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);
            var stateItem = responseContent.FromJson<Item<State>>();

            stateItem.Definition.Properties[nameof(PostgreSQLState)] = PostgreSQLState.DBCreated;

            response = await this.LocalApiClient.UpdateStateAsync(
                    stateItem.Id, JObject.FromObject(stateItem), cancellationToken)
                    .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();
        }

        /// <summary>
        /// Powershell script parameters {for server PostgreSQL}.
        /// </summary>
        /// <returns></returns>
        protected async Task SetParametersAsync(CancellationToken cancellationToken)
        {
            int logicalCores = Environment.ProcessorCount;

            // 10 virtual users/logical core.
            int numberOfVirtualUsers = logicalCores * 10;
            this.WarehouseCount = numberOfVirtualUsers * 5;
            this.NumOfVirtualUsers = numberOfVirtualUsers;

            this.Password = System.Guid.NewGuid().ToString();
            this.UserName = Environment.MachineName.ToLower();

            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                    @"<VIRTUALUSERS>",
                    $"{this.NumOfVirtualUsers}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                    @"<WAREHOUSECOUNT>",
                    $"{this.WarehouseCount}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                    @"<USERNAME>",
                    $"{this.UserName}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "createDB.tcl"),
                    @"<PASSWORD>",
                    $"{this.Password}",
                    cancellationToken).ConfigureAwait(false);
            Console.WriteLine("Updated parameters in createDB.tcl file");

        }

        private async Task ExecuteOnLinuxAsync(CancellationToken cancellationToken)
        {
            string restartCommand = $"sudo systemctl restart postgresql";
            string dropDBCommand = $"-u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(restartCommand, null, this.PostgreSQLInstallationPath, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(dropDBCommand, null, this.PostgreSQLInstallationPath, cancellationToken)
               .ConfigureAwait(false);

            await this.SetParametersAsync(cancellationToken).ConfigureAwait(false);

            string dropUserCommand = $"-u postgres psql -c \"DROP ROLE IF EXISTS {this.UserName};\"";
            string createUserCommand = $"-u postgres psql -c \"CREATE USER {this.UserName} PASSWORD '{this.Password}';\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(dropUserCommand, null, this.PostgreSQLInstallationPath, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>(createUserCommand, null, this.PostgreSQLInstallationPath, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>("bash createDBScript.sh", null, this.HammerDBPackagePath, cancellationToken)
            .ConfigureAwait(false);

        }

        private async Task ExecuteOnWindowsAsync(CancellationToken cancellationToken)
        {
            string restartCommandArg = $"restart -D \"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "data")}\"";
            string dropDBCommandArg = $"-U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin", "pg_ctl.exe")}", restartCommandArg, this.PostgreSQLInstallationPath, cancellationToken)
                .ConfigureAwait(false);

            Environment.SetEnvironmentVariable("PGPASSWORD", "postgres", EnvironmentVariableTarget.Process);
           
            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin", "psql.exe")}", dropDBCommandArg, this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin"), cancellationToken)
               .ConfigureAwait(false);

            await this.SetParametersAsync(cancellationToken).ConfigureAwait(false);

            string dropUserCommand = $"-U postgres -c \"DROP ROLE IF EXISTS {this.UserName};\"";
            string createUserCommand = $"-U postgres -c \"CREATE USER {this.UserName} PASSWORD '{this.Password}';\"";

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin", "psql.exe")}", dropUserCommand, this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin"), cancellationToken)
               .ConfigureAwait(false);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"{this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin", "psql.exe")}", createUserCommand, this.PlatformSpecifics.Combine(this.PostgreSQLInstallationPath, "bin"), cancellationToken)
               .ConfigureAwait(false);

            await this.ExecuteCommandAsync<PostgreSQLServerExecutor>($"{this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "hammerdbcli.bat")}", "auto createDB.tcl", this.HammerDBPackagePath, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a state for client parameters,which is be fetched by client for running.
        /// It contains credentials and other necessary parameters required by client to execute queries on the given DB.
        /// </summary>
        private async Task CreateOrUpdateClientParameters(CancellationToken cancellationToken)
        { 
            ISystemManagement systemManager = this.Dependencies.GetService<ISystemManagement>();

            var clientParameters = new PostgreSQLParameters()
            {
                UserName = this.UserName,
                Password = this.Password,
                WarehouseCount = this.WarehouseCount,
                NumOfVirtualUsers = this.NumOfVirtualUsers,
            };

            HttpResponseMessage response = await this.LocalApiClient.GetStateAsync(nameof(PostgreSQLParameters), cancellationToken)
                   .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                response = await this.LocalApiClient.CreateStateAsync(nameof(PostgreSQLParameters), JObject.FromObject(clientParameters), cancellationToken)
                    .ConfigureAwait(false);
                response.ThrowOnError<WorkloadException>();
            }
        }
    }
}