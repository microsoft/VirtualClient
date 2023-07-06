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

        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="MLPerfTrainingExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MLPerfTrainingExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;

            if (string.IsNullOrEmpty(this.DiskFilter))
            {
                this.DiskFilter = "SizeGreaterThan:8000gb&OSDisk:false";
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
        /// ML specific parameter used to control size of training samples
        /// </summary>
        public string BatchSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BatchSize));
            }
        }

        /// <summary>
        /// The framework used for the implementation. It will be a sub directory in the model/implementations directory
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
        /// Name of zip file without .zip. It will be the name of the folder where the model package is extracted to
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
        public string GPUCount
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.GPUCount));
            }
        }

        /// <summary>
        /// Config file for MLPerf Training
        /// </summary>
        public string ConfigFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ConfigFile));
            }
        }

        /// <summary>
        /// The NVIDIA implementation for the MLPerf Training Workload, which is a sub-directory in the repository home
        /// </summary>
        protected string NvidiaPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "mlperf", "NVIDIA");
            }
        }

        /// <summary>
        /// The MLPerf Training Pytorch code directory.
        /// </summary>
        protected string ExecutionPath
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.NvidiaPath, "benchmarks", this.Model, "implementations", this.Implementation);
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

            MLPerfTrainingState state = await this.stateManager.GetStateAsync<MLPerfTrainingState>($"{nameof(MLPerfTrainingState)}", cancellationToken)
                ?? new MLPerfTrainingState();

            if (!state.Initialized)
            {
                // add user in docker group
                await this.ExecuteCommandAsync("usermod", $"-aG docker {this.Username}", this.ExecutionPath, cancellationToken);

                // Setup Environment
                await this.SetupDocker(cancellationToken);
                state.Initialized = true;
                await this.stateManager.SaveStateAsync<MLPerfTrainingState>($"{nameof(MLPerfTrainingState)}", state, cancellationToken);
            }
        }

        /// <summary>
        /// Creates setup for MLPerf Training workload.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected async Task SetupDocker(CancellationToken cancellationToken)
        {
            string dockerImageCommand = $"docker build --pull -t {this.GetContainerName()} .";
            string dockerRunCommand = $"docker run --runtime=nvidia {this.GetContainerName()}";

            await this.ExecuteCommandAsync(
                "sudo",
                dockerImageCommand,
                this.ExecutionPath,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                dockerRunCommand,
                this.ExecutionPath,
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
            string shardsPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "hdf5", "training-4320");
            string evalPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "hdf5", "eval_varlength");
            string checkpointPath = this.PlatformSpecifics.Combine("/mlperftraining0", $"{this.DataPath}", "mlperf-training-package", "phase1");

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string execCommand = $"su -c \"source {this.ConfigFile}; " + 
                                     $"env BATCHSIZE={this.BatchSize} " + 
                                     $"DGXNGPU={this.GPUCount} " + 
                                     $"CUDA_VISIBLE_DEVICES=\"{this.GetGPULabels()}\" " + 
                                     $"CONT={this.GetContainerName()} DATADIR={shardsPath} DATADIR_PHASE2={shardsPath} EVALDIR={evalPath} CHECKPOINTDIR={checkpointPath} CHECKPOINTDIR_PHASE1={checkpointPath} ./run_with_docker.sh\"";

                using (IProcessProxy process = await this.ExecuteCommandAsync("sudo", execCommand, this.ExecutionPath, telemetryContext, cancellationToken)
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
            string logs = string.Concat(process.StandardOutput.ToString(), Environment.NewLine);

            await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf Training", results: logs.AsArray(), logToFile: true);

            MLPerfTrainingMetricsParser parser = new MLPerfTrainingMetricsParser(logs);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "MLPerf Training",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                "GPU",
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
            if (int.TryParse(this.GPUCount, out int gpuCount))
            {
                if (gpuCount < 0)
                {
                    throw new WorkloadException(
                    $"Invalid number of GPUs ({this.GPUCount}) provided",
                    ErrorReason.EnvironmentIsInsufficent);
                }

                return string.Join(",", Enumerable.Range(0, gpuCount));
            }
            else
            {
                throw new WorkloadException(
                    $"Invalid number of GPUs ({this.GPUCount}) provided",
                    ErrorReason.EnvironmentIsInsufficent);
            }
        }

        internal class MLPerfTrainingState : State
        {
            public MLPerfTrainingState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool Initialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(MLPerfTrainingState.Initialized), false);
                }

                set
                {
                    this.Properties[nameof(MLPerfTrainingState.Initialized)] = value;
                }
            }
        }
    }
}