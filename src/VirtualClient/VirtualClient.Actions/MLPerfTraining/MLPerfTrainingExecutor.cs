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
        /// Container image name
        /// </summary>
        public string ContainerName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ContainerName));
            }
        }

        /// <summary>
        /// Container image name
        /// </summary>
        public string DataPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DataPath));
            }
        }

        /// <summary>
        /// Number of GPUs to be utilized
        /// </summary>
        public string GPUNum
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.GPUNum));
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
                return this.PlatformSpecifics.Combine(this.NvidiaDirectoryPath, "benchmarks", this.Model, "implementations", this.Implementation);
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
            string dockerRunCommand = $"docker run --runtime=nvidia {this.GetContainerName()}";

            await this.ExecuteCommandAsync(
                "sudo",
                dockerImageCommand,
                this.ExecutionDirectoryPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                dockerRunCommand,
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
                return $"mlperf-training-{this.Username}-x86_64:{this.ContainerName}";
            }
            else if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.Arm64)
            {
                return $"mlperf-training-{this.Username}-arm64:{this.ContainerName}";
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
            string shardsPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "hdf5", "training-4320");
            string evalPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "hdf5", "eval_varlength");
            string checkpointPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "phase1");

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string execCommand = $"su -c \"source config_DGXA100_1x8x56x1.sh; " + 
                                     $"env BATCHSIZE={this.BatchSize} " + 
                                     $"DGXNGPU={this.GPUNum} " + 
                                     $"CUDA_VISIBLE_DEVICES=\"{this.GetGPULabels()}\" " + 
                                     $"CONT={this.GetContainerName()} DATADIR={shardsPath} DATADIR_PHASE2={shardsPath} EVALDIR={evalPath} CHECKPOINTDIR={checkpointPath} CHECKPOINTDIR_PHASE1={checkpointPath} ./run_with_docker.sh\"";

                using (IProcessProxy process = await this.ExecuteCommandAsync("sudo", execCommand, this.ExecutionDirectoryPath, telemetryContext, cancellationToken)
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

        /// <summary>
        /// Parse metrics and push to telemetry
        /// </summary>
        /// <param name="process">Execute process for StandardOutput containing the metrics</param>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken, string context = null)
        {
            // Convert StandardOutput to string
            ConcurrentBuffer buffer = process.StandardOutput;

            string logs = string.Concat(buffer.ToString(), Environment.NewLine);

            await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf Training", results: logs.AsArray(), logToFile: true);

            MLPerfTrainingMetricsParser parser = new MLPerfTrainingMetricsParser(logs);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "MLPerf Training",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                "Executing",
                null,
                this.Tags,
                telemetryContext);
        }

        /// <summary>
        /// Unsupported platform error handling
        /// </summary>
        /// <exception cref="WorkloadException"></exception>
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

        /// <summary>
        /// Unsupported Linux error handling
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
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

        /// <summary>
        /// Make a list of all benchmarks and their configs to make scenarios
        /// </summary>
        private void PrepareBenchmarkConfigsAndScenarios()
        {
            this.scenarios = new Dictionary<string, string>
            {
                { "bert", "PyTorch-22.09" },
            };

            List<string> bertConfigs = new List<string>()
            {
                "default"
            };

            this.benchmarkConfigs = new Dictionary<string, List<string>>
            {
                { "bert", bertConfigs }
            };
        }

        /// <summary>
        ///  Filter the disks using the disk filter and return them
        /// </summary>
        /// <param name="disks"></param>
        /// <param name="diskFilter"></param>
        /// <returns></returns>
        private IEnumerable<Disk> GetFilteredDisks(IEnumerable<Disk> disks, string diskFilter)
        {
            diskFilter = string.IsNullOrWhiteSpace(diskFilter) ? DiskFilters.DefaultDiskFilter : diskFilter;
            List<Disk> filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, System.PlatformID.Unix).ToList();

            return filteredDisks;
        }

        /// <summary>
        /// Get the GPU labels to be used for training (To be made a parameter later)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        private string GetGPULabels()
        {
            if (int.TryParse(this.GPUNum, out int gpuCount))
            {
                if (gpuCount < 0)
                {
                    throw new WorkloadException(
                    $"Invalid number of GPUs ({this.GPUNum}) provided",
                    ErrorReason.EnvironmentIsInsufficent);
                }

                return string.Join(",", Enumerable.Range(0, gpuCount));
            }
            else
            {
                throw new WorkloadException(
                    $"Invalid number of GPUs ({this.GPUNum}) provided",
                    ErrorReason.EnvironmentIsInsufficent);
            }
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