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
        private readonly IStateManager stateManager;

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
        /// Workload package path.
        /// </summary>
        protected string PostgreSqlInstallationPath { get; set; }

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

            DependencyPath postgreSQLPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            postgreSQLPackage.ThrowIfNull(this.PackageName);

            // The *.vcpkg definition is expected to contain definitions specific to each platform/architecture
            // where the PostgreSQL application is installed.
            // 
            // e.g.
            // "metadata": {
            //   "installationPath-linux-x64": "/etc/postgresql/14/main",
            //   "installationPath-linux-arm64": "/etc/postgresql/14/main",
            //   "installationPath-windows-x64": "C:\\Program Files\\PostgreSQL\\14",
            //   "installationPath-windows-arm64": "C:\\Program Files\\PostgreSQL\\14",
            // }
            string metadataKey = $"{PackageMetadata.InstallationPath}-{this.PlatformArchitectureName}";
            if (!postgreSQLPackage.Metadata.TryGetValue(metadataKey, out IConvertible installationPath))
            {
                throw new WorkloadException(
                    $"Missing installation path. The '{this.PackageName}' package registration is missing the required '{metadataKey}' " +
                    $"metadata definition. This is required in order to execute PostgreSQL operations from the location where the software is installed.",
                    ErrorReason.DependencyNotFound);
            }

            this.PostgreSqlInstallationPath = installationPath.ToString();

            // The path to the PostgreSQL 'bin' folder is expected to exist in the PATH environment variable
            // for the HammerDB toolset to work correctly.
            this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(this.PostgreSqlInstallationPath, "bin"), append: true);
            
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

            string arguments = $"{packageDirectory}/install-server.py";

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
