namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for the MS-MPI.
    /// </summary>
    public class MSMPIInstallation : VirtualClientComponent
    {
        private const string LockFileName = "msmpisuccess.lock";

        private ISystemManagement systemManagement;
        private IPackageManager packageManager;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSMPIInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MSMPIInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;rge
            this.processManager = this.systemManagement.ProcessManager;
        }

        /// <summary>
        /// Installs the MS-MPI.
        /// </summary>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(
                    this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None)
                    .ConfigureAwait(false);

                string packageDirectory = workloadPackage.Path;

                string osuBenchmarkPath = this.PlatformSpecifics.Combine(packageDirectory, this.PackageName);
                string lockFile = this.PlatformSpecifics.Combine(packageDirectory, MSMPIInstallation.LockFileName);
                IFile fileInterface = this.systemManagement.FileSystem.File;

                if (!fileInterface.Exists(lockFile))
                {
                    string msmpiBin = this.PlatformSpecifics.Combine(packageDirectory, "bin");
                    this.SetEnvironmentVariable(EnvironmentVariable.PATH, msmpiBin, EnvironmentVariableTarget.Machine, true);

                    FirewallEntry firewallEntry = new FirewallEntry(
                    $"Virtual Client: Allow {this.PlatformSpecifics.Combine(msmpiBin, "smpd.exe")}",
                    $"Allows smpd to Communicate in Multiple Machine Scenario.",
                    this.PlatformSpecifics.Combine(msmpiBin, "smpd.exe"));

                    await this.systemManagement.FirewallManager.EnableInboundAppAsync(firewallEntry, cancellationToken)
                        .ConfigureAwait(false);

                    await this.InstallComponentAsync(
                        this.PlatformSpecifics.Combine(packageDirectory, "msmpisdk.msi"),
                        telemetryContext,
                        cancellationToken)
                        .ConfigureAwait(false);

                    fileInterface.Create(lockFile);
                }
            }
        }

        private async Task InstallComponentAsync(string msiPath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy installationProcess = this.processManager.CreateProcess("msiexec.exe", $"/i \"{msiPath}\" /qn"))
            {
                await installationProcess.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(installationProcess, telemetryContext.Clone(), logToFile: true)
                        .ConfigureAwait(false);
                    installationProcess.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}
