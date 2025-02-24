// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for downloading and installing snap packages
    /// on the system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class SnapPackageInstallation : VirtualClientComponent
    {
        private const string SnapCommand = "snap";
        private const string SystemCtlCommand = "systemctl";
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SnapPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public SnapPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The retry policy to apply to package install for handling transient errors.
        /// </summary>
        public IAsyncPolicy InstallRetryPolicy { get; set; } = Policy
            .Handle<WorkloadException>(exc => exc.Reason == ErrorReason.DependencyInstallationFailed)
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2));

        /// <summary>
        /// The name of the Snap package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SnapPackageInstallation.Packages), string.Empty).Trim();
            }

            set
            {
                this.Parameters[nameof(SnapPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Bool to check if installer should upgrade packages as well.
        /// </summary>
        public bool AllowUpgrades
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SnapPackageInstallation.AllowUpgrades), true);
            }
        }

        /// <summary>
        /// Executes the Snap package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);
            telemetryContext.AddContext("allowUpgrades", this.AllowUpgrades);

            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            // Snap installation only applies to Linux.
            if (this.Platform != PlatformID.Unix || packages == null || !packages.Any())
            {
                return;
            }

            await this.SetupSnapdSockets(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            // Determine which packages should be installed, and which can be skipped.
            List<string> toInstall = new List<string>();
            foreach (string package in packages)
            {
                if (!this.AllowUpgrades && await this.IsPackageInstalledAsync(package, telemetryContext, cancellationToken))
                {
                    this.Logger.LogTraceMessage($"Package '{package}' is already installed, skipping.", EventContext.Persisted());
                }
                else
                {
                    toInstall.Add(package);
                }
            }

            // Nothing to install.
            if (toInstall.Count == 0)
            {
                return;
            }

            string formattedArguments = $"install {string.Join(' ', toInstall)}";

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs Snap update first.
                using (IProcessProxy process = await this.ExecuteCommandAsync(SnapPackageInstallation.SnapCommand, $"refresh", Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait(false))
                {
                    process.ThrowIfDependencyInstallationFailed();
                }

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                using (IProcessProxy process = await this.ExecuteCommandAsync(SnapPackageInstallation.SnapCommand, formattedArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    process.ThrowIfDependencyInstallationFailed(process.StandardError.ToString());
                }
            }).ConfigureAwait(false);

            this.Logger.LogTraceMessage($"VirtualClient installed Snap package(s): '[{string.Join(' ', toInstall)}]'.", EventContext.Persisted());

            // Then, confirms that the packages were installed.
            List<string> failedPackages = toInstall.Where(package => !(this.IsPackageInstalledAsync(package, telemetryContext, cancellationToken).GetAwaiter().GetResult())).ToList();
            if (failedPackages?.Count > 0)
            {
                throw new ProcessException(
                    $"Packages were supposedly successfully installed, but cannot be found! Packages: '{string.Join(", ", failedPackages)}'",
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        private async Task SetupSnapdSockets(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var linuxDistributionInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

            List<string> snapdSocketCommands = new List<string>();

            // for ubuntu, debian, centos8, rhel8, etc. no socket enabling needed

            switch (linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.CentOS7:
                case LinuxDistribution.RHEL7:
                    snapdSocketCommands.Add($"enable --now snapd.socket");
                    break;
                case LinuxDistribution.SUSE:
                    snapdSocketCommands.Add($"enable --now snapd");
                    snapdSocketCommands.Add($"enable --now snapd.apparmor");
                    break;
                default:
                    break;
            }

            foreach (string command in snapdSocketCommands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(SnapPackageInstallation.SystemCtlCommand, command, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait(false))
                {
                    process.ThrowIfDependencyInstallationFailed();
                }
            }
        }

        private async Task<bool> IsPackageInstalledAsync(string packageName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = await this.ExecuteCommandAsync(SnapPackageInstallation.SnapCommand, $"list {packageName}", Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true)
                .ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    process.ThrowIfDependencyInstallationFailed();
                }

                return process.ExitCode == 0;
            }
        }
    }
}
