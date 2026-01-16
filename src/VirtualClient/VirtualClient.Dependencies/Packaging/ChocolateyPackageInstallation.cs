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
    /// Provides functionality for downloading and installing Choco packages
    /// on the system.
    /// </summary>
    [SupportedPlatforms("win-arm64,win-x64")]
    public class ChocolateyPackageInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;
        private string chocoDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChocolateyPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ChocolateyPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// The name of the Choco package to download and install from the feed.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ChocolateyPackageInstallation.Packages)).Trim();
            }

            set
            {
                this.Parameters[nameof(ChocolateyPackageInstallation.Packages)] = value;
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the AspNetBench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath chocoPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (chocoPackage == null)
            {
                throw new DependencyException(
                    $"The expected chocolatey package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.chocoDirectory = chocoPackage.Path;
        }

        /// <summary>
        /// Executes the Choco package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packages", this.Packages);

            List<string> packages = this.Packages.Split(',', ';', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Choco installation only applies to Windows.
            if (this.Platform != PlatformID.Win32NT || packages == null || !packages.Any())
            {
                return;
            }

            // https://docs.chocolatey.org/en-us/choco/commands/install
            //  --dir {this.PlatformSpecifics.PackagesDirectory} This option seems to require a choco license on win11, still researching.
            string formattedArguments = $"install {string.Join(' ', packages)} --yes";
            string chocoExePath = this.Combine(this.chocoDirectory, "choco.exe");

            await this.InstallRetryPolicy.ExecuteAsync(async () =>
            {
                // Runs the installation command with retries and throws if the command fails after all
                // retries are expended.
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, chocoExePath, formattedArguments))
                {
                    this.CleanupTasks.Add(() => process.SafeKill(this.Logger));

                    await process.StartAndWaitAsync(cancellationToken)
                       .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Chocolatey")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            }).ConfigureAwait(false);

            // The environment variable needs to be reloaded after installation. There is a script that comes with chocolatey(RefreshEnv.cmd)
            await this.RefreshEnvironmentVariablesAsync(cancellationToken);

            // Directly adding the packages to path in here.
            foreach (string package in packages)
            {
                string programFilesPath = this.PlatformSpecifics.GetEnvironmentVariable("ProgramFiles");

                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(programFilesPath, package), append: true);
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(programFilesPath, package, "bin"), append: true);
            }

            // choco list doesn't work well enough and is going through a rename/deprecating
            // https://docs.chocolatey.org/en-us/choco/commands/list
            // Need to add the list/verify function once chocolatey releases 2.0
            this.Logger.LogTraceMessage($"VirtualClient installed choco package(s): '[{string.Join(' ', packages)}]'.", EventContext.Persisted());
        }
    }
}
