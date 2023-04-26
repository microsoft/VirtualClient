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
                if (!this.AllowUpgrades && await this.IsPackageInstalledAsync(package, cancellationToken))
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
                await this.ExecuteCommandAsync(SnapPackageInstallation.SnapCommand, $"refresh", Environment.CurrentDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                await this.ExecuteCommandAsync(SnapPackageInstallation.SnapCommand, formattedArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            this.Logger.LogTraceMessage($"VirtualClient installed Snap package(s): '[{string.Join(' ', toInstall)}]'.", EventContext.Persisted());

            // Then, confirms that the packages were installed.
            List<string> failedPackages = toInstall.Where(package => !(this.IsPackageInstalledAsync(package, cancellationToken).GetAwaiter().GetResult())).ToList();
            if (failedPackages?.Count > 0)
            {
                throw new ProcessException(
                    $"Packages were supposedly successfully installed, but cannot be found! Packages: '{string.Join(", ", failedPackages)}'",
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        /// <inheritdoc />
        protected override bool IsSupported()
        {
            bool shouldExecute = false;
            if (base.IsSupported())
            {
                shouldExecute = this.Platform == PlatformID.Unix;
            }

            return shouldExecute;
        }

        private async Task SetupSnapdSockets(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var linuxDistributionInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

            // for ubuntu, debian, centos8, rhel8, etc. no socket enabling needed

            switch (linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.CentOS7:
                case LinuxDistribution.RHEL7:
                    await this.ExecuteCommandAsync(SnapPackageInstallation.SystemCtlCommand, $"enable --now snapd.socket", Environment.CurrentDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
                case LinuxDistribution.SUSE:
                    await this.ExecuteCommandAsync(SnapPackageInstallation.SystemCtlCommand, $"enable --now snapd", Environment.CurrentDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteCommandAsync(SnapPackageInstallation.SystemCtlCommand, $"enable --now snapd.apparmor", Environment.CurrentDirectory, telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    break;
            }
        }

        private async Task<bool> IsPackageInstalledAsync(string packageName, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, SnapPackageInstallation.SnapCommand, $"list {packageName}"))
            {
                this.CleanupTasks.Add(() => process.SafeKill());

                await process.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, EventContext.Persisted(), logToFile: true);
                    process.ThrowIfDependencyInstallationFailed();
                }

                return process.ExitCode == 0;
            }
        }

        private Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                string output = string.Empty;
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.", EventContext.Persisted());

                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }
    }
}
