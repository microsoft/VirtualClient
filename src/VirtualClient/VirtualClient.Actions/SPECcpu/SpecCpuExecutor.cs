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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Routes SPEC CPU execution to the executor matching the package version and provides common execution behavior.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class SpecCpuExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Feature flag that enables CSV result parsing.
        /// </summary>
        protected const string FeatureFlagUseCsvResults = "UseCsvResults";

        private const string SpecCpuRunShell = "runspeccpu.sh";
        private const string SpecCpuRunBat = "runspeccpu.bat";

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;
        private string tuning;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecCpuExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecCpuExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.InitializeServices();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecCpuExecutor"/> class from an existing component.
        /// </summary>
        /// <param name="component">The component to copy.</param>
        protected SpecCpuExecutor(SpecCpuExecutor component)
            : base(component)
        {
            this.InitializeServices();
        }

        /// <summary>
        /// The name of SPEC CPU profile, e.g. intrate, fpspeed.
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
        /// List of benchmarks to run.
        /// </summary>
        public string Benchmarks
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SpecCpuExecutor.Benchmarks), out IConvertible benchmarks);
                return benchmarks?.ToString();
            }
        }

        /// <summary>
        /// Whether SPEC CPU runs base tuning or base+peak tuning.
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
        /// Recommended default: -g -O3 -march=native.
        /// </summary>
        public string BaseOptimizingFlags
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecCpuExecutor.BaseOptimizingFlags), "-g -O3 -march=native");
            }
        }

        /// <summary>
        /// Iterations.
        /// Recommended default: 2.
        /// </summary>
        public int Iterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SpecCpuExecutor.Iterations), 2);
            }
        }

        /// <summary>
        /// Peak optimizing flags.
        /// Recommended default: -g -Ofast -march=native -flto.
        /// </summary>
        public string PeakOptimizingFlags
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecCpuExecutor.PeakOptimizingFlags), "-g -Ofast -march=native -flto");
            }
        }

        /// <summary>
        /// Threads.
        /// </summary>
        public int Threads
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SpecCpuExecutor.Threads), Environment.ProcessorCount);
            }
        }

        /// <summary>
        /// Copies.
        /// </summary>
        public int Copies
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SpecCpuExecutor.Copies), Environment.ProcessorCount);
            }
        }

        /// <summary>
        /// A feature flag to apply. For example 'UseCsvResults' can be used to parse the CSV file results vs. the standard output.
        /// </summary>
        public string FeatureFlag
        {
            get
            {
                string featureFlag = SpecCpuExecutor.FeatureFlagUseCsvResults;
                if (this.Parameters.TryGetValue(nameof(this.FeatureFlag), out IConvertible flag) && !string.IsNullOrWhiteSpace(flag?.ToString()))
                {
                    featureFlag = flag.ToString();
                }

                return featureFlag;
            }
        }

        /// <summary>
        /// The path to the SPEC CPU package.
        /// </summary>
        protected string PackageDirectory { get; set; }

        /// <summary>
        /// The path to the directory where SPEC CPU writes result files.
        /// </summary>
        protected string ResultsDirectory { get; set; }

        /// <summary>
        /// Creates the version-specific SPEC CPU executor.
        /// </summary>
        protected virtual SpecCpuExecutor CreateExecutor()
        {
            if (this.PackageName?.Contains("2026", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new SpecCpu2026Executor(this);
            }

            if (this.PackageName?.Contains("2017", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new SpecCpu2017Executor(this);
            }

            throw new WorkloadException(
                $"The SPEC CPU package name '{this.PackageName}' does not identify a supported version. The package name must contain '2017' or '2026'.",
                ErrorReason.InvalidProfileDefinition);
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (SpecCpuExecutor executor = this.CreateExecutor())
            {
                await executor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the common SPEC CPU workload flow.
        /// </summary>
        /// <param name="telemetryContext">Provides context information for telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ExecuteSpecCpuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    string commandLineArguments = this.GetCommandLineArguments();
                    string command;
                    string commandArguments;

                    if (this.Platform == PlatformID.Unix)
                    {
                        command = "bash";
                        commandArguments = $"{SpecCpuExecutor.SpecCpuRunShell} \"{commandLineArguments}\"";
                    }
                    else
                    {
                        command = "cmd";
                        commandArguments = $"/c {SpecCpuExecutor.SpecCpuRunBat} {commandLineArguments}";
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        command,
                        commandArguments,
                        this.PackageDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: false))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "SPECcpu", logToFile: true);
                            process.ThrowIfWorkloadFailed();

                            await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                        }
                    }
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.RequestLogUploadsAsync(cancellationToken);
                }
            }
        }

        /// <summary>
        /// Initializes the common SPEC CPU package and installation flow.
        /// </summary>
        /// <param name="telemetryContext">Provides context information for telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task InitializeSpecCpuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.PackageDirectory = workloadPackage.Path;
            this.ResultsDirectory = this.Combine(this.PackageDirectory, "result");

            if (this.fileSystem.Directory.Exists(this.ResultsDirectory))
            {
                await this.fileSystem.Directory.DeleteAsync(this.ResultsDirectory);
                this.fileSystem.Directory.CreateDirectory(this.ResultsDirectory);
            }

            string imageFile = this.GetIsoFilePath(workloadPackage);
            telemetryContext.AddContext(nameof(imageFile), imageFile);

            await this.SetupSpecCpuAsync(imageFile, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Creates the metrics parser for SPEC CPU results.
        /// </summary>
        /// <param name="results">The raw result content.</param>
        /// <param name="csv">True when the results are CSV formatted.</param>
        /// <returns>A parser for the results.</returns>
        protected virtual MetricsParser CreateMetricsParser(string results, bool csv)
        {
            return new SpecCpuMetricsParser(results, csv);
        }

        /// <summary>
        /// Gets the SPEC CPU version implemented by the executor.
        /// </summary>
        /// <returns>The SPEC CPU version.</returns>
        protected virtual string GetSpecCpuVersion()
        {
            throw new WorkloadException(
                "A SPEC CPU version has not been defined.",
                ErrorReason.NotSupported);
        }

        /// <summary>
        /// Gets the configuration file name for the current platform and architecture.
        /// </summary>
        /// <returns>The configuration file name.</returns>
        protected virtual string GetConfigurationFileName()
        {
            switch ((this.Platform, this.CpuArchitecture))
            {
                case (PlatformID.Unix, Architecture.X64):
                    return $"vc-linux-x64-{this.GetSpecCpuVersion()}.cfg";

                case (PlatformID.Unix, Architecture.Arm64):
                    return $"vc-linux-arm64-{this.GetSpecCpuVersion()}.cfg";

                case (PlatformID.Win32NT, Architecture.X64):
                    return $"vc-win-x64-{this.GetSpecCpuVersion()}.cfg";

                case (PlatformID.Win32NT, Architecture.Arm64):
                    return $"vc-win-arm64-{this.GetSpecCpuVersion()}.cfg";

                default:
                    throw new WorkloadException(
                        $"Current CPU architecture '{this.CpuArchitecture}' is not supported for SPEC CPU.",
                        ErrorReason.NotSupported);
            }
        }

        /// <summary>
        /// Gets the search pattern used by the SPEC CPU version for result files.
        /// </summary>
        /// <param name="extension">The result file extension, or <see langword="null"/> to match all result files.</param>
        /// <returns>The result file search pattern.</returns>
        protected virtual string GetResultsFileSearchPattern(string extension = null)
        {
            throw new WorkloadException(
                "A SPEC CPU result file search pattern has not been defined.",
                ErrorReason.NotSupported);
        }

        /// <summary>
        /// Gets the state ID used to track installation of the SPEC CPU package.
        /// </summary>
        /// <returns>A state ID scoped to the package name and SPEC CPU version.</returns>
        protected virtual string GetInstallationStateId()
        {
            return $"{nameof(SpecCpuState)}-{this.GetSpecCpuVersion()}-{this.PackageName?.Trim().ToLowerInvariant()}";
        }

        /// <summary>
        /// Applies version-specific changes to the configuration template.
        /// </summary>
        /// <param name="templateText">The configuration template text.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The customized configuration template.</returns>
        protected virtual Task<string> CustomizeConfigurationAsync(string templateText, CancellationToken cancellationToken)
        {
            return Task.FromResult(templateText);
        }

        /// <summary>
        /// Gets the installed compiler major version.
        /// </summary>
        /// <param name="compilerName">The compiler executable name.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The compiler major version, or an empty string if it cannot be determined.</returns>
        protected async Task<string> GetInstalledCompilerDumpVersionAsync(string compilerName, CancellationToken cancellationToken)
        {
            string version = string.Empty;

            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, compilerName, "-dumpversion"))
            {
                try
                {
                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        version = process.StandardOutput.ToString().Trim().Split(".")[0];
                    }
                }
                catch
                {
                    version = string.Empty;
                }
            }

            return version;
        }

        private void InitializeServices()
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
            this.tuning = this.RunPeak ? "all" : "base";
        }

        private string GetIsoFilePath(DependencyPath workloadPackage)
        {
            string[] isoFiles = this.fileSystem.Directory.GetFiles(workloadPackage.Path, "*.iso", SearchOption.TopDirectoryOnly);

            if (isoFiles?.Any() != true)
            {
                throw new DependencyException(
                    $"SPEC CPU .iso/image file not found in the expected package directory path '{this.PackageDirectory}'.",
                    ErrorReason.DependencyNotFound);
            }
            else if (isoFiles.Length > 1)
            {
                throw new DependencyException(
                   $"Ambiguous scenario. Multiple SPEC CPU .iso/image files were found in the expected package directory path '{this.PackageDirectory}'.",
                   ErrorReason.DependencyNotFound);
            }

            return isoFiles.First();
        }

        private async Task SetupSpecCpuAsync(string isoFilePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string stateId = this.GetInstallationStateId();
            SpecCpuState state = await this.stateManager.GetStateAsync<SpecCpuState>(stateId, cancellationToken)
                ?? new SpecCpuState();

            if (state.SpecCpuInitialized)
            {
                return;
            }

            string mountPath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.GetPackagePath(), "speccpu_mount");
            this.fileSystem.Directory.CreateDirectory(mountPath);

            if (this.Platform == PlatformID.Unix)
            {
                await this.LinuxSetupAsync(isoFilePath, mountPath, telemetryContext, cancellationToken);
            }
            else
            {
                await this.WindowsSetupAsync(isoFilePath, telemetryContext, cancellationToken);
            }

            state.SpecCpuInitialized = true;
            await this.stateManager.SaveStateAsync<SpecCpuState>(stateId, state, cancellationToken);
        }

        private async Task LinuxSetupAsync(
            string isoFilePath,
            string mountPath,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            IEnumerable<(string Command, string Arguments, string WorkingDirectory, bool WriteConfiguration)> commands =
            [
                ("mount", $"-t iso9660 -o ro,exec,loop {isoFilePath} {mountPath}", this.PackageDirectory, false),
                ("./install.sh", $"-f -d {this.PackageDirectory}", mountPath, true),
                ("chmod", $"-R ugo=rwx {this.PackageDirectory}", this.PackageDirectory, false),
                ("umount", mountPath, this.PackageDirectory, false)
            ];

            foreach ((string command, string arguments, string workingDirectory, bool writeConfiguration) in commands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    arguments,
                    workingDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true).ConfigureAwait(false);
                    process.ThrowIfWorkloadFailed();
                }

                if (writeConfiguration)
                {
                    await this.WriteSpecCpuConfigAsync(cancellationToken);
                }
            }
        }

        private async Task WindowsSetupAsync(
            string isoFilePath,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            IEnumerable<(string Command, string Arguments, bool CaptureOutput)> preparationCommands =
            [
                ("powershell", $"-Command \"Mount-DiskImage -ImagePath {isoFilePath}\"", false),
                ("powershell", $"-Command \"(Get-DiskImage -ImagePath {isoFilePath}| Get-Volume).DriveLetter\"", true)
            ];

            string driveLetter = null;
            foreach ((string command, string arguments, bool captureOutput) in preparationCommands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    arguments,
                    this.PackageDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true).ConfigureAwait(false);
                    process.ThrowIfWorkloadFailed();

                    if (captureOutput)
                    {
                        driveLetter = process.StandardOutput.ToString().Trim();
                    }
                }
            }

            IEnumerable<(string Command, string Arguments, bool WriteConfiguration)> installationCommands =
            [
                ("cmd", $"/c echo 1 | {this.PlatformSpecifics.Combine($"{driveLetter}:", "install.bat")} {this.PackageDirectory}", true),
                ("powershell", $"-Command \"Dismount-DiskImage -ImagePath {isoFilePath}\"", false)
            ];

            foreach ((string command, string arguments, bool writeConfiguration) in installationCommands)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    command,
                    arguments,
                    this.PackageDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true).ConfigureAwait(false);
                    process.ThrowIfWorkloadFailed();
                }

                if (writeConfiguration)
                {
                    await this.WriteSpecCpuConfigAsync(cancellationToken);
                }
            }
        }

        private async Task CaptureMetricsAsync(
            IProcessProxy process,
            string commandArguments,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    $"SPECcpu/{this.SpecProfile}",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                bool useCsv = string.Equals(this.FeatureFlag, SpecCpuExecutor.FeatureFlagUseCsvResults, StringComparison.OrdinalIgnoreCase);
                string extension = useCsv ? "csv" : "txt";
                string[] outputFiles = this.fileSystem.Directory.GetFiles(
                    this.ResultsDirectory,
                    this.GetResultsFileSearchPattern(extension),
                    SearchOption.TopDirectoryOnly);

                foreach (string file in outputFiles)
                {
                    KeyValuePair<string, string> results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "SPECcpu", results: results);

                    MetricsParser parser = this.CreateMetricsParser(results.Value, useCsv);
                    IList<Metric> metrics = parser.Parse();

                    metrics.LogConsole(this.Scenario, "SPECcpu");
                    metrics.ToList().ForEach(m => m.Categorization = $"{this.SpecProfile}-{this.tuning}");

                    foreach (Metric metric in metrics)
                    {
                        this.Logger.LogMetric(
                            metric,
                            toolName: "SPECcpu",
                            scenarioName: this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            commandArguments,
                            telemetryContext,
                            this.Tags);
                    }
                }
            }
        }

        private async Task RequestLogUploadsAsync(CancellationToken cancellationToken)
        {
            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                string[] outputFiles = this.fileSystem.Directory.GetFiles(
                    this.ResultsDirectory,
                    this.GetResultsFileSearchPattern(),
                    SearchOption.TopDirectoryOnly);

                if (outputFiles?.Any() == true)
                {
                    IEnumerable<IFileInfo> files = outputFiles
                        .Select(path => this.fileSystem.FileInfo.New(path));

                    IEnumerable<FileUploadDescriptor> descriptors = files
                        .Select(file => this.CreateFileUploadDescriptor(new FileContext(
                            file,
                            HttpContentType.PlainText,
                            Encoding.UTF8.WebName,
                            this.ExperimentId,
                            this.AgentId,
                            "speccpu",
                            this.Scenario,
                            null,
                            this.Roles?.FirstOrDefault())));

                    foreach (FileUploadDescriptor descriptor in descriptors)
                    {
                        await this.RequestFileUploadAsync(descriptor);
                    }
                }
            }
        }

        private string GetCommandLineArguments()
        {
            List<string> suites = new List<string> { "intrate", "intspeed", "fprate", "fpspeed" };
            string configurationFile = this.GetConfigurationFileName();
            string command = $"--config {configurationFile} --iterations {this.Iterations} --copies {this.Copies} --threads {this.Threads} --tune {this.tuning}";

            bool reportable = this.Platform == PlatformID.Unix
                && suites.Contains(this.Benchmarks.ToLowerInvariant())
                && (this.Iterations == 2 || this.Iterations == 3);

            command = reportable ? $"{command} --reportable" : $"{command} --noreportable";
            return $"{command} {this.Benchmarks}";
        }

        private async Task WriteSpecCpuConfigAsync(CancellationToken cancellationToken)
        {
            string configurationFile = this.GetConfigurationFileName();
            string templateText = await this.fileSystem.File.ReadAllTextAsync(
                this.PlatformSpecifics.GetScriptPath("speccpu", configurationFile));

            string runScript = this.Platform == PlatformID.Unix
                ? SpecCpuExecutor.SpecCpuRunShell
                : SpecCpuExecutor.SpecCpuRunBat;

            this.fileSystem.File.Copy(
                this.PlatformSpecifics.GetScriptPath("speccpu", runScript),
                this.Combine(this.PackageDirectory, runScript),
                true);

            templateText = await this.CustomizeConfigurationAsync(templateText, cancellationToken);
            templateText = templateText.Replace(
                SpecCpuConfigPlaceholder.BaseOptimizingFlags,
                this.BaseOptimizingFlags,
                StringComparison.OrdinalIgnoreCase);
            templateText = templateText.Replace(
                SpecCpuConfigPlaceholder.PeakOptimizingFlags,
                this.PeakOptimizingFlags,
                StringComparison.OrdinalIgnoreCase);
            templateText = templateText.Replace(
                SpecCpuConfigPlaceholder.Threads,
                this.Threads.ToString(),
                StringComparison.OrdinalIgnoreCase);

            await this.fileSystem.File.WriteAllTextAsync(
                this.Combine(this.PackageDirectory, "config", configurationFile),
                templateText,
                cancellationToken);
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

        private static class SpecCpuConfigPlaceholder
        {
            public const string BaseOptimizingFlags = "$BaseOptimizingFlags$";
            public const string PeakOptimizingFlags = "$PeakOptimizingFlags$";
            public const string Threads = "$Threads$";
        }
    }
}
