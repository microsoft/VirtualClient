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
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteAMDDriverInstallation", telemetryContext, async () =>
            {
                State installationState = await this.stateManager.GetStateAsync<State>(nameof(AMDGPUDriverInstallation), cancellationToken)
                    .ConfigureAwait(false);

                if (installationState == null)
                {
                    if (this.Platform == PlatformID.Win32NT)
                    {
                        await this.AMDDriverInstallation(telemetryContext, cancellationToken)
                                   .ConfigureAwait(false);

                        await this.stateManager.SaveStateAsync(nameof(this.AMDDriverInstallation), new State(), cancellationToken)
                            .ConfigureAwait(false);

                    }

                    VirtualClientRuntime.IsRebootRequested = this.RebootRequired;
                }
            });
        }

        private async Task AMDDriverInstallation(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installerPath = string.Empty;

            DependencyPath amdDriverInstallerPackage = await this.packageManager.GetPackageAsync(
                this.PackageName, cancellationToken)
                    .ConfigureAwait(false);

            if (this.fileSystem.Directory.GetFiles(amdDriverInstallerPackage.Path, "*.exe", SearchOption.AllDirectories).Length > 0)
            {
                installerPath = this.fileSystem.Directory.GetFiles(amdDriverInstallerPackage.Path, "*.exe", SearchOption.AllDirectories)[0];
            }
            else
            {
                throw new DependencyException($"The installer file was not found in the directory {amdDriverInstallerPackage.Path}", ErrorReason.DependencyNotFound);
            }

            await this.ExecuteCommandAsync(installerPath, "/S /v/qn", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}