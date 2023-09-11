// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The HPL(High Performance Linpack) workload executor.
    /// </summary>
    [UnixCompatible]
    public class HPLinpackExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;
        private IPackageManager packageManager;
        private int coreCount;
        private string makeFileName = "Make.Linux_GCC";
        private string commandArguments;
        private string hplPerfLibraryInfo;
        private CpuInfo cpuInfo;
        private long totalMemoryKiloBytes;

        /// <summary>
        /// Constructor for <see cref="HPLinpackExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HPLinpackExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
            this.stateManager = this.systemManagement.StateManager;
            this.cpuInfo = this.systemManagement.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Parameter defines whether to use perf libraries or not.
        /// </summary>
        public bool UsePerformanceLibraries
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.UsePerformanceLibraries), false);
            }
        }

        /// <summary>
        /// The order of the coefficient matrix also known as problem size (N)
        /// </summary>
        public string ProblemSizeN
        {
            get
            {
                // HPLinpack problemSize could take 80% of total available memory for optimal performance.-> xKiloByte * 0.8
                // Number of double precision(8  bytes) elements that can fit the available memory -> ( xKiloByte * 0.8 ) / 8 -> ( xByte * 1024 * 0.8 ) /8
                // The memory the benchmark uses is propotional to the 2nd-power of the size. -> sqrt( ( xByte * 1024 * 0.8 ) /8)
                int size = Convert.ToInt32(Math.Sqrt(this.totalMemoryKiloBytes * 1024 * 0.8 / 8));
                return this.Parameters.GetValue<string>(nameof(this.ProblemSizeN), size);
            }
        }

        /// <summary>
        /// The partitioning blocking factor also known as block size (NB)
        /// </summary>
        public string BlockSizeNB
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.BlockSizeNB), out IConvertible nb);
                return nb?.ToString();
            }
        }

        /// <summary>
        /// The number of instances of program to start. Defaults to number of logical processor count and can be overwritten from command line.
        /// </summary>
        public int NumberOfProcesses
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.NumberOfProcesses), this.cpuInfo.LogicalCoreCount);
            }
        }

        /// <summary>
        /// Version of HPL being used.
        /// </summary>
        public string Version
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Version), out IConvertible version);
                return version?.ToString();
            }
        }

        /// <summary>
        /// Compiler flags.
        /// </summary>
        public string CCFlags
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.CCFlags), out IConvertible ccflags);
                return ccflags?.ToString();
            }
        }

        /// <summary>
        /// The name of the package where the ARMPerformanceLibraries package is downloaded.
        /// </summary>
        public string PerformanceLibrariesPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.PerformanceLibrariesPackageName), "hplperformancelibraries");
            }
        }

        /// <summary>
        /// Parameter defines whether to bind the a process to cores on the system.
        /// </summary>
        public bool BindToCores
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.BindToCores), true);
            }
        }

        /// <summary>
        /// The path to the HPL directory.
        /// </summary>
        protected string HPLDirectory { get; set; }

        /// <summary>
        /// The number of Process rows(P).
        /// </summary>
        protected int ProcessRows { get; set; }

        /// <summary>
        /// The number of Process columns(Q).
        /// </summary>
        protected int ProcessColumns { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the HPL workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);
            this.ThrowIfPlatformIsNotSupported();
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken);
            this.coreCount = this.cpuInfo.LogicalCoreCount;

            MemoryInfo memoryInfo = await this.systemManagement.GetMemoryInfoAsync(CancellationToken.None);
            this.totalMemoryKiloBytes = memoryInfo.TotalMemory;

            this.ValidateParameters();

            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken);

            this.HPLDirectory = workloadPackage.Path;

            await this.ConfigurePerformanceLibrary(telemetryContext, cancellationToken);
            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, this.makeFileName));
            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", this.makeFileName));

            await this.ExecuteCommandAsync("bash", "-c \"source make_generic\"", this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup"), telemetryContext, cancellationToken, runElevated: true);
            await this.ConfigureMakeFileAsync(telemetryContext, cancellationToken);
            await this.ExecuteCommandAsync("ln", $"-s {this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup", this.makeFileName)} {this.makeFileName}", this.HPLDirectory, telemetryContext, cancellationToken);

        }

        /// <summary>
        /// Executes the HPL workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                DateTime startTime = DateTime.UtcNow;
                await this.ExecuteCommandAsync("make", $"arch=Linux_GCC", this.HPLDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                this.SetParameters();
                await this.ConfigureDatFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                IProcessProxy process;

                if (this.cpuInfo.IsHyperthreadingEnabled)
                {
                    this.commandArguments = $"--use-hwthread-cpus -np {this.NumberOfProcesses} --allow-run-as-root";
                }
                else
                {
                    this.commandArguments = $"-np {this.NumberOfProcesses} --allow-run-as-root";
                }

                if (this.BindToCores)
                {
                    this.commandArguments += $" --bind-to core";
                }

                process = await this.ExecuteCommandAsync("runuser", $"-u {Environment.UserName} -- mpirun {this.commandArguments} ./xhpl", this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC"), telemetryContext, cancellationToken, runElevated: true);

                using (process)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "HPLinpack", logToFile: true)
                            .ConfigureAwait();

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        this.CaptureMetrics(process.StandardOutput.ToString(), $"runuser {this.commandArguments}", startTime, DateTime.UtcNow, telemetryContext, cancellationToken);

                    }
                }
            }
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
                this.Logger.LogNotSupported("HPLinPack", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private void SetParameters()
        {
            // gives you P*Q = Number of processes( Default: Environment.ProcessorCount, overwrite to value from command line to VC if supplied) and P <= Q and  Q-P to be the minimum possible value.
            this.ProcessRows = 1;
            this.ProcessColumns = this.NumberOfProcesses;
            for (int i = 2; i <= Math.Sqrt(this.NumberOfProcesses); i++)
            {
                if (this.NumberOfProcesses % i == 0)
                {
                    int j = this.NumberOfProcesses / i;
                    if (j - i < this.ProcessColumns - this.ProcessRows)
                    {
                        this.ProcessRows = i;
                        this.ProcessColumns = j;
                    }
                }
            }
        }

        private void ThrowIfPlatformIsNotSupported()
        {
            if (this.Platform == PlatformID.Unix && this.CpuArchitecture != Architecture.Arm64 && this.UsePerformanceLibraries == true)
            {
                throw new WorkloadException(
                    $"The HPL workload with performance Libraries is currently only supported on the following platform/architectures: " +
                    $"'{PlatformSpecifics.LinuxArm64}'",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait();

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The HPLinpack benchmark workload is not supported by Virtual Client on the current Linux distro " +
                            $"'{distroInfo.LinuxDistribution}'.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        private void ValidateParameters()
        {
            if (this.cpuInfo.IsHyperthreadingEnabled && this.NumberOfProcesses > this.coreCount)
            {
                throw new Exception(
                    $"NumberOfProcesses parameter value should be less than or equal to number of logical cores");
            }
        }

        private async Task ConfigurePerformanceLibrary(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.UsePerformanceLibraries)
            {
                if (this.CpuArchitecture == Architecture.Arm64)
                {
                    this.hplPerfLibraryInfo = "arm-performance-libraries_23.04.1_Ubuntu-22.04";
                    DependencyPath performanceLibrariesPackage = await this.packageManager.GetPackageAsync(this.PerformanceLibrariesPackageName, cancellationToken)
                                                                        .ConfigureAwait(false);

                    string armperfLibrariesPath = this.PlatformSpecifics.Combine(performanceLibrariesPackage.Path, "ARM");
                    await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(armperfLibrariesPath, "arm-performance-libraries_23.04.1_Ubuntu-22.04.sh"), this.Platform, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteCommandAsync($"./arm-performance-libraries_23.04.1_Ubuntu-22.04.sh", $"-a", armperfLibrariesPath, telemetryContext, cancellationToken, runElevated: true);
                }
            }           
        }

        private async Task ConfigureMakeFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string makeFilePath = this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", this.makeFileName);
            await this.ExecuteCommandAsync("mv", $"Make.UNKNOWN {this.makeFileName}", this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup"), telemetryContext, cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"ARCH *= *[^\n]*", "ARCH = Linux_GCC", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"TOPdir *= *[^\n]*", $"TOPdir = {this.HPLDirectory}", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                            makeFilePath, @"CCFLAGS *= *[^\n]*", $"CCFLAGS = $(HPL_DEFS) {this.CCFlags}", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"CC *= *[^\n]*", "CC = mpicc", cancellationToken);

            if (this.UsePerformanceLibraries && this.CpuArchitecture == Architecture.Arm64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"LAdir *=", "LAdir = $(ARMPL_DIR)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", $"LAinc = $(ARMPL_INCLUDES)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_23.04.1_gcc-11.3/lib/libarmpl.a", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LINKER *= *[^\n]*", "LINKER = mpifort", cancellationToken);
            }
            else if (this.UsePerformanceLibraries && this.CpuArchitecture != Architecture.Arm64)
            {
                throw new WorkloadException(
                    $"The HPL workload is currently only supports with perf libraries on the following platform/architectures: " +
                    $"'{PlatformSpecifics.LinuxArm64}'.",
                    ErrorReason.PlatformNotSupported);
            }
            else 
            {
                string architecture;
                if (this.CpuArchitecture == Architecture.Arm64)
                {
                    architecture = "aarch64";
                }
                else
                {
                    architecture = "x86_64";
                }

                await this.fileSystem.File.ReplaceInFileAsync(
                            makeFilePath, @"MPinc *=", $"MPinc =  -I/usr/lib/{architecture}-linux-gnu/openmpi", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"MPlib *=", $"MPlib =  /usr/lib/{architecture}-linux-gnu/openmpi/lib/libmpi.so", cancellationToken);

                // /usr/lib/x86_64-linux-gnu/openmpi

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", $"LAinc = -I/usr/lib/{architecture}-linux-gnu", cancellationToken);
            }
        }

        private async Task ConfigureDatFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string hplDatFile = this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC", "HPL.dat");
            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of problems sizes", $"1   # of problems sizes", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+Ns", $"{this.ProblemSizeN} Ns", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of NBs", $"1   # of NBs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+NBs", $"{this.BlockSizeNB} NBs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of process grids", $"1   # of process grids", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+Ps", $"{this.ProcessRows}  Ps", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+Qs", $"{this.ProcessColumns}  Qs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of recursive stopping criterium", $"1   # of recursive stopping criterium", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+PFACTs", $"0    PFACTs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of panel fact", $"1   # of panel fact", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+NBMINs", $"1  NBMINs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of panels in recursion", $"1   # of panels in recursion", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+NDIVs", $"2   NDIVs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of recursive panel fact.", $"1   # of recursive panel fact.", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+RFACTs", $"2   RFACTs", cancellationToken);

        }

        private async Task DeleteFileAsync(string filePath)
        {
            if (this.systemManagement.FileSystem.File.Exists(filePath))
            {
                await this.systemManagement.FileSystem.File.DeleteAsync(filePath)
                    .ConfigureAwait(false);
            }
        }

        private void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.MetadataContract.AddForScenario(
                "HPLinpack",
                commandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            HPLinpackMetricsParser parser = new HPLinpackMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            foreach (Metric result in metrics)
            {
                this.Logger.LogMetrics(
                    "HPLinpack",
                    $"{this.Scenario}_{this.ProblemSizeN}N_{this.BlockSizeNB}NB_{this.ProcessRows}P_{this.ProcessColumns}Q",
                    startTime,
                    endTime,
                    result.Name,
                    result.Value,
                    result.Unit,
                    null,
                    $"[ -N {this.ProblemSizeN} -NB {this.BlockSizeNB} -P {this.ProcessRows} -Q {this.ProcessColumns} {this.commandArguments} --perfLibrary={this.hplPerfLibraryInfo} ]",
                    this.Tags,
                    telemetryContext,
                    result.Relativity,
                    metricMetadata: result.Metadata);
            }
        }

        internal class HPLinpackState : State
        {
            public HPLinpackState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool HPLInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HPLinpackState.HPLInitialized), false);
                }

                set
                {
                    this.Properties[nameof(HPLinpackState.HPLInitialized)] = value;
                }
            }
        }
    }
}
