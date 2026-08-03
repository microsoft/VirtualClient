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
    /// Provides functionality for downloading and installing Apt packages
    /// on the system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class AptPackageInstallation : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AptPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public AptPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// The name of the Apt package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AptPackageInstallation.Packages), string.Empty).Trim();
            }

            set
            {
                this.Parameters[nameof(AptPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Repository to add, if not in the default sources.list.d
        /// It could only be add one by one. And could look like this: apt-add-repository 'deb http://myserver/path/to/repo stable myrepo'
        /// </summary>
        public string Repositories
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AptPackageInstallation.Repositories), string.Empty).Trim();
            }
        }

        /// <summary>
        /// Boolean value for allowing/disallowing upgrades.
        /// </summary>
        public bool AllowUpgrades
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(AptPackageInstallation.AllowUpgrades), true);
            }
        }

        /// <summary>
        /// Boolean value for installing interactive or not.
        /// </summary>
        public bool Interactive
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(AptPackageInstallation.Interactive), true);
            }
        }

        /// <summary>
        /// Executes the Apt package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);
            telemetryContext.AddContext("allowUpgrades", this.AllowUpgrades);
            telemetryContext.AddContext("interactive", this.Interactive);

            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (packages?.Any() != true)
            {
                return;
            }

            if (!string.IsNullOrEmpty(this.Repositories))
            {
                List<string> repos = this.Repositories.Split(',', ';').ToList();

                foreach (string repo in repos)
                {
                    await this.ExecuteCommandAsync(
                        "add-apt-repository", 
                        $"\"{repo}\" -y", 
                        Environment.CurrentDirectory, 
                        telemetryContext, 
                        cancellationToken, 
                        runElevated: true);
                }
            }

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs apt update first.
                await this.ExecuteCommandAsync(
                    "apt", 
                    $"update", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken, 
                    runElevated: true);

                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                await this.ExecuteCommandAsync(
                    this.Interactive ? "apt" : $"DEBIAN_FRONTEND=noninteractive apt",
                    $"install {string.Join(' ', packages)} --yes --quiet{(this.AllowUpgrades ? string.Empty : " --no-upgrade")}", 
                    Environment.CurrentDirectory, 
                    telemetryContext, 
                    cancellationToken, 
                    runElevated: true);
            });

            this.Logger.LogTraceMessage($"APT packages installed: '[{string.Join(' ', packages)}]'.", EventContext.Persisted());
        }
    }
}
