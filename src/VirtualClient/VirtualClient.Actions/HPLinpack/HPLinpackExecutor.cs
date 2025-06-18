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
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class HPLinpackExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;
        private IPackageManager packageManager;
        private int coreCount;
        private string makeFileName = "Make.Linux_GCC";
        private string commandArguments;
        private string hplArmPerfLibrary;
        private string hplIntelMKL;
        private string hplIntelHpcToolkit;
        private string armPerfLibrariesPath;
        private string amdPerfLibrariesPath;
        private string intelPerfLibrariesPath;
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
                return this.Parameters.GetValue<int>(nameof(this.NumberOfProcesses), this.cpuInfo.LogicalProcessorCount);
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
        /// Perf library name like ARM, AMD, INTEL etc.
        /// </summary>
        public string PerformanceLibrary
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.PerformanceLibrary), out IConvertible performanceLibrary);
                return performanceLibrary?.ToString()?.ToUpperInvariant();
            }
        }

        /// <summary>
        /// Per lib version.
        /// </summary>
        public string PerformanceLibraryVersion
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.PerformanceLibraryVersion), out IConvertible performanceLibraryVersion);
                return performanceLibraryVersion?.ToString();
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
            this.coreCount = this.cpuInfo.LogicalProcessorCount;

            MemoryInfo memoryInfo = await this.systemManagement.GetMemoryInfoAsync(CancellationToken.None);
            this.totalMemoryKiloBytes = memoryInfo.TotalMemory;

            this.ValidateParameters();

            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.HPLDirectory = workloadPackage.Path;

            DependencyPath performanceLibrariesPackage = await this.packageManager.GetPackageAsync(this.PerformanceLibrariesPackageName, cancellationToken)
                    .ConfigureAwait(false);

            await this.ConfigurePerformanceLibrary(telemetryContext, cancellationToken);

            if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
            {
                // Use PlatformSpecifics.Combine for paths instead of string concatenation
                if (this.PerformanceLibraryVersion == "2024.2.2.17")
                {
                    string intelMklPath = "/opt/intel/oneapi/mkl/2024.2/share/mkl/benchmarks/mp_linpack";
                    await this.ExecuteCommandAsync("cp", $"-r {intelMklPath} {this.intelPerfLibrariesPath}", this.HPLDirectory, telemetryContext, cancellationToken);
                }
                else if (this.PerformanceLibraryVersion == "2025.1.0.803")
                {
                    string intelMklPath = "~/intel/oneapi/mkl/2025.1/share/mkl/benchmarks/mp_linpack";
                    await this.ExecuteCommandAsync("cp", $"-r {intelMklPath} {this.intelPerfLibrariesPath}", this.HPLDirectory, telemetryContext, cancellationToken);
                }
                else
                {
                    throw new WorkloadException($"The HPL workload currently only supports 2024.2.2.17 and 2025.1.0.803 versions of INTEL Math Kernel Library");
                }
            }
            else
            {
                await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, this.makeFileName));
                await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", this.makeFileName));
                await this.ExecuteCommandAsync("bash", "-c \"source make_generic\"", this.PlatformSpecifics.Combine(this.HPLDirectory, "setup"), telemetryContext, cancellationToken, runElevated: true);
                await this.ConfigureMakeFileAsync(telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("ln", $"-s {this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", this.makeFileName)} {this.makeFileName}", this.HPLDirectory, telemetryContext, cancellationToken);
            }
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

                await this.ConfigureDatFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                IProcessProxy process;

                if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
                {
                    this.commandArguments = "./runme_intel64_dynamic";
                    process = await this.ExecuteCommandAsync("bash", $"-c \". /opt/intel/oneapi/mpi/latest/env/vars.sh && {this.commandArguments}\"", this.PlatformSpecifics.Combine(this.intelPerfLibrariesPath, "mp_linpack"), telemetryContext, cancellationToken, runElevated: true);
                }
                else
                {
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
                }

                using (process)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "HPLinpack", logToFile: true)
                            .ConfigureAwait();

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        this.CaptureMetrics(process.StandardOutput.ToString(), $"{this.commandArguments}", startTime, DateTime.UtcNow, telemetryContext, cancellationToken);

                    }
                }
            }
        }

        private void SetParameters(int count)
        {
            // gives you P*Q = Number of processes( Default: Environment.ProcessorCount, overwrite to value from command line to VC if supplied) and P <= Q and  Q-P to be the minimum possible value.
            this.ProcessRows = 1;
            this.ProcessColumns = count;
            for (int i = 2; i <= Math.Sqrt(count); i++)
            {
                if (count % i == 0)
                {
                    int j = count / i;
                    if (j - i < this.ProcessColumns - this.ProcessRows)
                    {
                        this.ProcessRows = i;
                        this.ProcessColumns = j;
                    }
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
            DependencyPath performanceLibrariesPackage = await this.packageManager.GetPackageAsync(this.PerformanceLibrariesPackageName, cancellationToken)
                                                    .ConfigureAwait(false);

            if (this.CpuArchitecture == Architecture.Arm64 && this.PerformanceLibrary == "ARM")
            {
                // Switch between ARM perf lib versions
                switch (this.PerformanceLibraryVersion)
                {
                    case "23.04.1":
                        this.hplArmPerfLibrary = "arm-performance-libraries_23.04.1.sh";
                        break;
                    case "24.10":
                        this.hplArmPerfLibrary = "arm-performance-libraries_24.10.sh";
                        break;
                    case "25.04.1":
                        this.hplArmPerfLibrary = "arm-performance-libraries_25.04.1.sh";
                        break;
                    default:
                        throw new WorkloadException($"The HPL workload currently only supports versions 23.04.1, 24.10 and 25.04.1 of the ARM performance libraries");
                }

                this.armPerfLibrariesPath = this.PlatformSpecifics.Combine(performanceLibrariesPackage.Path, "ARM");
                await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.armPerfLibrariesPath, $"{this.hplArmPerfLibrary}"), this.Platform, cancellationToken).ConfigureAwait(false);
                await this.ExecuteCommandAsync($"./{this.hplArmPerfLibrary}", $"-a", this.armPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true);
            }

            if (this.CpuArchitecture == Architecture.X64)
            {
                if (this.PerformanceLibrary == "AMD")
                {
                    switch (this.PerformanceLibraryVersion)
                    {
                        case "4.2.0":
                        case "5.0.0":
                        case "5.1.0":
                            this.amdPerfLibrariesPath = this.PlatformSpecifics.Combine(performanceLibrariesPackage.Path, "AMD", this.PerformanceLibraryVersion);
                            break;
                        default:
                            throw new WorkloadException($"The HPL workload currently only supports 4.2.0, 5.0.0 and 5.1.0 versions of AMD performance libraries");
                    }

                    string installPath = this.PlatformSpecifics.Combine(this.HPLDirectory);
                    await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.amdPerfLibrariesPath, "install.sh"), this.Platform, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteCommandAsync($"./install.sh", $"-t {installPath} -i lp64", this.amdPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
                }

                if (this.PerformanceLibrary == "INTEL")
                {
                    // Switch between different versions of INTEL Math Kernel Library 
                    switch (this.PerformanceLibraryVersion)
                    {
                        case "2024.2.2.17":
                            this.hplIntelMKL = "l_onemkl_p_2024.2.2.17_offline.sh";
                            this.hplIntelHpcToolkit = "l_HPCKit_p_2024.2.1.79_offline.sh";
                            break;
                        case "2025.1.0.803":
                            this.hplIntelMKL = "intel-onemkl-2025.1.0.803_offline.sh";
                            this.hplIntelHpcToolkit = "intel-oneapi-hpc-toolkit-2025.1.3.10_offline.sh";
                            break;
                        default:
                            throw new WorkloadException($"The HPL workload currently only supports 2024.2.2.17 and 2025.1.0.803 versions of INTEL Math Kernel Library");
                    }

                    this.intelPerfLibrariesPath = this.PlatformSpecifics.Combine(performanceLibrariesPackage.Path, "INTEL", this.PerformanceLibraryVersion);
                    await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.intelPerfLibrariesPath, $"{this.hplIntelHpcToolkit}"), this.Platform, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteCommandAsync($"./{this.hplIntelHpcToolkit}", "-a --silent --eula accept", this.intelPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true);
                    await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.intelPerfLibrariesPath, $"{this.hplIntelMKL}"), this.Platform, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteCommandAsync($"./{this.hplIntelMKL}", "-a --silent --eula accept", this.intelPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true);
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

            if (this.PerformanceLibrary == "ARM" && this.CpuArchitecture == Architecture.Arm64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"LAdir *=", "LAdir = $(ARMPL_DIR)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", $"LAinc = $(ARMPL_INCLUDES)", cancellationToken);

                switch (this.PerformanceLibraryVersion)
                {
                    case "23.04.1":
                        await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_23.04.1_gcc-11.3/lib/libarmpl.a", cancellationToken);
                        break;
                    case "24.10":
                        await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_24.10_gcc/lib/libarmpl.a", cancellationToken);
                        break;
                    case "25.04.1":
                        await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_25.04.1_gcc/lib/libarmpl.a", cancellationToken);
                        break;
                    default:
                        throw new WorkloadException(
                            $"The HPL workload is currently only supports the perf libraries versions 23.04.1, 24.10 and 25.04.1 on the following platform/architectures: " +
                            $"'{PlatformSpecifics.LinuxArm64}'.",
                            ErrorReason.PlatformNotSupported);
                }

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LINKER *= *[^\n]*", "LINKER = mpifort", cancellationToken);
            }
            else if (this.PerformanceLibrary == "AMD" && this.CpuArchitecture == Architecture.X64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                  makeFilePath, @"LAdir *=", $"LAdir = {this.PlatformSpecifics.Combine(this.HPLDirectory, this.PerformanceLibraryVersion, "gcc")}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                 makeFilePath, @"LAlib *= *[^\n]*", $"LAlib = {this.PlatformSpecifics.Combine(this.HPLDirectory, this.PerformanceLibraryVersion, "gcc", "lib", "libblis.a")}", cancellationToken);
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

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", $"LAinc = -I/usr/lib/{architecture}-linux-gnu", cancellationToken);
            }
        }

        private async Task ConfigureDatFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string hplDatFile;
            if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
            {
                this.SetParameters(this.cpuInfo.SocketCount);
                hplDatFile = this.PlatformSpecifics.Combine(this.intelPerfLibrariesPath, "mp_linpack", "HPL.dat");

                string hplRunmeFile = this.PlatformSpecifics.Combine(this.intelPerfLibrariesPath, "mp_linpack", "runme_intel64_dynamic");
                await this.fileSystem.File.ReplaceInFileAsync(
                    hplRunmeFile, @"export MPI_PROC_NUM *= *[^\n]*", $"export MPI_PROC_NUM={this.cpuInfo.SocketCount}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                   hplRunmeFile, @"export MPI_PER_NODE *= *[^\n]*", $"export MPI_PER_NODE={this.cpuInfo.SocketCount}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                   hplRunmeFile, @"export NUMA_PER_MPI *= *[^\n]*", $"export NUMA_PER_MPI={this.cpuInfo.SocketCount}", cancellationToken);
            }
            else
            {
                hplDatFile = this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC", "HPL.dat");
                this.SetParameters(this.NumberOfProcesses);
            }

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
            HPLinpackMetricsParser parser = new HPLinpackMetricsParser(results);
            IList<Metric> metrics = parser.Parse();
            
            var additionalMetadata = new Dictionary<string, object>();
            additionalMetadata[$"{nameof(this.PerformanceLibrary)}"] = this.PerformanceLibrary;
            additionalMetadata[$"{nameof(this.PerformanceLibraryVersion)}"] = this.PerformanceLibraryVersion;

            // Add GCC version to metadata
            string gccVersion = this.GetGccVersion();
            additionalMetadata["GccVersion"] = gccVersion;
            
            if (this.PerformanceLibrary == "ARM")
            {
                additionalMetadata["Origin"] = "Netlib HPL configured with Arm Performance Libraries";
            }
            else if (this.PerformanceLibrary == "AMD" && this.CpuArchitecture == Architecture.X64)
            {
                additionalMetadata["Origin"] = "Netlib HPL configured with Amd Performance Libraries";
            }
            else if (this.PerformanceLibrary == "INTEL" && this.CpuArchitecture == Architecture.X64)
            {
                additionalMetadata["Origin"] = "Intel's Distro for HPL based on Netlib HPLinpack 2.3";
            }
            else
            {
                additionalMetadata["Origin"] = "Netlib HPL with no Performance Libraries";
            }

            this.MetadataContract.AddForScenario(
                "HPLinpack",
                commandArguments,
                toolVersion: parser.Version,
                additionalMetadata: additionalMetadata);

            this.MetadataContract.Apply(telemetryContext);

            foreach (Metric result in metrics)
            {
                this.Logger.LogMetric(  
                    "HPLinpack",
                    $"{this.Scenario}_{this.ProblemSizeN}N_{this.BlockSizeNB}NB_{this.ProcessRows}P_{this.ProcessColumns}Q",
                    startTime,
                    endTime,
                    result.Name,
                    result.Value,
                    result.Unit,
                    null,
                    $"[ -N {this.ProblemSizeN} -NB {this.BlockSizeNB} -P {this.ProcessRows} -Q {this.ProcessColumns} --perfLibrary={this.PerformanceLibrary} --perfLibraryVersion={this.PerformanceLibraryVersion} ]",
                    this.Tags,
                    telemetryContext,
                    result.Relativity,
                    metricMetadata: result.Metadata);
            }
        }

        private string GetGccVersion()
        {
            string version = string.Empty;
            try
            {
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess("gcc", "-dumpversion"))
                {
                    process.StartAndWaitAsync(CancellationToken.None).GetAwaiter().GetResult();
                    version = process.StandardOutput.ToString().Trim();
                }
            }
            catch
            {
                // If GCC is not installed or there's an error, return empty string
                version = string.Empty;
            }

            return version;
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
