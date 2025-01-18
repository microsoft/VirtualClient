// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The LMbench workload virtual client action
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class LMbenchExecutor : VirtualClientComponent
    {
        private static readonly List<string> DefaultBenchmarks = new List<string>
        {
            "BENCHMARK_BCOPY",
            "BENCHMARK_MEM",
            "BENCHMARK_MMAP",
            "BENCHMARK_FILE",

            // BENCHMARK_HARDWARE
            // BENCHMARK_OS
            // BENCHMARK_DEVELOPMENT
            // BENCHMARK_CONNECT
            // BENCHMARK_CTX
            // BENCHMARK_HTTP
            // BENCHMARK_OPS
            // BENCHMARK_PAGEFAULT
            // BENCHMARK_PIPE
            // BENCHMARK_PROC
            // BENCHMARK_RPC
            // BENCHMARK_SELECT
            // BENCHMARK_SIG
            // BENCHMARK_SYSCALL
            // BENCHMARK_TCP
            // BENCHMARK_UDP
            // BENCHMARK_UNIX
        };

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LMbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.processManager = this.systemManagement.ProcessManager;
        }

        /// <summary>
        /// The set of benchmarks to execute. Each of these is an environment variable that is
        /// recognized by the LMbench software. See the set of benchmarks noted above.
        /// </summary>
        public IEnumerable<string> Benchmarks
        {
            get
            {
                this.Parameters.TryGetCollection(nameof(this.Benchmarks), out IEnumerable<string> benchmarks);
                return benchmarks ?? LMbenchExecutor.DefaultBenchmarks;
            }
        }

        /// <summary>
        /// Name of the Binary to run for the suite of Benchmarks compiled.
        /// </summary>
        public string BinaryName
        {
            get
            {
               return this.Parameters.GetValue<string>(nameof(this.BinaryName), null);
            }
        }

        /// <summary>
        /// Name of the Binary to run for the suite of Benchmarks compiled.
        /// </summary>
        public string BinaryCommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BinaryCommandLine), null);
            }
        }

        /// <summary>
        /// The compilerFlags that are used for make command in compiling LMbench.
        /// </summary>
        public string CompilerFlags
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LMbenchExecutor.CompilerFlags), out IConvertible compilerFlags);
                return compilerFlags?.ToString();
            }
        }

        /// <summary>
        /// The amount of memory to use for the target memory benchmarks (in megabytes).
        /// </summary>
        public long? MemorySizeMB
        {
            get
            { 
                this.Parameters.TryGetValue(nameof(LMbenchExecutor.MemorySizeMB), out IConvertible memory);
                return memory?.ToInt64(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The path where the LMbench JSON results file should be output.
        /// </summary>
        protected DependencyPath LMbenchPackage { get; set; }

        /// <summary>
        /// Executes LMbench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.BuildSourceCodeAsync(telemetryContext, cancellationToken);
            if (this.BinaryName == null)
            {
                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
            }
            else if (this.BinaryName.Equals("lat_mem_rd", StringComparison.OrdinalIgnoreCase))
            {
                string binaryPath = string.Empty; 
                if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.X64)
                {
                    binaryPath = this.PlatformSpecifics.Combine(this.LMbenchPackage.Path, "bin", "x86_64-Linux");
                }
                else if (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.Arm64)
                {
                    binaryPath = this.PlatformSpecifics.Combine(this.LMbenchPackage.Path, "bin", "aarch64-Linux");
                }
                else
                {
                    this.Logger.LogNotSupported(this.BinaryName, this.Platform, this.CpuArchitecture, EventContext.Persisted());
                }

                if (!string.IsNullOrEmpty(binaryPath))
                {
                    using (IProcessProxy executeBinary = await this.ExecuteCommandAsync(this.PlatformSpecifics.Combine(binaryPath, this.BinaryName), this.BinaryCommandLine, binaryPath, telemetryContext, cancellationToken))
                    {
                        await this.LogProcessDetailsAsync(executeBinary, telemetryContext);
                        executeBinary.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        LatMemRdMetricsParser latMemRdMetricsParser = new LatMemRdMetricsParser($"{executeBinary.StandardOutput.ToString()}{executeBinary.StandardError.ToString()}");
                        this.CaptureMetrics(executeBinary, latMemRdMetricsParser, telemetryContext, this.BinaryName);
                    }
                }
            }
            else
            {
                this.Logger.LogMessage($"Unsupported {nameof(this.BinaryName)}: {this.BinaryName}, supported binarienames are : null(for sumarry),lat_rd_mem", telemetryContext);
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the LMbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The LMbench package contains source code. It is compiled on system and thus does not have platform/architecture
            // subdirectories
            this.LMbenchPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            // On Linux systems, in order to allow the various GCC executables to be used in compilation (e.g. make, config),
            // they must be attributed as executable.
            string scriptsPath = this.Combine(this.LMbenchPackage.Path, "scripts");
            await this.systemManagement.MakeFilesExecutableAsync(scriptsPath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("LMbench", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private Task BuildSourceCodeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "make";
            string commandArguments = $"build {this.CompilerFlags}".Trim();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.BuildSourceCode", relatedContext, async () =>
            {
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(command, commandArguments, this.LMbenchPackage.Path))
                {
                    this.CleanupTasks.Add(() => process?.SafeKill(this.Logger));
                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, "LMbench_Build");
                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                    }
                }
            });
        }

        private void CaptureMetrics(IProcessProxy process, MetricsParser metricsParser, EventContext telemetryContext, string scenario)
        {
            this.MetadataContract.AddForScenario(
                "LMbench",
                process.FullCommand(),
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            IList<Metric> metrics = metricsParser.Parse();

            this.Logger.LogMetrics(
                toolName: "LMbench",
                scenarioName: scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                metricCategorization: null,
                scenarioArguments: process.FullCommand(),
                this.Tags,
                telemetryContext);
        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "bash";
            string commandArguments = "-c \"echo -e '\n\n\n\n\n\n\n\n\n\n\n\n\nnone' | make results\"";

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(command, commandArguments, this.LMbenchPackage.Path))
                    {
                        if (this.MemorySizeMB != null)
                        {
                            // The $MB environment variable sets the size of the memory to run each
                            // benchmark against.
                            process.EnvironmentVariables["MB"] = this.MemorySizeMB.ToString();
                        }

                        if (this.Benchmarks?.Any() == true)
                        {
                            foreach (string benchmarkVariable in this.Benchmarks)
                            {
                                // e.g.
                                // BENCHMARK_MEM, BENCHMARK_MMAP, BENCHMARK_FILE
                                process.EnvironmentVariables[benchmarkVariable.Trim()] = "YES";
                            }
                        }

                        this.CleanupTasks.Add(() => process?.SafeKill(this.Logger));
                        await process.StartAndWaitAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, relatedContext, "LMbench");
                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }

                command = "make";
                commandArguments = $"summary";
                string resultsPath = this.Combine(this.LMbenchPackage.Path, "results");

                EventContext relatedContext2 = telemetryContext.Clone()
                    .AddContext("command", command)
                    .AddContext("commandArguments", commandArguments);

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(command, commandArguments, resultsPath))
                {
                    this.CleanupTasks.Add(() => process?.SafeKill(this.Logger));
                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext2, "LMbench_Summary");
                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                    }

                    // The use of the original telemetry context created at the top
                    // is purposeful.
                    LMbenchMetricsParser lmbenchMetricsParser = new LMbenchMetricsParser(process.StandardOutput.ToString());
                    this.CaptureMetrics(process, lmbenchMetricsParser, relatedContext, "Memory Benchmark");
                }
            });
        }
    }
}