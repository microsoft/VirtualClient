namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
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
    public class MsmpiInstallation : VirtualClientComponent
    {
        private const string LockFileName = "msmpisuccess.lock";

        private ISystemManagement systemManagement;
        private IPackageManager packageManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MsmpiInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MsmpiInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Installs the MS-MPI.
        /// </summary>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None);

            string packageDirectory = workloadPackage.Path;

            State installationState = await this.stateManager.GetStateAsync<State>(nameof(MsmpiInstallation), cancellationToken);

            if (installationState == null)
            {
                string msmpiBin = this.PlatformSpecifics.Combine(packageDirectory, "bin");
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, msmpiBin, EnvironmentVariableTarget.Machine, true);

                FirewallEntry firewallEntry = new FirewallEntry(
                $"Virtual Client: Allow {this.PlatformSpecifics.Combine(msmpiBin, "smpd.exe")}",
                $"Allows smpd to Communicate in Multiple Machine Scenario.",
                this.PlatformSpecifics.Combine(msmpiBin, "smpd.exe"));

                await this.systemManagement.FirewallManager.EnableInboundAppAsync(firewallEntry, cancellationToken);

                string msiPath = this.PlatformSpecifics.Combine(packageDirectory, "msmpisdk.msi");
                await this.ExecuteCommandAsync("msiexec.exe", $"/i \"{msiPath}\" /qn", packageDirectory, telemetryContext, cancellationToken);

                await this.stateManager.SaveStateAsync(nameof(MsmpiInstallation), new State(), cancellationToken);
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported() && this.Platform == PlatformID.Win32NT && this.CpuArchitecture == Architecture.X64;

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Msmpi", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }
    }
}
