// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Numerics.Distributions;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for altering the superuser password for PostgreSQL Server.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLServerAlterPassword : ExecuteCommand
    {
        private ISystemManagement systemManager;
        private IPackageManager packageManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerAlterPassword"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PostgreSQLServerAlterPassword(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.systemManager.ThrowIfNull(nameof(this.systemManager));
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// Parameter defines the password of the superuser for the PostgreSQL Server.
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
        /// The path to the PostgreSQL package for altering password.
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// Initializes PostgreSQL alter password requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.PackagePath = package.Path;
        }

        /// <summary>
        /// Executes PostgreSQL alter password steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            AlterPasswordState state = await this.stateManager.GetStateAsync<AlterPasswordState>(nameof(PostgreSQLServerAlterPassword), cancellationToken);

            if (state == null)
            {
                await this.AlterPasswordAsync(telemetryContext, cancellationToken);

                await this.stateManager.SaveStateAsync(
                    nameof(PostgreSQLServerAlterPassword),
                    new Item<AlterPasswordState>(nameof(PostgreSQLServerAlterPassword), new AlterPasswordState()),
                    cancellationToken);
            }
        }

        private async Task AlterPasswordAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string alterPasswordScript = this.Combine(this.PackagePath, "alterPassword.py");

            string command = "python3";
            string commandArguments = $"{alterPasswordScript} --password {this.Password}";
            string workingDirectory = this.Combine(this.PackagePath);

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments)
                .AddContext("workingDirectory", workingDirectory);

            await this.systemManager.MakeFileExecutableAsync(alterPasswordScript, this.Platform, cancellationToken);

            await this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, relatedContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        internal class AlterPasswordState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AlterPasswordState"/> object.
            /// </summary>
            public AlterPasswordState()
                : base()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="AlterPasswordState"/> object.
            /// </summary>
            [JsonConstructor]
            public AlterPasswordState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }
        }
    }
}
