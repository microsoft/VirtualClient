namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for AMD GPU Drivers
    /// </summary>
    public class AMDGPUDriverInstallation : VirtualClientComponent
    {
        private const string Nvv4ExeName = "AMD-Azure-NVv4-Driver-22Q2.exe";
        private const string V620ExeName = "Setup.exe";
        private IPackageManager packageManager;
        private IFileSystem fileSystem;
        private ISystemManagement systemManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AMDGPUDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public AMDGPUDriverInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
            this.packageManager = this.systemManager.PackageManager;
        }

        /// <summary>
        /// Determines whether Reboot is required or not after Driver installation
        /// </summary>
        public bool RebootRequired
        {
            get
            {
                switch (this.Platform)
                {
                    case PlatformID.Win32NT:
                        return this.Parameters.GetValue<bool>(nameof(AMDGPUDriverInstallation.RebootRequired), false);
                    default:
                        return this.Parameters.GetValue<bool>(nameof(AMDGPUDriverInstallation.RebootRequired), true);
                }
            }
        }

        /// <summary>
        /// Vm series on which driver is installed. (e.g. nvv4, v620)
        /// </summary>
        public string VmSeries
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AMDGPUDriverInstallation.VmSeries), "nvv4");
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes  GPU driver installation steps.
        /// </summary>
        /// <returns></returns>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
            {
                State installationState = await this.stateManager.GetStateAsync<State>(nameof(AMDGPUDriverInstallation), cancellationToken)
                    .ConfigureAwait(false);

                if (installationState == null)
                {
                    if (this.Platform == PlatformID.Win32NT)
                    {
                        await this.InstallAMDGPUDriver(telemetryContext, cancellationToken)
                                   .ConfigureAwait(false);

                        await this.stateManager.SaveStateAsync(nameof(AMDGPUDriverInstallation), new State(), cancellationToken)
                            .ConfigureAwait(false);

                    }
                    else if (this.Platform == PlatformID.Unix)
                    {
                        throw new DependencyException(
                                $"AMD GPU Driver Installation is not supported by Virtual Client on the current platform '{this.Platform}'",
                                ErrorReason.PlatformNotSupported);
                    }

                    VirtualClientRuntime.IsRebootRequested = this.RebootRequired;
                }
            });
        }

        private async Task InstallAMDGPUDriver(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installerPath = string.Empty;

            DependencyPath amdDriverInstallerPackage = await this.packageManager.GetPackageAsync(
                this.PackageName, cancellationToken)
                    .ConfigureAwait(false);

            if (amdDriverInstallerPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            switch (this.VmSeries)
            {
                case "nvv4":
                    {
                        await this.InstallAMDGPUDriverOnNvv4(amdDriverInstallerPackage.Path, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    }

                case "v620":
                    {
                        await this.InstallAMDGPUDriverOnV620(amdDriverInstallerPackage.Path, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException($"VM Series '{this.VmSeries}' is not supported.");
                    }
            }            
        }

        private async Task InstallAMDGPUDriverOnNvv4(string driverPackagePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationFile = this.Combine(driverPackagePath, this.VmSeries, AMDGPUDriverInstallation.Nvv4ExeName);

            if (!this.fileSystem.File.Exists(installationFile))
            { 
                throw new DependencyException($"The installer file was not found in the directory {driverPackagePath}", ErrorReason.DependencyNotFound);
            }

            IProcessProxy process = await this.ExecuteCommandAsync(installationFile, "/S /v/qn", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait(false);
                process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
            }
        }

        private async Task InstallAMDGPUDriverOnV620(string driverPackagePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationFile = this.Combine(driverPackagePath, this.VmSeries, AMDGPUDriverInstallation.V620ExeName);

            if (!this.fileSystem.File.Exists(installationFile))
            {
                throw new DependencyException($"The installer file was not found in the directory {driverPackagePath}", ErrorReason.DependencyNotFound);
            }

            IProcessProxy process = await this.ExecuteCommandAsync(installationFile, "-INSTALL -OUTPUT screen", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait(false);
                process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
            }
        }
    }
}