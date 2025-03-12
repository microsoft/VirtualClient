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
    /// Provides functionality for downloading and installing Dnf packages
    /// on the system.
    /// https://man7.org/linux/man-pages/man8/dnf.8.html
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class DnfPackageInstallation : VirtualClientComponent
    {
        /// <summary>
        /// The list of exit codes that dnf could return.
        /// </summary>
        public static readonly IEnumerable<int> DnfSuccessfulCodes = new int[] { 0, 100 };

        private const string DnfCommand = "dnf";
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="DnfPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public DnfPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// The name of the Dnf package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DnfPackageInstallation.Packages), string.Empty).Trim();
            }

            set
            {
                this.Parameters[nameof(DnfPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Repository to add, if not in the default sources.list.d
        /// It could only be add one by one. And could look like this: Dnf-add-repository 'deb http://myserver/path/to/repo stable myrepo'
        /// </summary>
        public string Repositories
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DnfPackageInstallation.Repositories), string.Empty).Trim();
            }
        }

        /// <summary>
        /// The name of the Dnf package to download and install from the feed.
        /// </summary>
        public bool AllowUpgrades
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(DnfPackageInstallation.AllowUpgrades), true);
            }
        }

        /// <summary>
        /// Executes the Dnf package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);
            telemetryContext.AddContext("allowUpgrades", this.AllowUpgrades);

            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            // Dnf installtion only applies to Linux.
            if (this.Platform != PlatformID.Unix)
            {
                return;
            }

            var linuxDistribution = this.GetLinuxDistribution(telemetryContext);

            if (linuxDistribution != LinuxDistribution.AwsLinux && (packages == null || !packages.Any()))
            {
                return;
            }
            
            if (!string.IsNullOrEmpty(this.Repositories))
            {
                List<string> repos = this.Packages.Split(',', ';').ToList();
                // Repo could only be add one by one
                foreach (string repo in repos)
                {
                    // https://dnf-plugins-core.readthedocs.io/en/latest/config_manager.html
                    await this.ExecuteCommandAsync(DnfPackageInstallation.DnfCommand, $"config-manager --add-repo {repo} -y", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

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

            if (linuxDistribution == LinuxDistribution.AwsLinux)
            {
                toInstall.Add("iptables");
            }

            // Nothing to install.
            if (toInstall.Count == 0)
            {
                return;
            }

            string formattedArguments = $"install {string.Join(' ', toInstall)} -y --quiet{(this.AllowUpgrades ? string.Empty : " --no-upgrade")}";

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs Dnf update first.
                await this.ExecuteCommandAsync(DnfPackageInstallation.DnfCommand, $"check-update -y", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                await this.ExecuteCommandAsync(DnfPackageInstallation.DnfCommand, formattedArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            }).ConfigureAwait(false);

            this.Logger.LogTraceMessage($"VirtualClient installed Dnf package(s): '[{string.Join(' ', toInstall)}]'.", EventContext.Persisted());

            // Then, confirms that the packages were installed.
            List<string> failedPackages = toInstall.Where(package => !(this.IsPackageInstalledAsync(package, cancellationToken).GetAwaiter().GetResult())).ToList();

            if (failedPackages?.Count > 0)
            {
                throw new ProcessException(
                    $"Packages were supposedly successfully installed, but cannot be found! Packages: '{string.Join(", ", failedPackages)}'",
                    ErrorReason.DependencyInstallationFailed);
            }
        }

        private LinuxDistribution GetLinuxDistribution(EventContext telemetryContext)
        {
            try
            {
                var linuxDistributionInfo = this.systemManagement.GetLinuxDistributionAsync(CancellationToken.None).GetAwaiter().GetResult();

                telemetryContext.AddContext("LinuxDistribution", linuxDistributionInfo.LinuxDistribution.ToString());

                return linuxDistributionInfo.LinuxDistribution;
            }
            catch (Exception ex)
            {
                this.Logger.LogErrorMessage($"Failed to get Linux distribution information. Exception: {ex.Message}", ex, EventContext.Persisted());
                
                return LinuxDistribution.Unknown;
            }
        }

        private async Task<bool> IsPackageInstalledAsync(string packageName, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, DnfPackageInstallation.DnfCommand, $"list {packageName}"))
            {
                this.CleanupTasks.Add(() => process.SafeKill());

                await process.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, EventContext.Persisted(), "Dnf")
                        .ConfigureAwait(false);

                    process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
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
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Dnf")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(DnfPackageInstallation.DnfSuccessfulCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
