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
    /// Provides functionality for downloading and installing Zypper packages
    /// on the system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class ZypperPackageInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZypperPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ZypperPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// The name of the Zypper package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ZypperPackageInstallation.Packages), string.Empty).Trim();
            }

            set
            {
                this.Parameters[nameof(ZypperPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Repository to add, if not in the default sources.list.d
        /// It could only be add one by one. And could look like this: Zypper-add-repository 'deb http://myserver/path/to/repo stable myrepo'
        /// </summary>
        public string Repositories
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ZypperPackageInstallation.Repositories), string.Empty).Trim();
            }
        }

        /// <summary>
        /// Executes the Zypper package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);
            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();

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
                    await this.ExecuteCommandAsync(
                        "zypper", 
                        $"ar -f {repo}", 
                        Environment.CurrentDirectory, 
                        telemetryContext, 
                        cancellationToken,
                        runElevated: true);
                }
            }

            List<string> packagesToLock = new List<string>();

            try
            {
                await this.InstallRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!this.AllowUpgrades)
                    {
                        foreach (string package in packages)
                        {
                            if (!package.StartsWith("http"))
                            {
                                packagesToLock.Add(package);
                            }
                        }

                        await this.ExecuteCommandAsync(
                            "zypper",
                            $"addlock {string.Join(' ', packagesToLock)}",
                            Environment.CurrentDirectory,
                            telemetryContext,
                            cancellationToken,
                            runElevated: true);
                    }

                    // Runs Zypper update first.
                    await this.ExecuteCommandAsync(
                        "zypper",
                        "update",
                        Environment.CurrentDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true);

                    // Runs the installation command with retries and throws if the command fails after all
                    // retries are expended.
                    await this.ExecuteCommandAsync(
                        "zypper",
                        $"--non-interactive install -y {string.Join(' ', packages)}",
                        Environment.CurrentDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true);
                });
            }
            finally
            {
                if (packagesToLock?.Any() == true)
                {
                    await this.ExecuteCommandAsync(
                        "zypper",
                        $"removelock {string.Join(' ', packagesToLock)}",
                        Environment.CurrentDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true);
                }
            }

            this.Logger.LogTraceMessage($"Zypper packages installed: '[{string.Join(' ', packages)}]'.");
        }
    }
}
