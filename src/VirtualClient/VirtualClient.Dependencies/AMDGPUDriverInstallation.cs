// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies.Packaging;

    /// <summary>
    /// Installation component for AMD GPU Drivers
    /// </summary>
    public class AMDGPUDriverInstallation : DependencyPackageInstallation
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="AMDGPUDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public AMDGPUDriverInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Installs the AMD GPU Driver, overwriting any existing installation
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.ExecuteAsync(telemetryContext, cancellationToken);

            DependencyPath package = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                   .ConfigureAwait(false);

            if (package == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            DependencyPath packagePath = this.PlatformSpecifics.ToPlatformSpecificPath(package, this.Platform, this.CpuArchitecture);

            string executablePath = this.PlatformSpecifics.Combine(packagePath.Path, "Setup.exe");

            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(executablePath, $"-INSTALL -OUTPUT screen"))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());

                try
                {
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.Logger.LogProcessDetails<AMDGPUDriverInstallation>(process, telemetryContext);
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                    }
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            }
        }
    }
}
