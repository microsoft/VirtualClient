// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installs chocolatey on Windows.
    /// on the system.
    /// </summary>
    public class ChocolateyInstallation : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChocolateyInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ChocolateyInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes the NuGet package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Win32NT)
            {
                ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

                if (!systemManagement.FileSystem.Directory.Exists(this.PlatformSpecifics.PackagesDirectory))
                {
                    systemManagement.FileSystem.Directory.CreateDirectory(this.PlatformSpecifics.PackagesDirectory);
                }

                // https://chocolatey.org/install
                // https://docs.chocolatey.org/en-us/choco/setup#more-install-options
                // Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

                string argument = "Set-ExecutionPolicy Bypass -Scope Process -Force; " +
                    "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; " +
                    "iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))";
                
                using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(
                        this.Platform, "powershell.exe", $"-Command {argument}", this.PlatformSpecifics.PackagesDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());

                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Chocolatey")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }

                // It will install to "%programdata%\chocolatey\bin\choco.exe"
                string programDataPath = this.PlatformSpecifics.GetEnvironmentVariable("ProgramData");
                DependencyPath package = new DependencyPath(this.PackageName, this.Combine(programDataPath, "chocolatey", "bin"));

                await systemManagement.PackageManager.RegisterPackageAsync(package, cancellationToken)
                    .ConfigureAwait(false);

                this.SetEnvironmentVariable(EnvironmentVariable.PATH, this.Combine(programDataPath, "chocolatey", "bin"), append: true);
                await this.RefreshEnvironmentVariablesAsync(cancellationToken);
            }
        }
    }
}
