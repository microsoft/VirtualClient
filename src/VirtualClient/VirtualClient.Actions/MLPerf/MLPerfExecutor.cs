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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The MLPerf workload executor.
    /// </summary>
    [SupportedPlatforms("linux-x64")]
    public class MLPerfExecutor : VirtualClientComponent
    {
        private const string AccuracySummary = nameof(MLPerfExecutor.AccuracySummary);
        private const string PerformanceSummary = nameof(MLPerfExecutor.PerformanceSummary);

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        private IDiskManager diskManager;
        private string mlperfScratchSpace;

        private List<string> benchmarks;
        private Dictionary<string, string> scenarios;
        private Dictionary<string, List<string>> benchmarkConfigs;

        /// <summary>
        /// Constructor for <see cref="MLPerfExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MLPerfExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;

            this.fileSystem = this.systemManager.FileSystem;
            this.diskManager = this.systemManager.DiskManager;

            this.benchmarks = new List<string>
            {
                "bert",
                "3d-unet"
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
                string filter = this.Parameters.GetValue<string>(nameof(MLPerfExecutor.DiskFilter), "SizeGreaterThan:1000gb");
                // Enforce filter to remove OS disk.
                filter = $"{filter}&OSDisk:false";
                return filter;
            }

            set
            {
                this.Parameters[nameof(MLPerfExecutor.DiskFilter)] = value;
            }
        }

        /// <summary>
        /// The MLPerf model name (e.g. bert, 3d-unet).
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
                string username = this.Parameters.GetValue<string>(nameof(MLPerfExecutor.Username), string.Empty);
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = Environment.UserName;
                }

                return username;
            }
        }

        /// <summary>
        /// This enables A100_PCIe_40GBx8 system support that was not supported by github repo of MLPerf.
        /// </summary>
        public bool RequireCustomSystemSupport
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(MLPerfExecutor.RequireCustomSystemSupport), false);
            }
        }

        /// <summary>
        /// The MLPerf Nvidia code directory.
        /// </summary>
        protected string NvidiaDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "mlperf", "closed", "NVIDIA");
            }
        }

        /// <summary>
        /// Export statement for scratch space
        /// </summary>
        protected string ExportScratchSpace { get; set; }

        /// <summary>
        /// The output directory of MLPerf.
        /// </summary>
        protected string OutputDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.NvidiaDirectory, "build", "logs");
            }
        }

        /// <summary>
        /// Executes the MLPerf workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.PrepareBenchmarkConfigsAndScenarios();

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                foreach (string config in this.benchmarkConfigs[this.Model])
                {
                    string perfModeExecCommand = $"docker exec -u {this.Username} {this.GetContainerName()} " +
                        $"sudo bash -c \"{this.ExportScratchSpace} && " +
                        $"make run RUN_ARGS=\'--benchmarks={this.Model} --scenarios={this.scenarios[this.Model]} " +
                        $"--config_ver={config} --test_mode=PerformanceOnly --fast\'\"";

                    string accuracyModeExecCommand = $"docker exec -u {this.Username} {this.GetContainerName()} " +
                        $"sudo bash -c \"{this.ExportScratchSpace} && " +
                        $"make run RUN_ARGS=\'--benchmarks={this.Model} --scenarios={this.scenarios[this.Model]} " +
                        $"--config_ver={config} --test_mode=AccuracyOnly --fast\'\"";

                    using (IProcessProxy process = await this.ExecuteCommandAsync("sudo", perfModeExecCommand, this.NvidiaDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait())
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (process.IsErrored())
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }

                            await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken, MLPerfExecutor.PerformanceSummary);
                        }
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync("sudo", accuracyModeExecCommand, this.NvidiaDirectory, telemetryContext, cancellationToken)
                       .ConfigureAwait())
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext)
                                .ConfigureAwait();

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);

                            await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken, MLPerfExecutor.AccuracySummary)
                               .ConfigureAwait();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the MLPerf workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.InitializationStarted", telemetryContext);

            await this.ThrowIfUnixDistroNotSupportedAsync(cancellationToken)
                .ConfigureAwait(false);

            await this.CreateScratchSpace(cancellationToken);
            this.ExportScratchSpace = $"export MLPERF_SCRATCH_PATH={this.mlperfScratchSpace}";

            MLPerfState state = await this.stateManager.GetStateAsync<MLPerfState>($"{nameof(MLPerfState)}", cancellationToken)
                ?? new MLPerfState();

            if (!state.Initialized)
            {
                // add user in docker group and create scratch space
                await this.ExecuteCommandAsync("usermod", $"-aG docker {this.Username}", this.NvidiaDirectory, cancellationToken);

                if (this.RequireCustomSystemSupport)
                {
                    // This enables A100_PCIe_40GBx8 system support that was not supported by github repo of MLPerf..
                    this.ReplaceGPUConfigFilesToSupportAdditionalGPUs();
                }

                this.ReplaceMakefile();

                await this.SetupEnvironmentAsync(cancellationToken);
                state.Initialized = true;
                await this.stateManager.SaveStateAsync<MLPerfState>($"{nameof(MLPerfState)}", state, cancellationToken);
            }
        }

        /// <summary>
        /// Create a scratch directory to store downloaded data and models for MLPerf.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected async Task CreateScratchSpace(CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await this.diskManager.GetDisksAsync(cancellationToken).ConfigureAwait(false);

            if (disks?.Any() != true)
            {
                throw new WorkloadException(
                    "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            IEnumerable<Disk> filteredDisks = this.GetFilteredDisks(disks, this.DiskFilter);

            if (filteredDisks?.Any() != true)
            {
                throw new WorkloadException(
                    "Expected disks based on filter not found. Given the parameters defined for the profile action/step or those passed " +
                    "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                    "of the existing disks.",
                    ErrorReason.DependencyNotFound);
            }

            if (await this.diskManager.CreateMountPointsAsync(filteredDisks, this.systemManager, cancellationToken).ConfigureAwait(false))
            {
                // Refresh the disks to pickup the mount point changes.
                await Task.Delay(1000).ConfigureAwait(false);

                IEnumerable<Disk> updatedDisks = await this.diskManager.GetDisksAsync(cancellationToken)
                    .ConfigureAwait(false);

                filteredDisks = this.GetFilteredDisks(updatedDisks, this.DiskFilter);
            }

            filteredDisks.ToList().ForEach(disk => this.Logger.LogTraceMessage($"Disk Target: '{disk}'"));

            string accessPath = filteredDisks.OrderBy(d => d.Index).First().GetPreferredAccessPath(this.Platform);
            this.mlperfScratchSpace = this.PlatformSpecifics.Combine(accessPath, "scratch");

            if (!this.fileSystem.Directory.Exists(this.mlperfScratchSpace))
            {
                this.fileSystem.Directory.CreateDirectory(this.mlperfScratchSpace).Create();
            }
        }

        /// <summary>
        /// Gets the container name created by MLPerf.
        /// </summary>
        /// <returns>Container name created by MLPerf</returns>
        /// <exception cref="WorkloadException"></exception>
        protected string GetContainerName()
        {
            // Update this function to accomodate other architectures
            if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.X64)
            {
                return $"mlperf-inference-{this.Username}-x86_64";
            }
            else if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.Arm64)
            {
                return $"mlperf-inference-{this.Username}-arm64";
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
        /// Creates setup for MLPerf workload.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns></returns>
        protected async Task SetupEnvironmentAsync(CancellationToken cancellationToken)
        {
            string dockerExecCommand = $"docker exec -u {this.Username} {this.GetContainerName()}";

            this.fileSystem.Directory.CreateDirectory(this.PlatformSpecifics.Combine(this.mlperfScratchSpace, "data"));
            this.fileSystem.Directory.CreateDirectory(this.PlatformSpecifics.Combine(this.mlperfScratchSpace, "models"));
            this.fileSystem.Directory.CreateDirectory(this.PlatformSpecifics.Combine(this.mlperfScratchSpace, "preprocessed_data"));

            await this.ExecuteCommandAsync(
                "sudo",
                $"systemctl restart docker",
                this.NvidiaDirectory,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                $"systemctl start nvidia-fabricmanager",
                this.NvidiaDirectory,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo", 
                $" -u {this.Username} bash -c \"make prebuild MLPERF_SCRATCH_PATH={this.mlperfScratchSpace}\"", 
                this.NvidiaDirectory, 
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                $"docker ps",
                this.NvidiaDirectory,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo",
                $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make clean\"",
                this.NvidiaDirectory,
                cancellationToken);

            await this.ExecuteCommandAsync(
                "sudo", 
                $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make link_dirs\"", 
                this.NvidiaDirectory, 
                cancellationToken);

            foreach (string benchmark in this.benchmarks)
            {
                await this.ExecuteCommandAsync(
                    "sudo", 
                    $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make download_data BENCHMARKS={benchmark}\"", 
                    this.NvidiaDirectory, 
                    cancellationToken);

                await this.ExecuteCommandAsync(
                    "sudo", 
                    $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make download_model BENCHMARKS={benchmark}\"", 
                    this.NvidiaDirectory, 
                    cancellationToken);

                await this.ExecuteCommandAsync(
                    "sudo", 
                    $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make preprocess_data BENCHMARKS={benchmark}\"", 
                    this.NvidiaDirectory, 
                    cancellationToken);
            }

            await this.ExecuteCommandAsync(
                "sudo", 
                $"{dockerExecCommand} sudo bash -c \"{this.ExportScratchSpace} && make build\"", 
                this.NvidiaDirectory, 
                cancellationToken);
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken, string context = null)
        {
            this.MetadataContract.AddForScenario(
                "MLPerf",
                process.FullCommand(),
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            if (context == MLPerfExecutor.AccuracySummary)
            {
                string[] resultsFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectory, "metadata.json", SearchOption.AllDirectories);

                foreach (string file in resultsFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf", results: results.AsArray(), logToFile: true);

                    MLPerfMetricsParser parser = new MLPerfMetricsParser(results, accuracyMode: true);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        "MLPerf",
                        this.Model,
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        "AccuracyMode",
                        process.FullCommand(),
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
            else if (context == MLPerfExecutor.PerformanceSummary)
            {
                string[] resultsFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectory, "metadata.json", SearchOption.AllDirectories);

                foreach (string file in resultsFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MLPerf", results: results.AsArray(), logToFile: true);

                    MLPerfMetricsParser parser = new MLPerfMetricsParser(results, accuracyMode: false);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        "MLPerf",
                        this.Model,
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        "PerformanceMode",
                        process.FullCommand(),
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
        }

        private void ReplaceGPUConfigFilesToSupportAdditionalGPUs()
        {
            foreach (string directory in this.fileSystem.Directory.GetDirectories(this.PlatformSpecifics.GetScriptPath("mlperf", "GPUConfigFiles"), "*", SearchOption.AllDirectories))
            {
                foreach (string subDirectory in this.fileSystem.Directory.GetDirectories(directory))
                {
                    if (this.fileSystem.File.Exists(this.Combine(subDirectory, "__init__.py")))
                    {
                        this.fileSystem.File.Copy(
                        this.Combine(subDirectory, "__init__.py"),
                        this.Combine(this.NvidiaDirectory, "configs", Path.GetFileName(directory), Path.GetFileName(subDirectory), "__init__.py"),
                        true);
                    }
                }
            }
        }

        private void ReplaceMakefile()
        {
            if (this.fileSystem.File.Exists(this.PlatformSpecifics.GetScriptPath("mlperf", "Makefile.docker")))
            {
                this.fileSystem.File.Copy(
                    this.PlatformSpecifics.GetScriptPath("mlperf", "Makefile.docker"),
                    this.PlatformSpecifics.GetPackagePath("mlperf", "closed", "NVIDIA", "Makefile.docker"),
                    true);
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
                            $"The MLPerf benchmark workload is not supported on the current Linux distro - " +
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
            this.scenarios = new Dictionary<string, string>();

            this.scenarios.Add("bert", "Offline,Server,SingleStream");
            this.scenarios.Add("3d-unet", "Offline,SingleStream");

            List<string> configs = new List<string>()
            {
                "default"
            };

            this.benchmarkConfigs = new Dictionary<string, List<string>>();

            this.benchmarkConfigs.Add("bert", configs);
            this.benchmarkConfigs.Add("3d-unet", configs);
        }

        private IEnumerable<Disk> GetFilteredDisks(IEnumerable<Disk> disks, string diskFilter)
        {
            List<Disk> filteredDisks = new List<Disk>();
            diskFilter = string.IsNullOrWhiteSpace(diskFilter) ? DiskFilters.DefaultDiskFilter : diskFilter;
            filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, System.PlatformID.Unix).ToList();

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