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
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Metadata;

    /// <summary>
    /// The SpecCpu workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class SpecCpuExecutor : VirtualClientComponent
    {
        private const string SpecCpuRunShell = "runspeccpu.sh";
        private const string SpecCpuRunBat = "runspeccpu.bat";

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
        /// Iterations.
        /// Recommand Default: 2
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
        /// The path to the SPECcpu package.
        /// </summary>
        protected string PackageDirectory { get; set; }

        /// <summary>
        /// Executes the SPECcpu workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                string command = null;
                string commandArguments = null;

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

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "SPECcpu", logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                    }
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.UploadSpecCpuLogsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the SPECcpu workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.PackageDirectory = workloadPackage.Path;

            string imageFile = this.GetIsoFilePath(workloadPackage);
            telemetryContext.AddContext(nameof(imageFile), imageFile);

            await this.SetupSpecCpuAsync(imageFile, telemetryContext, cancellationToken);
        }

        private string GetConfigurationFileName()
        {
            switch ((this.Platform, this.CpuArchitecture))
            {
                // Windows is not supported. Modify this section if Windows is added.
                case (PlatformID.Unix, Architecture.X64):
                    return "vc-linux-x64.cfg";

                case (PlatformID.Unix, Architecture.Arm64):
                    return "vc-linux-arm64.cfg";

                case (PlatformID.Win32NT, Architecture.X64):
                    return "vc-win-x64.cfg";

                case (PlatformID.Win32NT, Architecture.Arm64):
                    return "vc-win-arm64.cfg";

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

                if (this.Platform == PlatformID.Unix)
                {
                    await this.ExecuteCommandAsync("mount", $"-t iso9660 -o ro,exec,loop {isoFilePath} {mountPath}", this.PackageDirectory, telemetryContext, cancellationToken);
                    await this.ExecuteCommandAsync("./install.sh", $"-f -d {this.PackageDirectory}", mountPath, telemetryContext, cancellationToken);
                    await this.WriteSpecCpuConfigAsync(cancellationToken);
                    await this.ExecuteCommandAsync("chmod", $"-R ugo=rwx {this.PackageDirectory}", this.PackageDirectory, telemetryContext, cancellationToken);
                    await this.ExecuteCommandAsync("umount", mountPath, this.PackageDirectory, telemetryContext, cancellationToken);
                }
                else
                {
                    // powershell -Command "Mount-DiskImage -ImagePath "C:\Users\azureuser\Desktop\cpu2017-1.1.8.iso""
                    string mountIsoCmd = $"-Command \"Mount-DiskImage -ImagePath {isoFilePath}\"";
                    await this.ExecuteCommandAsync("powershell", mountIsoCmd, this.PackageDirectory, telemetryContext, cancellationToken);

                    // powershell -Command "(Get-DiskImage -ImagePath "C:\Users\azureuser\Desktop\cpu2017-1.1.8.iso" | Get-Volume).DriveLetter "
                    string getDriveLetterCmd = $"-Command \"(Get-DiskImage -ImagePath {isoFilePath}| Get-Volume).DriveLetter\"";
                    string driveLetter = await this.ExecuteCommandAsync("powershell", getDriveLetterCmd, this.PackageDirectory, telemetryContext, cancellationToken);

                    // The reason for the echo is that there is a "pause" in the install.bat. The echo skips it.
                    // echo 1 | install.bat  C:\cpu2017
                    string installCmd = $"/c echo 1 | {this.PlatformSpecifics.Combine($"{driveLetter.Trim()}:", "install.bat")} {this.PackageDirectory}";
                    await this.ExecuteCommandAsync("cmd", installCmd, this.PackageDirectory, telemetryContext, cancellationToken);

                    await this.WriteSpecCpuConfigAsync(cancellationToken);

                    // powershell -Command "Dismount-DiskImage -ImagePath "C:\Users\azureuser\Desktop\cpu2017-1.1.8.iso""
                    string dismountCmd = $"-Command \"Dismount-DiskImage -ImagePath {isoFilePath}\"";
                    await this.ExecuteCommandAsync("powershell", dismountCmd, this.PackageDirectory, telemetryContext, cancellationToken);
                }

                state.SpecCpuInitialized = true;
            }

            await this.stateManager.SaveStateAsync<SpecCpuState>($"{nameof(SpecCpuState)}", state, cancellationToken);
        }

        private async Task<string> ExecuteCommandAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = EventContext.Persisted()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments);

            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, commandArguments, workingDirectory))
            {
                this.CleanupTasks.Add(() => process.SafeKill());
                this.LogProcessTrace(process);

                await process.StartAndWaitAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    if (process.IsErrored())
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfWorkloadFailed();
                    }
                }

                return process.StandardOutput.ToString();
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    $"SPECcpu/{this.SpecProfile}",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                // CPU2017.008.intrate.txt
                string resultsDirectory = this.PlatformSpecifics.Combine(this.PackageDirectory, "result");
                string[] outputFiles = this.fileSystem.Directory.GetFiles(resultsDirectory, "CPU2017.*.txt", SearchOption.TopDirectoryOnly);

                foreach (string file in outputFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "SPECcpu", results: results.AsArray(), logToFile: true);

                    SpecCpuMetricsParser parser = new SpecCpuMetricsParser(results);
                    IList<Metric> metrics = parser.Parse();
                    metrics.LogConsole(this.Scenario, "SPECcpu");

                    this.Logger.LogMetrics(
                        toolName: "SPECcpu",
                        scenarioName: this.Scenario,
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        metricCategorization: $"{this.SpecProfile}-{this.tuning}",
                        commandArguments,
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
        }

        private async Task UploadSpecCpuLogsAsync(CancellationToken cancellationToken)
        {
            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                // CPU2017.001.log, CPU2017.001.log.debug, etc
                string results = this.PlatformSpecifics.Combine(this.PackageDirectory, "result");
                string[] outputFiles = this.fileSystem.Directory.GetFiles(results, "*CPU2017*", SearchOption.TopDirectoryOnly);

                if (outputFiles?.Any() == true)
                {
                    IEnumerable<IFileInfo> files = outputFiles.ToList()
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

                    await this.UploadFilesAsync(blobManager, this.fileSystem, descriptors, cancellationToken);
                }
            }
        }

        private string GetCommandLineArguments()
        {
            // runcpu arguments document: https://www.spec.org/cpu2017/Docs/runcpu.html#strict
            string configurationFile = this.GetConfigurationFileName();

            string cmd = @$"--config {configurationFile} --iterations {this.Iterations} --copies {this.Copies} --threads {this.Threads} --tune {this.tuning}";

            // For linux runs we are doing reportable. For windows since not all benchmarks could be run, it will be noreportable.
            // Iterations has to be either 2 or 3 for reportable runs. https://www.spec.org/cpu2017/Docs/config.html#reportable
            bool reportable = (this.Platform == PlatformID.Unix) && (this.Iterations == 2 || this.Iterations == 3);
            cmd = reportable ? $"{cmd} --reportable" : $"{cmd} --noreportable";
            cmd = $"{cmd} {this.SpecProfile}";
            return cmd;
        }

        private async Task WriteSpecCpuConfigAsync(CancellationToken cancellationToken)
        {
            // Copy SPECcpu configuration file to the config folder.
            string configurationFile = this.GetConfigurationFileName();
            string templateText = await this.fileSystem.File.ReadAllTextAsync(this.PlatformSpecifics.GetScriptPath("speccpu", configurationFile));

            // Copy SPECcpu run shell to the config folder.
            if (this.Platform == PlatformID.Unix) 
            {
                this.fileSystem.File.Copy(
                    this.PlatformSpecifics.GetScriptPath("speccpu", SpecCpuExecutor.SpecCpuRunShell),
                    this.Combine(this.PackageDirectory, SpecCpuExecutor.SpecCpuRunShell),
                    true);

                string compilerVersion = await this.GetInstalledCompilerDumpVersionAsync("gcc", cancellationToken);

                if (string.IsNullOrEmpty(compilerVersion))
                {
                    throw new WorkloadException("gcc version not found.");
                }

                templateText = templateText.Replace(
                SpecCpuConfigPlaceHolder.Gcc10Workaround,
                Convert.ToInt32(compilerVersion) >= 10 ? SpecCpuConfigPlaceHolder.Gcc10WorkaroundContent : string.Empty,
                StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                this.fileSystem.File.Copy(
                this.PlatformSpecifics.GetScriptPath("speccpu", SpecCpuExecutor.SpecCpuRunBat),
                this.Combine(this.PackageDirectory, SpecCpuExecutor.SpecCpuRunBat),
                true);

                templateText = templateText.Replace(
                SpecCpuConfigPlaceHolder.Gcc10Workaround,
                SpecCpuConfigPlaceHolder.Gcc10WorkaroundContent,
                StringComparison.OrdinalIgnoreCase);
            }

            templateText = templateText.Replace(SpecCpuConfigPlaceHolder.BaseOptimizingFlags, this.BaseOptimizingFlags, StringComparison.OrdinalIgnoreCase);
            templateText = templateText.Replace(SpecCpuConfigPlaceHolder.PeakOptimizingFlags, this.PeakOptimizingFlags, StringComparison.OrdinalIgnoreCase);
            await this.fileSystem.File.WriteAllTextAsync(this.Combine(this.PackageDirectory, "config", configurationFile), templateText, cancellationToken);
        }

        private async Task<string> GetInstalledCompilerDumpVersionAsync(string compilerName, CancellationToken cancellationToken)
        {
            string command = compilerName;
            string commandArguments = "-dumpversion";
            string version = string.Empty;

            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, commandArguments))
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