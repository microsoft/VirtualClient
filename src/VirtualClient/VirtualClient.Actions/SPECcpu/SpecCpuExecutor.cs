// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Core;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The SpecCpu workload executor.
    /// </summary>
    [UnixCompatible]
    public class SpecCpuExecutor : VirtualClientComponent
    {
        private const string SpecCpuRunShell = "runspeccpu.sh";

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;
        private string tuning;

        /// <summary>
        /// Constructor for <see cref="SpecCpuExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecCpuExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;

            this.tuning = this.RunPeak ? "all" : "base";
        }

        /// <summary>
        /// The name of SPECcpu profile, e.g. intrate, fpspeed.
        /// </summary>
        public string SpecProfile
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SpecCpuExecutor.SpecProfile), out IConvertible profileName);
                return profileName?.ToString();
            }
        }

        /// <summary>
        /// The whether SPECcpu runs base tuning or base+peak tuning.
        /// </summary>
        public bool RunPeak
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SpecCpuExecutor.RunPeak));
            }
        }

        /// <summary>
        /// Base optimizing flags.
        /// Recommand Default:-g -O3 -march=native
        /// </summary>
        public string BaseOptimizingFlags
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecCpuExecutor.BaseOptimizingFlags), "-g -O3 -march=native");
            }
        }

        /// <summary>
        /// Compiler version
        /// </summary>
        public string CompilerVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecCpuExecutor.CompilerVersion));
            }
        }

        /// <summary>
        /// Peak optimizing flags.
        /// Recommand Default:-g -Ofast -march=native -flto
        /// </summary>
        public string PeakOptimizingFlags
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecCpuExecutor.PeakOptimizingFlags), "-g -Ofast -march=native -flto");
            }
        }

        /// <summary>
        /// The path to the SPECcpu package.
        /// </summary>
        protected string PackageDirectory { get; set; }

        /// <summary>
        /// Executes the SPECcpu workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandLineArguments = this.GetCommandLineArguments();

            DateTime startTime = DateTime.UtcNow;
            await this.ExecuteCommandAsync("bash", $"{SpecCpuExecutor.SpecCpuRunShell} \"{commandLineArguments}\"", this.PackageDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;

            if (!cancellationToken.IsCancellationRequested)
            {
                this.LogSpecCpuOutput(startTime, endTime, telemetryContext);

                await this.UploadSpecCpuLogsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the SPECcpu workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.PackageDirectory = workloadPackage.Path;

            string imageFile = this.GetIsoFilePath(workloadPackage);
            telemetryContext.AddContext(nameof(imageFile), imageFile);

            await this.SetupSpecCpuAsync(imageFile, telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        private string GetConfigurationFileName()
        {
            switch (this.CpuArchitecture)
            {
                // Windows is not supported. Modify this section if Windows is added.
                case Architecture.X64:
                    return "vc-linux-x64.cfg";

                case Architecture.Arm64:
                    return "vc-linux-arm64.cfg";

                default:
                    throw new NotSupportedException($"Current CPU architechture '{this.CpuArchitecture.ToString()}' is not supported for SPECcpu.");
            }
        }

        private string GetIsoFilePath(DependencyPath workloadPackage)
        {
            string[] isoFiles = this.fileSystem.Directory.GetFiles(workloadPackage.Path, "*.iso", SearchOption.TopDirectoryOnly);
            if (isoFiles?.Any() != true)
            {
                throw new DependencyException(
                    $"SPECcpu .iso/image file not found in the expected package directory path '{this.PackageDirectory}'.",
                    ErrorReason.DependencyNotFound);
            }
            else if (isoFiles.Length > 1)
            {
                throw new DependencyException(
                   $"Ambiguous scenario. Multiple SPECcpu .iso/image files were found in the expected package directory path '{this.PackageDirectory}'.",
                   ErrorReason.DependencyNotFound);
            }

            return isoFiles.First();
        }

        private async Task SetupSpecCpuAsync(string isoFilePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SpecCpuState state = await this.stateManager.GetStateAsync<SpecCpuState>($"{nameof(SpecCpuState)}", cancellationToken)
                ?? new SpecCpuState();

            if (!state.SpecCpuInitialized)
            {
                string mountPath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.GetPackagePath(), "speccpu_mount");
                this.fileSystem.Directory.CreateDirectory(mountPath);

                await this.ExecuteCommandAsync("mount", $"-t iso9660 -o ro,exec,loop {isoFilePath} {mountPath}", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("./install.sh", $"-f -d {this.PackageDirectory}", mountPath, telemetryContext, cancellationToken);
                await this.WriteSpecCpuConfigAsync(cancellationToken);
                await this.ExecuteCommandAsync("chmod", $"-R ugo=rwx {this.PackageDirectory}", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("umount", mountPath, this.PackageDirectory, telemetryContext, cancellationToken);

                state.SpecCpuInitialized = true;
            }

            await this.stateManager.SaveStateAsync<SpecCpuState>($"{nameof(SpecCpuState)}", state, cancellationToken);
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            string output = string.Empty;
            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.", EventContext.Persisted());

                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<SpecCpuExecutor>(process, relatedContext);
                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                }
            }
        }

        private void LogSpecCpuOutput(DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            // CPU2017.008.intrate.txt
            string results = this.PlatformSpecifics.Combine(this.PackageDirectory, "result");
            string[] outputFiles = this.fileSystem.Directory.GetFiles(results, "CPU2017.*.txt", SearchOption.TopDirectoryOnly);

            foreach (string file in outputFiles)
            {
                string text = this.fileSystem.File.ReadAllText(file);
                SpecCpuMetricsParser parser = new SpecCpuMetricsParser(text);
                this.Logger.LogMetrics(
                    toolName: "SPECcpu",
                    scenarioName: "SPECcpu",
                    startTime,
                    endTime,
                    parser.Parse(),
                    metricCategorization: $"{this.SpecProfile}-{this.tuning}",
                    scenarioArguments: this.GetCommandLineArguments(),
                    this.Tags,
                    telemetryContext);

                this.fileSystem.File.Delete(file);
            }
        }

        private async Task UploadSpecCpuLogsAsync(CancellationToken cancellationToken)
        {
            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                // CPU2017.001.log, CPU2017.001.log.debug, etc
                string results = this.PlatformSpecifics.Combine(this.PackageDirectory, "result");
                string[] outputFiles = this.fileSystem.Directory.GetFiles(results, "*CPU2017*", SearchOption.TopDirectoryOnly);

                IEnumerable<KeyValuePair<string, BlobDescriptor>> fileBlobDescriptorPairs = BlobDescriptor.ToBlobDescriptors(
                    this.ExperimentId,
                    this.AgentId,
                    "speccpu",
                    outputFiles);

                await this.UploadFilesAsync(blobManager, this.fileSystem, fileBlobDescriptorPairs, cancellationToken).ConfigureAwait(false);
            }
        }

        private string GetCommandLineArguments()
        {
            // runcpu arguments document: https://www.spec.org/cpu2017/Docs/runcpu.html#strict
            string configurationFile = this.GetConfigurationFileName();
            int coreCount = this.systemManager.GetSystemCoreCount();
            return @$"--config {configurationFile} --iterations 2 --copies {coreCount} --threads {coreCount} --tune {this.tuning} --reportable {this.SpecProfile}";
        }

        private async Task WriteSpecCpuConfigAsync(CancellationToken cancellationToken)
        {
            // Copy SPECcpu configuration file to the config folder.
            string configurationFile = this.GetConfigurationFileName();
            string templateText = await this.fileSystem.File.ReadAllTextAsync(this.PlatformSpecifics.GetScriptPath("speccpu", configurationFile));

            // Copy SPECcpu run shell to the config folder.
            this.fileSystem.File.Copy(
                this.PlatformSpecifics.GetScriptPath("speccpu", SpecCpuExecutor.SpecCpuRunShell),
                this.Combine(this.PackageDirectory, SpecCpuExecutor.SpecCpuRunShell),
                true);

            templateText = templateText.Replace(SpecCpuConfigPlaceHolder.BaseOptimizingFlags, this.BaseOptimizingFlags, StringComparison.OrdinalIgnoreCase);
            templateText = templateText.Replace(SpecCpuConfigPlaceHolder.PeakOptimizingFlags, this.PeakOptimizingFlags, StringComparison.OrdinalIgnoreCase);
            templateText = templateText.Replace(
                SpecCpuConfigPlaceHolder.Gcc10Workaround,
                Convert.ToInt32(this.CompilerVersion) >= 10 ? SpecCpuConfigPlaceHolder.Gcc10WorkaroundContent : string.Empty,
                StringComparison.OrdinalIgnoreCase);

            await this.fileSystem.File.WriteAllTextAsync(this.Combine(this.PackageDirectory, "config", configurationFile), templateText, cancellationToken).ConfigureAwait(false);
        }

        internal class SpecCpuState : State
        {
            public SpecCpuState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool SpecCpuInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(SpecCpuState.SpecCpuInitialized), false);
                }

                set
                {
                    this.Properties[nameof(SpecCpuState.SpecCpuInitialized)] = value;
                }
            }
        }

        private static class SpecCpuConfigPlaceHolder
        {
            public const string BaseOptimizingFlags = "$BaseOptimizingFlags$";
            public const string PeakOptimizingFlags = "$PeakOptimizingFlags$";
            public const string Gcc10Workaround = "$Gcc10Workaround$";
            public const string Gcc10WorkaroundContent = "%define GCCge10";
        }
    }
}