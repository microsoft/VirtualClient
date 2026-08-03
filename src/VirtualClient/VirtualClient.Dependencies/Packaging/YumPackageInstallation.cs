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
    /// Provides functionality for downloading and installing Yum packages
    /// on the system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class YumPackageInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="YumPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public YumPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// The name of the Yum package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(YumPackageInstallation.Packages), string.Empty).Trim();
            }

            set
            {
                this.Parameters[nameof(YumPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Repository to add, if not in the default sources.list.d
        /// It could only be add one by one. And could look like this: Yum-add-repository 'deb http://myserver/path/to/repo stable myrepo'
        /// </summary>
        public string Repositories
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(YumPackageInstallation.Repositories), string.Empty).Trim();
            }
        }

        /// <summary>
        /// The name of the Yum package to download and install from the feed.
        /// </summary>
        public bool AllowUpgrades
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(YumPackageInstallation.AllowUpgrades), true);
            }
        }

        /// <summary>
        /// Executes the Yum package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);
            telemetryContext.AddContext("allowUpgrades", this.AllowUpgrades);

            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Yum installtion only applies to Linux.
            if (packages?.Any() != true)
            {
                return;
            }

            if (!string.IsNullOrEmpty(this.Repositories))
            {
                List<string> repos = this.Packages.Split(',', ';').ToList();
                // Repo could only be add one by one
                foreach (string repo in repos)
                {
                    // https://www.redhat.com/sysadmin/add-yum-repository
                    await this.ExecuteCommandAsync(
                        "yum-config-manager", 
                        $"--enable {repo} -y", 
                        Environment.CurrentDirectory, 
                        telemetryContext, 
                        cancellationToken,
                        runElevated: true);
                }
            }

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs Yum update first.
                await this.ExecuteCommandAsync(
                    "yum", 
                    "update -y", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken,
                    runElevated: true);

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                await this.ExecuteCommandAsync(
                    "yum",
                    $"install {string.Join(' ', packages)} -y --quiet{(this.AllowUpgrades ? string.Empty : " --no-upgrade")}", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken,
                    runElevated: true);

            });

            this.Logger.LogTraceMessage($"YUM packages installed: '[{string.Join(' ', packages)}]'.");
        }
    }
}
