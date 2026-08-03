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

            if (packages?.Any() != true)
            {
                return;
            }

            if (!string.IsNullOrEmpty(this.Repositories))
            {
                List<string> repos = this.Packages.Split(',', ';').ToList();

                foreach (string repo in repos)
                {
                    // https://dnf-plugins-core.readthedocs.io/en/latest/config_manager.html
                    await this.ExecuteCommandAsync(
                        "dnf", 
                        $"config-manager --add-repo {repo} -y", 
                        Environment.CurrentDirectory, 
                        telemetryContext, 
                        cancellationToken,
                        runElevated: true);
                }
            }

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs Dnf update first.
                await this.ExecuteCommandAsync(
                    "dnf", 
                    "check-update -y", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken);

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                await this.ExecuteCommandAsync(
                    "dnf",
                    $"install {string.Join(' ', packages)} -y --quiet{(this.AllowUpgrades ? string.Empty : " --no-upgrade")}", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken);
            });

            this.Logger.LogTraceMessage($"DNF packages installed: '[{string.Join(' ', packages)}]'.", EventContext.Persisted());
        }
    }
}
