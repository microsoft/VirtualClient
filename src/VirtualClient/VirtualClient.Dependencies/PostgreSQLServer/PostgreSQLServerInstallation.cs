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
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.MySqlServer;

    /// <summary>
    /// Provides functionality for installing specific version of PostgreSQL.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLServerInstallation : ExecuteCommand
    {
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLServerInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PostgreSQLServerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
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
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Initializes PostgreSQL installation requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform != PlatformID.Unix && this.Platform != PlatformID.Win32NT)
            {
                throw new WorkloadException($"Unsupported platform. The platform '{this.Platform}' is not supported.", ErrorReason.NotSupported);
            }

            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        break;

                    default:
                        throw new WorkloadException(
                            $"PostgreSQL installation is not supported by Virtual Client on the current Unix/Linux distro '{distroInfo.LinuxDistribution}'.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        /// <summary>
        /// Executes PostgreSQL installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ProcessManager manager = this.SystemManager.ProcessManager;
            string stateId = $"{nameof(MySQLServerInstallation)}-{this.Action}-action-success";
            InstallationState installationState = await this.stateManager.GetStateAsync<InstallationState>($"{nameof(InstallationState)}", cancellationToken)
                .ConfigureAwait(false);

            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            workloadPackage.ThrowIfNull(this.PackageName);

            telemetryContext.AddContext(nameof(installationState), installationState);

            if (installationState == null && !this.SkipInitialize)
            {
                switch (this.Action)
                {
                    case InstallationAction.InstallServer:
                        await this.InstallPostgreSQLServerAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        await this.stateManager.SaveStateAsync(stateId, new InstallationState(this.Action), cancellationToken);
                        break;
                }
            }
        }

        private async Task InstallPostgreSQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            string packageDirectory = package.Path;

            string arguments = $"{packageDirectory}/installServer.py";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "python3",
                    arguments,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLServerInstallation", logToFile: true);
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }
        }

        /// <summary>
        /// Supported MySQL Server installation actions.
        /// </summary>
        internal class InstallationAction
        {
            /// <summary>
            /// Setup the required configurations of the SQL Server.
            /// </summary>
            public const string InstallServer = nameof(InstallServer);

        }

        internal class InstallationState
        {
            [JsonConstructor]
            public InstallationState(string action)
            {
                this.Action = action;
            }

            [JsonProperty("action")]
            public string Action { get; }
        }
    }
}
