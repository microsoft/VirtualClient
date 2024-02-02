// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Executes the Blender Benchmark workload.
    /// </summary>
    [WindowsCompatible]
    public class BlenderBenchmarkExecutor : VirtualClientComponent
    {
        private const string BlenderBenchmarkExecutableName = "benchmark-launcher-cli.exe";
        private IFileSystem fileSystem;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="BlenderBenchmarkExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public BlenderBenchmarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// The scenes that will be run by Blender Benchmark.
        /// </summary>
        public string[] Scenes
        {
            get
            {
                // Trim and remove any whitespaces
                return this.Parameters.GetValue<string>(nameof(BlenderBenchmarkExecutor.Scenes)).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// The blender version being used by the Blender benchmark.
        /// </summary>
        public string BlenderVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderBenchmarkExecutor.BlenderVersion));
            }
        }

        /// <summary>
        /// The device types to be tested on.
        /// </summary>
        public string[] DeviceTypes
        {
            get
            {
                // Trim and remove any whitespaces
                return this.Parameters.GetValue<string>(nameof(BlenderBenchmarkExecutor.DeviceTypes)).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// The path to the blender-benchmark-cli.exe.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the Blender Benchmark package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, BlenderBenchmarkExecutableName);

            await this.InitializeBlenderBenchmarkAsync(telemetryContext, cancellationToken).
                ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the Blender Benchmark workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandArguments;

            // Run blender Benchmark over each scene and device type combination
            foreach (string deviceType in this.DeviceTypes)
            {
                foreach (string scene in this.Scenes)
                {
                    commandArguments = this.GenerateCommandArgument(scene, deviceType);

                    EventContext relatedContext = telemetryContext.Clone()
                    .AddContext("executable", this.ExecutablePath)
                    .AddContext("commandArguments", commandArguments);
                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, commandArguments, this.Package.Path, relatedContext, cancellationToken).ConfigureAwait(false))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext);
                                process.ThrowIfWorkloadFailed();

                                // Blender Benchmark's results are outputted to Stdout directly as a json.
                                this.CaptureMetrics(process, commandArguments, relatedContext, process.StandardOutput.ToString(), scene);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && this.Platform == PlatformID.Win32NT
                && this.CpuArchitecture == Architecture.X64;

            if (!isSupported)
            {
                this.Logger.LogNotSupported("BlenderBenchmark", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Processes benchmark results
        /// </summary>
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext, string resultsContent, string scenario)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    if (resultsContent == null)
                    {
                        throw new WorkloadResultsException(
                            "No results output from Blender Benchmark",
                            ErrorReason.WorkloadResultsNotFound);
                    }

                    BlenderBenchmarkMetricsParser resultsParser = new BlenderBenchmarkMetricsParser(resultsContent);
                    var metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                           scenario,
                           workloadProcess.FullCommand());
                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        this.PackageName,
                        scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext,
                        toolVersion: this.Package.Version);
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(BlenderBenchmarkExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }
            }
        }

        /// <summary>
        /// Download the required Blender engine and the scenes that will be used by Blender benchmark.
        /// </summary>
        private async Task InitializeBlenderBenchmarkAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            State installationState = await this.stateManager.GetStateAsync<State>(nameof(BlenderBenchmarkExecutor), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
            {
                await this.DownloadBlenderAsync(telemetryContext, cancellationToken);
                await this.DownloadBlenderBenchmarkScenesAsync(telemetryContext, cancellationToken);
                await this.stateManager.SaveStateAsync(nameof(BlenderBenchmarkExecutor), new State(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Download Blender engine with the specified version for Blender benchmark.
        /// </summary>
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

        /// <summary>
        /// Download the required scenes.
        /// </summary>
        private async Task DownloadBlenderBenchmarkScenesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string scenes = string.Join(" ", this.Scenes);
            string downloadScenesCommandArguments = $"scenes download --blender-version {this.BlenderVersion} {scenes}";
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

        /// <summary>
        /// Validate the Blender Benchmark Package
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                this.Package = this.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
            }
        }

        /// <summary>
        /// Generate the Blender Benchmark Command Arguments for a specific scene and device type.
        /// </summary>
        private string GenerateCommandArgument(string scene, string deviceType)
        {
            // --verbosity 3 gives detailed error messsage if Blender fails to run
            return $"benchmark --blender-version {this.BlenderVersion} --device-type {deviceType} {scene} --json --verbosity 3";
        }
    }
}