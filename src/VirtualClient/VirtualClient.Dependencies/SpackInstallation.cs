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

    /// <summary>
    /// Installation component for Spack
    /// </summary>
    public class SpackInstallation : VirtualClientComponent
    {
        private string spackDirectory;
        private string spackExecutablePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpackInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public SpackInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            this.SystemManager = dependencies.GetService<ISystemManagement>();
            this.spackDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, this.PackageName.ToLowerInvariant());
            this.spackExecutablePath = this.PlatformSpecifics.Combine(this.spackDirectory, "bin", "spack");
        }

        /// <summary>
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Installs Spack9
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.InstallSpackAsync(cancellationToken, telemetryContext);
        }

        private async Task InstallSpackAsync(CancellationToken cancellationToken, EventContext telemetryContext)
        {
            State installationState = await this.SystemManager.StateManager.GetStateAsync<State>(nameof(SpackInstallation), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
            {
                DependencyPath spackPackage = new DependencyPath(
                this.PackageName,
                this.spackDirectory,
                "Spack Package Manager",
                metadata: new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, this.spackExecutablePath }
                });

                if (!this.SystemManager.FileSystem.Directory.Exists(this.spackDirectory))
                {
                    await this.ExecuteCommandAsync("git", $"clone https://github.com/spack/spack {this.spackDirectory}", telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }

                await this.SystemManager.PackageManager.RegisterPackageAsync(spackPackage, cancellationToken)
                    .ConfigureAwait(false);

                await this.SystemManager.StateManager.SaveStateAsync(nameof(SpackInstallation), new State(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ExecuteCommandAsync(string command, string arguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments))
            {
                this.CleanupTasks.Add(() => process.SafeKill());

                await process.StartAndWaitAsync(cancellationToken, null)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext)
                        .ConfigureAwait(false);

                    process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}
