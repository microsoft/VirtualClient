// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// STREAM: Sustainable Memory Bandwidth in High Performance Computers Benchmark.
    /// Executor for Stream.
    /// </summary>
    [SupportedPlatforms("linux-x64,linux-arm64,win-x64,win-arm64")]
    public class StreamExecutor : VirtualClientComponent
    {
        private readonly ISystemManagement systemManagement;
        private readonly IFileSystem fileSystem;
        private readonly IPackageManager packageManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public StreamExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
        }

        /// <summary>
        /// GCC compilation parameters for generating STREAM binary.
        /// </summary>
        public string CompilerParameters
        {
            get
            {
                this.Parameters.TryGetValue(nameof(StreamExecutor.CompilerParameters), out IConvertible compilerParameters);
                return compilerParameters?.ToString();
            }
        }

        /// <summary>
        /// Command line parameters to run Stream Msft.
        /// </summary>
        public string CommandLineParameters
        {
            get
            {
                this.Parameters.TryGetValue(nameof(StreamExecutor.CommandLineParameters), out IConvertible commandLineParameters);
                return commandLineParameters?.ToString();
            }
        }

        /// <summary>
        /// Number of threads to run Stream/Stream Triad.
        /// </summary>
        public int? ThreadCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(StreamExecutor.ThreadCount), out IConvertible threadCount);
                return threadCount != null ? threadCount.ToInt32(CultureInfo.InvariantCulture) : null;
            }

            protected set
            {
                this.Parameters[nameof(this.ThreadCount)] = value;
            }
        }

        /// <summary>
        /// The STREAM toolset to use (e.g. STREAM or STREAMTriad).
        /// </summary>
        public string Toolset
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(StreamExecutor.Toolset), "STREAM");
            }
        }

        /// <summary>
        /// Gets the command line arguments for Windows Stream execution.
        /// </summary>
        public string CommandArgumentsWindows
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(StreamExecutor.CommandArgumentsWindows), "-n 50 -s 320000000");
            }
        }

        /// <summary>
        /// Path to Stream Package.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// Path to Stream Windows executable.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Normalized toolset string (upper-case, trimmed).
        /// </summary>
        private string ToolsetNormalized =>
            this.Toolset?.Trim().ToUpperInvariant() ?? string.Empty;

        /// <summary>
        /// Initializes the environment and dependencies for running the Stream workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);
            await this.InitializePlatformAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes Stream workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix && this.ToolsetNormalized == "STREAM")
            {
                await this.ExecuteStreamAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Unix && this.ToolsetNormalized == "STREAMTRIAD")
            {
                await this.ExecuteStreamTriadAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Unix && this.ToolsetNormalized == "STREAMMSFT")
            {
                await this.ExecuteStreamMsftAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT && this.ToolsetNormalized == "STREAM")
            {
                await this.ExecuteStreamAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Validates the component for viability before executing the workload.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            string platformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture);
            string toolset = this.ToolsetNormalized;
            string supportedScenarios = "Supported scenarios: linux-x64 STREAM, linux-x64 STREAMTRIAD, linux-x64 STREAMMSFT, linux-arm64 STREAM, linux-arm64 STREAMMSFT, win-x64 STREAM, win-arm64 STREAM.";

            if (this.Platform == PlatformID.Unix && toolset != "STREAM" && toolset != "STREAMTRIAD" && toolset != "STREAMMSFT")
            {
                throw new WorkloadException(
                    $"Unsupported toolset '{this.Toolset}'. {supportedScenarios}",
                    ErrorReason.InvalidProfileDefinition);
            }
            else if (this.Platform == PlatformID.Unix && toolset == "STREAMTRIAD" && this.CpuArchitecture != Architecture.X64)
            {
                throw new WorkloadException(
                    $"The STREAMTRIAD toolset is only supported on linux-x64. Current platform/architecture: {platformArchitecture}. {supportedScenarios}",
                    ErrorReason.PlatformNotSupported);
            }
            else if (this.Platform == PlatformID.Unix && toolset == "STREAMMSFT" && this.CpuArchitecture != Architecture.Arm64)
            {
                throw new WorkloadException(
                    $"The STREAMMSFT toolset is only supported on linux-arm64. Current platform/architecture: {platformArchitecture}. {supportedScenarios}",
                    ErrorReason.PlatformNotSupported);
            }
            else if (this.Platform == PlatformID.Win32NT && toolset != "STREAM")
            {
                throw new WorkloadException(
                    $"Unsupported toolset '{this.Toolset}' for Windows. {supportedScenarios}",
                    ErrorReason.InvalidProfileDefinition);
            }
            else if (this.Platform != PlatformID.Unix && this.Platform != PlatformID.Win32NT)
            {
                throw new WorkloadException(
                    $"The Stream workload is not supported on the current platform/architecture {platformArchitecture}. {supportedScenarios}",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private async Task InitializePlatformAsync(CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.PackagePath = workloadPackage.Path;

            if (this.Platform == PlatformID.Win32NT)
            {
                this.ExecutablePath = this.PlatformSpecifics.Combine(this.PackagePath, "stream.exe");
                this.fileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);
            }
        }

        private async Task ExecuteStreamAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    await this.ExecuteStreamLinuxAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;

                case PlatformID.Win32NT:
                    await this.ExecuteStreamWindowsAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        private async Task ExecuteStreamLinuxAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            string sourceCodeFile = "stream.c";
            string executableName = "streamworkload";
            string sourceCodePath = this.PlatformSpecifics.Combine(this.PackagePath, sourceCodeFile);
            string executablePath = this.PlatformSpecifics.Combine(this.PackagePath, executableName);

            string effectiveCompilerParameters = this.CompilerParameters;

            if (this.CpuArchitecture == Architecture.Arm64
                && !string.IsNullOrEmpty(effectiveCompilerParameters)
                && Regex.IsMatch(effectiveCompilerParameters, @"-mcmodel=\w+"))
            {
                // mcmodel command line argument is not supported by arm64
                string removedParameter = Regex.Match(effectiveCompilerParameters, @"-mcmodel=\w+").Value;

                this.Logger.LogTraceMessage(
                    $"Removed the parameter from compiler's commandline: \"{removedParameter}\" as it is not supported on cpuarchitecture:{this.CpuArchitecture}, New effective commandLine arguments : {effectiveCompilerParameters}",
                    EventContext.Persisted());

                effectiveCompilerParameters = Regex.Replace(effectiveCompilerParameters, @"-mcmodel=\w+", string.Empty);
            }

            string compileStream = $"gcc {sourceCodePath} -o {executablePath} {effectiveCompilerParameters}";

            await this.ExecuteCommandAsync("bash", $"-c \"{compileStream}\"", relatedContext, cancellationToken)
                .ConfigureAwait(false);

            string ompNumThreads = $"export OMP_NUM_THREADS={this.ThreadCount}";
            string makeExecutable = $"chmod +x {executablePath}";
            string command = $"{ompNumThreads} && {makeExecutable} && {executablePath}";

            relatedContext.AddContext("command", command);

            DateTime startTime = DateTime.UtcNow;

            string results = await this.ExecuteCommandAsync("bash", $"-c \"{command}\"", relatedContext, cancellationToken, "STREAM")
                .ConfigureAwait(false);

            this.MetadataContract.AddForScenario(
              "STREAM",
              command,
              toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            DateTime endTime = DateTime.UtcNow;
            StreamMetricsParser streamResultsParser = new StreamMetricsParser(results);
            IList<Metric> metrics = streamResultsParser.Parse();

            this.Logger.LogMetrics(
                    toolName: "STREAM",
                    scenarioName: "MemoryBandwidth",
                    startTime,
                    endTime,
                    metrics,
                    null,
                    scenarioArguments: command,
                    this.Tags,
                    telemetryContext);
        }

        private async Task ExecuteStreamWindowsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            string commandArguments = this.CommandArgumentsWindows;
            string streamCommand = $"{this.ExecutablePath} {commandArguments}";

            if (this.ThreadCount.HasValue && this.ThreadCount.Value > 0)
            {
                Environment.SetEnvironmentVariable("OMP_NUM_THREADS", this.ThreadCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            relatedContext.AddContext("command", streamCommand);

            DateTime startTime = DateTime.UtcNow;

            string results = await this.ExecuteCommandAsync(this.ExecutablePath, commandArguments, relatedContext, cancellationToken, "STREAM", workingDir: this.PackagePath)
                .ConfigureAwait(false);

            this.MetadataContract.AddForScenario(
                  "STREAM",
                  streamCommand,
                  toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            DateTime endTime = DateTime.UtcNow;
            StreamMetricsParser streamResultsParser = new StreamMetricsParser(results);
            IList<Metric> metrics = streamResultsParser.Parse();

            this.Logger.LogMetrics(
                    toolName: "STREAM",
                    scenarioName: "MemoryBandwidth",
                    startTime,
                    endTime,
                    metrics,
                    null,
                    scenarioArguments: streamCommand,
                    this.Tags,
                    telemetryContext);
        }

        private async Task ExecuteStreamTriadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            Tuple<string, string> executableInfo = await this.GetStreamTriadExecutableAsync(relatedContext, cancellationToken);
            string scenario = executableInfo.Item1;
            string executableName = executableInfo.Item2;

            CpuInfo cpuInfo = await this.systemManagement.GetCpuInfoAsync(cancellationToken);
            string executablePath = this.PlatformSpecifics.Combine(this.PackagePath, executableName);
            string kmpAffinity;

            if (cpuInfo.IsHyperthreadingEnabled)
            {
                kmpAffinity = "export KMP_AFFINITY=granularity=fine,compact,1,0";
            }
            else
            {
                kmpAffinity = "export KMP_AFFINITY=compact";
            }

            string ompNumThreads = $"export OMP_NUM_THREADS={this.ThreadCount}";
            string icclibPath = this.PlatformSpecifics.Combine(this.PackagePath, "icclib");
            string ldLibPath = $"export LD_LIBRARY_PATH={icclibPath}";
            string makeExecutable = $"chmod +x {executablePath}";
            string command = $"{kmpAffinity} && {ompNumThreads} && {ldLibPath} && {makeExecutable} && {executablePath}";

            relatedContext.AddContext("command", command);

            DateTime startTime = DateTime.UtcNow;
            string results = await this.ExecuteCommandAsync("bash", $"-c \"{command}\"", relatedContext, cancellationToken, "STREAM");
            DateTime endTime = DateTime.UtcNow;

            this.MetadataContract.AddForScenario(
              "STREAM Triad",
              command,
              toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            StreamMetricsParser streamResultsParser = new StreamMetricsParser(results);
            IList<Metric> metrics = streamResultsParser.Parse();

            this.Logger.LogMetrics(
                    toolName: "STREAM Triad",
                    scenarioName: scenario,
                    startTime,
                    endTime,
                    metrics,
                    null,
                    scenarioArguments: command,
                    this.Tags,
                    telemetryContext);
        }

        private async Task ExecuteStreamMsftAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            string executableName = "perfrunner";

            await this.ExecuteCommandAsync("bash", "-c \"make\"", relatedContext, cancellationToken, workingDir: this.PackagePath)
                .ConfigureAwait(false);

            string executablePath = this.PlatformSpecifics.Combine(this.PackagePath, executableName);
            string command = $"{executablePath} --threads {this.ThreadCount} {this.CommandLineParameters}";

            relatedContext.AddContext("command", command);

            DateTime startTime = DateTime.UtcNow;
            string results = await this.ExecuteCommandAsync("bash", $"-c \"{command}\"", relatedContext, cancellationToken, "STREAM")
                .ConfigureAwait(false);
            DateTime endTime = DateTime.UtcNow;

            this.MetadataContract.AddForScenario(
             "STREAM",
             command,
             toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            StreamMsftMetricsParser streammsftResultsParser = new StreamMsftMetricsParser(results);
            IList<Metric> metrics = streammsftResultsParser.Parse();

            this.Logger.LogMetrics(
                    toolName: "STREAM MSFT",
                    scenarioName: "MemoryBandwidth",
                    startTime,
                    endTime,
                    metrics,
                    null,
                    scenarioArguments: command,
                    this.Tags,
                    telemetryContext);
        }

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <returns>Output of the command.</returns>
        private Task<string> ExecuteCommandAsync(string cmd, string cmdArgs, EventContext telemetryContext, CancellationToken cancellationToken, string toolName = null, string workingDir = null)
        {
            string output = string.Empty;

            return this.Logger.LogMessageAsync($"{nameof(StreamExecutor)}.ExecuteCommand", telemetryContext, async () =>
            {
                ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
                using (IProcessProxy process = this.Platform == PlatformID.Win32NT
                    ? systemManagement.ProcessManager.CreateProcess(cmd, cmdArgs, workingDir)
                    : systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, cmd, cmdArgs, workingDir: workingDir))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, toolName, logToFile: true);
                        process.ThrowIfWorkloadFailed();
                    }

                    output = process.StandardOutput.ToString();
                }

                return output;
            });
        }

        /// <summary>
        /// Gets executable name dependent on the AVX support.
        /// </summary>
        private async Task<Tuple<string, string>> GetStreamTriadExecutableAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string lscpuFlags = await this.ExecuteCommandAsync("bash", "-c \"lscpu | grep 'Flags'\"", telemetryContext, cancellationToken);
            string executableName;
            string scenarioName;

            if (lscpuFlags.Contains("avx512"))
            {
                executableName = "StreamTriadAVX512";
                scenarioName = "MemoryBandwidth-TriadAVX512";
            }
            else if (lscpuFlags.Contains("avx2"))
            {
                executableName = "StreamTriadAVX2";
                scenarioName = "MemoryBandwidth-TriadAVX2";
            }
            else if (lscpuFlags.Contains("avx"))
            {
                executableName = "StreamTriadAVX";
                scenarioName = "MemoryBandwidth-TriadAVX";
            }
            else
            {
                executableName = "StreamTriad";
                scenarioName = "MemoryBandwidth-Triad";
            }

            return new Tuple<string, string>(scenarioName, executableName);
        }
    }
}