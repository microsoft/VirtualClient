// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for Blender Benchmark cli.
    /// </summary>
    public class BlenderInstallation : VirtualClientComponent
    {
        private IPackageManager packageManager;
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlenderInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public BlenderInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
        }

        /// <summary>
        /// The blender version that will be used by the benchmark.
        /// </summary>
        public string BlenderVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderInstallation.BlenderVersion));
            }
        }

        /// <summary>
        /// The scenes to be run
        /// </summary>
        public string Scenes
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderInstallation.Scenes));
             
            }
        }

        /// <summary>
        /// The name of the blender benchmark cli (e.g. benchmark-launcher-cli.exe)
        /// </summary>
        public string ExecutableName 
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderInstallation.ExecutableName));

            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Defines the path to the blenderbenchmarkcli package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// The path to the blender-benchmark-cli.exe.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, this.ExecutableName);
        }

        /// <summary>
        /// Executes `Blender installation steps.
        /// </summary>
        /// <returns></returns>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath amdDriverInstallerPackage = await this.packageManager.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);

            if (amdDriverInstallerPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            // TODO: do we need await in here?
            await this.DownloadBlenderAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            await this.DownloadScenesAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Validate the blenderbenchmarkcli Package
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                this.Package = workloadPackage;
            }
        }

        private async Task DownloadBlenderAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string downloadBlenderCommandArguments = $"blender download {this.BlenderVersion}";
            EventContext relatedContext = telemetryContext.Clone().AddContext("commandArguments", downloadBlenderCommandArguments);

            using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, downloadBlenderCommandArguments, this.Package.Path, relatedContext, cancellationToken).ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfWorkloadFailed();
                }
            }
        }

        private async Task DownloadScenesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string downloadScenesCommandArguments = $"scenes download --blender-version {this.BlenderVersion} {this.Scenes}";
            EventContext relatedContext = telemetryContext.Clone().AddContext("commandArguments", downloadScenesCommandArguments);

            using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, downloadScenesCommandArguments, this.Package.Path, relatedContext, cancellationToken).ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfWorkloadFailed();
                }
            }
        }
    }
}