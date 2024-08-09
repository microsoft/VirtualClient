// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
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
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The MLPerf Training workload executor.
    /// </summary>
    [SupportedPlatforms("linux-x64")]
    public class MLPerfTrainingExecutor : VirtualClientComponent
    {
        private const string AccuracySummary = nameof(MLPerfTrainingExecutor.AccuracySummary);
        private const string PerformanceSummary = nameof(MLPerfTrainingExecutor.PerformanceSummary);
        private string executionPath;

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
        }

        /// <summary>
        /// The MLPerf Training model name (e.g. bert, rnnt, ssd-mobilenet).
        /// </summary>
        public string Model
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Model), "bert");
            }
        }

        /// <summary>
        /// The current running user.
        /// </summary>
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(MLPerfExecutor.Username), string.Empty);

                string sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
                if (!string.IsNullOrEmpty(sudoUser))
                {
                    username = sudoUser;
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    username = Environment.UserName;
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
                return this.Parameters.GetValue<string>(nameof(this.BatchSize), "40");
            }
        }

        /// <summary>
        /// The framework used for the implementation. It will be a sub directory in the model/implementations directory
        /// </summary>
        public string Implementation
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Implementation), "pytorch-22.09");
            }
        }

        /// <summary>
        /// Container image name
        /// </summary>
        public string ContainerName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ContainerName), "language_model");
            }
        }

        /// <summary>
        /// Name of zip file without .zip. It will be the name of the folder where the model package is extracted to
        /// </summary>
        public string DataPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DataPath), "mlperf-training-data-bert.1.0.0");
            }
        }

        /// <summary>
        /// Number of GPUs to be utilized
        /// </summary>
        public int GPUCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.GPUCount), 8);
            }
        }

        /// <summary>
        /// Config file for MLPerf Training
        /// </summary>
        public string ConfigFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ConfigFile), "config_DGXA100_1x8x56x1.sh");
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the MLPerf Training workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.InitializationStarted", telemetryContext);

            await this.ThrowIfUnixDistroNotSupportedAsync(cancellationToken);

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken);
            this.executionPath = this.PlatformSpecifics.Combine(workloadPackage.Path, "NVIDIA", "benchmarks", this.Model, "implementations", this.Implementation);

            await this.SetupDocker(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Creates setup for MLPerf Training workload.
        /// </summary>
        protected async Task SetupDocker(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // add user in docker group
            await this.ExecuteCommandAsync("sudo", $"usermod -aG docker {this.Username}", this.executionPath, telemetryContext, cancellationToken);

            string containerName = this.GetContainerName();
            string dockerImageCommand = $"docker build --pull -t {containerName} .";
            string dockerRunCommand = $"docker run --runtime=nvidia {containerName}";

            await this.ExecuteCommandAsync("sudo", dockerImageCommand, this.executionPath, telemetryContext, cancellationToken);
            await this.ExecuteCommandAsync("sudo", dockerRunCommand, this.executionPath, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Gets the container name created by MLPerf Training.
        /// </summary>
        protected string GetContainerName()
        {
            // Update this function to accomodate other architectures
            if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.X64)
            {
                return $"mlperf-training-{this.Username}-x86_64:{this.ContainerName}";
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
            string execCommand = $"su -c \"source {this.ConfigFile}; " +
                                 $"env BATCHSIZE={this.BatchSize} " +
                                 $"DGXNGPU={this.GPUCount} " +
                                 $"CUDA_VISIBLE_DEVICES=\"{string.Join(',', Enumerable.Range(0, this.GPUCount).ToArray())}\" " +
                                 $"CONT={this.GetContainerName()} DATADIR={shardsPath} DATADIR_PHASE2={shardsPath} EVALDIR={evalPath} CHECKPOINTDIR={checkpointPath} CHECKPOINTDIR_PHASE1={checkpointPath} ./run_with_docker.sh\"";

            using (IProcessProxy process = await this.ExecuteCommandAsync("sudo", execCommand, this.executionPath, telemetryContext, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true);

                    process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);

                    this.CaptureMetrics(process, telemetryContext, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Parse metrics and push to telemetry
        /// </summary>
        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.MetadataContract.AddForScenario(
                "MLPerf",
                process.FullCommand(),
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);
            // Convert StandardOutput to string
            string logs = string.Concat(process.StandardOutput.ToString(), Environment.NewLine);

            MLPerfTrainingMetricsParser parser = new MLPerfTrainingMetricsParser(logs);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "MLPerfTraining",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                "MLPerfTrainingPerformance",
                process.FullCommand(),
                this.Tags,
                telemetryContext);
        }

        /// <summary>
        /// Unsupported Linux error handling.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        private async Task ThrowIfUnixDistroNotSupportedAsync(CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);

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
    }
}