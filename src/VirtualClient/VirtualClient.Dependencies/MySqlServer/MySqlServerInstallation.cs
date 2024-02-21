// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.MySqlServer
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
    public class MySQLServerInstallation : ExecuteCommand
    {
        private readonly IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MySQLServerInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MySQLServerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Installs MySQL
        /// </summary>
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
                        await this.InstallMySQLServerAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        await this.stateManager.SaveStateAsync(stateId, new InstallationState(this.Action), cancellationToken);
                        break;
                }
            }
        }

        private async Task InstallMySQLServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix) 
            {
                DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
                string packageDirectory = package.Path;

                LinuxDistributionInfo distributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);
                string distribution = distributionInfo.LinuxDistribution.ToString();

                string arguments = $"{packageDirectory}/install.py --distro {distribution}";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "python3",
                    arguments,
                    Environment.CurrentDirectory,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "MySQLServerInstallation", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                    }
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
