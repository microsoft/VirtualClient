// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The MLPerf Training workload executor.
    /// </summary>
    [UnixCompatible]
    public class MLPerfTrainingExecutor : VirtualClientComponent
    {
        private const string AccuracySummary = nameof(MLPerfTrainingExecutor.AccuracySummary);
        private const string PerformanceSummary = nameof(MLPerfTrainingExecutor.PerformanceSummary);

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        private IDiskManager diskManager;

        private List<string> benchmarks;
        private Dictionary<string, string> scenarios;
        private Dictionary<string, List<string>> benchmarkConfigs;

        /// <summary>
        /// Constructor for <see cref="MLPerfTrainingExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MLPerfTrainingExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;

            this.fileSystem = this.systemManager.FileSystem;
            this.diskManager = this.systemManager.DiskManager;

            this.benchmarks = new List<string>
            {
                "bert"
            };

            if (string.IsNullOrEmpty(this.DiskFilter))
            {
                this.DiskFilter = "SizeGreaterThan:1000gb&OSDisk:false";
            }
        }

        /// <summary>
        /// Disk filter string to filter disks to format.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                // Change the size -----------------------------------------------------------------------------------------------------------------------------
                string filter = this.Parameters.GetValue<string>(nameof(MLPerfTrainingExecutor.DiskFilter), "SizeGreaterThan:1000gb");
                // Enforce filter to remove OS disk.
                filter = $"{filter}&OSDisk:false";
                return filter;
            }

            set
            {
                this.Parameters[nameof(MLPerfTrainingExecutor.DiskFilter)] = value;
            }
        }

        /// <summary>
        /// The MLPerf Training model name (e.g. bert, rnnt, ssd-mobilenet).
        /// </summary>
        public string Model
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Model));
            }
        }

        /// <summary>
        /// The current running user
        /// </summary>
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(MLPerfTrainingExecutor.Username));
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = this.GetCurrentUserName(true);
                }

                return username;
            }
        }

        /// <summary>
        /// The current running user
        /// </summary>
        public string BatchSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BatchSize));
            }
        }

        /// <summary>
        /// The current running user
        /// </summary>
        public string Implementation
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Implementation));
            }
        }

        /// <summary>
        /// The MLPerf Training Nvidia code directory.
        /// </summary>
        protected string NvidiaDirectoryPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "mlperf", "NVIDIA");
            }
        }

        /// <summary>
        /// The MLPerf Training Pytorch code directory.
        /// </summary>
        protected string ExecutionDirectoryPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.NvidiaDirectoryPath, this.Model, "implementations", this.Implementation);
            }
        }

        /// <summary>
        /// The MLPerf Training Nvidia data directory.
        /// </summary>
        protected string DataDirectoryPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, this.Model);
            }
        }

        /// <summary>
        /// The output directory of MLPerf.
        /// </summary>
        protected string OutputDirectoryPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.ExecutionDirectoryPath, "results");
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the MLPerf Training workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.InitializationStarted", telemetryContext);

            this.ThrowIfPlatformNotSupported();

            await this.ThrowIfUnixDistroNotSupportedAsync(cancellationToken)
                .ConfigureAwait(false);

            MLPerfState state = await this.stateManager.GetStateAsync<MLPerfState>($"{nameof(MLPerfState)}", cancellationToken)
                ?? new MLPerfState();

            if (!state.Initialized)
            {
                // add user in docker group
                await this.ExecuteCommandAsync("usermod", $"-aG docker {this.Username}", this.ExecutionDirectoryPath, cancellationToken);
                await this.ExecuteCommandAsync("newgrp", $"docker", this.ExecutionDirectoryPath, cancellationToken);

                // If GPUConfig is not included in the MLPerf code but is supported
                this.ReplaceGPUConfigFilesToSupportAdditionalGPUs();

                // Setup Environment
                await this.SetupEnvironmentAsync(cancellationToken);
                state.Initialized = true;
                await this.stateManager.SaveStateAsync<MLPerfState>($"{nameof(MLPerfState)}", state, cancellationToken);
            }
        }

        /// <summary>
        /// Creates setup for MLPerf Training workload.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task SetupEnvironmentAsync(CancellationToken cancellationToken)
        {
            string dockerImageCommand = $"docker build --pull -t {this.GetContainerName()} .";
            string configCommand = $"bash ./config_DGXA100_1x8x56x1.sh";
            string exportCUDACommand = $"export CUDA_VISIBLE_DEVICES=\"0,1,2,3,4,5,6,7\"";
            string exportBatchSizeCommand = $"export BATCHSIZE={this.BatchSize}";
            string suCommand = $"su";

            await this.ExecuteCommandAsync(
                "sudo",
                dockerImageCommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                configCommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                exportCUDACommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                exportBatchSizeCommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                suCommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

        }

        /// <summary>
        /// Gets the container name created by MLPerf Training.
        /// </summary>
        /// <returns>Container name created by MLPerf Training</returns>
        /// <exception cref="WorkloadException"></exception>
        protected string GetContainerName()
        {
            // Update this function to accomodate other architectures
            if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.X64)
            {
                return $"mlperf-training-{this.Username}-x86_64";
            }
            else if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.Arm64)
            {
                return $"mlperf-training-{this.Username}-arm64";
            }
            else
            {
                throw new WorkloadException(
                    $"The container name is not defined for the current platform/architecture " +
                    $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}.",
                    ErrorReason.PlatformNotSupported);
            }
        }

        /// <summary>
        /// Executes the MLPerf Training workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.PrepareBenchmarkConfigsAndScenarios();
            string shardsPath = this.PlatformSpecifics.Combine("/mlperfDataDrive", $"{this.Model}_data", "hdf5", "training-4320", "hdf5_4320_shards_varlength");
            string evalPath = this.PlatformSpecifics.Combine("/mlperfDataDrive", this.Model + "_data", "hdf5", "eval_varlength");
            string checkpointPath = this.PlatformSpecifics.Combine("/mlperfDataDrive", this.Model + "_data", "phase1");

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string execCommand = $"CONT={this.GetContainerName()} DATADIR={shardsPath} DATADIR_PHASE2={shardsPath} EVALDIR={evalPath} CHECKPOINTDIR={checkpointPath} CHECKPOINTDIR_PHASE1={checkpointPath} ./run_with_docker.sh";

                using (IProcessProxy process = await this.ExecuteCommandAsync("sudo bash", execCommand, this.ExecutionDirectoryPath, telemetryContext, cancellationToken)
                   .ConfigureAwait())
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext)
                            .ConfigureAwait();

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);

                        await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken, MLPerfTrainingExecutor.AccuracySummary)
                            .ConfigureAwait();
                    }
                }
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken, string context = null)
        {
            /*string[] resultsFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectoryPath, ".log", SearchOption.AllDirectories);

            foreach (string file in resultsFiles)
            {
                string results = await this.LoadResultsAsync(file, cancellationToken);
                await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf Training", results: results.AsArray(), logToFile: true);

                MLPerfTrainingMetricsParser parser = new MLPerfTrainingMetricsParser(results);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    "MLPerf Training",
                    this.Model,
                    process.StartTime,
                    process.ExitTime,
                    metrics,
                    "Executing",
                    null,
                    this.Tags,
                    telemetryContext);

                await this.fileSystem.File.DeleteAsync(file);
            }*/

            // Convert to string
            ConcurrentBuffer buffer = process.StandardOutput;

            string logs = string.Concat(buffer.ToString(), Environment.NewLine);

            // string results = await this.LoadResultsAsync(logs, cancellationToken);
            await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf Training", results: logs.AsArray(), logToFile: true);

            MLPerfTrainingMetricsParser parser = new MLPerfTrainingMetricsParser(logs);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "MLPerf Training",
                this.Model,
                process.StartTime,
                process.ExitTime,
                metrics,
                "Executing",
                null,
                this.Tags,
                telemetryContext);

            // await this.fileSystem.File.DeleteAsync(logs);
        }

        private void ReplaceGPUConfigFilesToSupportAdditionalGPUs()
        {
            foreach (string file in this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.GetScriptPath("mlperf", "GPUConfigFiles")))
            {
                this.fileSystem.File.Copy(
                    file,
                    this.Combine(this.NvidiaDirectoryPath, "code", "common", "systems", Path.GetFileName(file)),
                    true);
            }

            foreach (string directory in this.fileSystem.Directory.GetDirectories(
                this.PlatformSpecifics.GetScriptPath("mlperf", "GPUConfigFiles"), "*", SearchOption.AllDirectories))
            {
                foreach (string subDirectory in this.fileSystem.Directory.GetDirectories(directory))
                {
                    if (this.fileSystem.File.Exists(this.Combine(subDirectory, "__init__.py")))
                    {
                        this.fileSystem.File.Copy(
                        this.Combine(subDirectory, "__init__.py"),
                        this.Combine(this.NvidiaDirectoryPath, "configs", Path.GetFileName(directory), Path.GetFileName(subDirectory), "__init__.py"),
                        true);
                    }
                }
            }
        }

        private void ThrowIfPlatformNotSupported()
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    break;
                default:
                    throw new WorkloadException(
                        $"The MLPerf Training benchmark workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        $" Supported platform/architectures include: " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                        ErrorReason.PlatformNotSupported);
            }
        }

        private async Task ThrowIfUnixDistroNotSupportedAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                    case LinuxDistribution.SUSE:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The MLPerf Training benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()}.  Supported distros include:" +
                            $" Ubuntu, Debian, CentOD7, RHEL7, SUSE. ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(MLPerfExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait();
                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private void PrepareBenchmarkConfigsAndScenarios()
        {
            this.scenarios = new Dictionary<string, string>
            {
                { "bert", "Offline,Server,SingleStream" },
            };

            List<string> bertConfigs = new List<string>()
            {
                "default",
                "high_accuracy",
                "triton",
                "high_accuracy_triton"
            };

            this.benchmarkConfigs = new Dictionary<string, List<string>>
            {
                { "bert", bertConfigs }
            };
        }

        private IEnumerable<Disk> GetFilteredDisks(IEnumerable<Disk> disks, string diskFilter)
        {
            diskFilter = string.IsNullOrWhiteSpace(diskFilter) ? DiskFilters.DefaultDiskFilter : diskFilter;
            List<Disk> filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, System.PlatformID.Unix).ToList();

            return filteredDisks;
        }

        internal class MLPerfState : State
        {
            public MLPerfState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool Initialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(MLPerfState.Initialized), false);
                }

                set
                {
                    this.Properties[nameof(MLPerfState.Initialized)] = value;
                }
            }
        }
    }
}