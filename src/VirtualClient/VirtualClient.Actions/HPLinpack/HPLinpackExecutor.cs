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
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Contracts.Proxy;

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
        private long totalMemoryKilobytes;

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
                int size = Convert.ToInt32(Math.Sqrt(this.totalMemoryKilobytes * 1024 * 0.8 / 8));
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
        public string PerformanceLibraryPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.PerformanceLibraryPackageName), "hplperformancelibraries");
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
        protected string HPLinpackPackagePath { get; set; }

        /// <summary>
        /// The path to the HPL performance library package.
        /// </summary>
        protected string PerformanceLibraryPackagePath { get; set; }

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
            this.coreCount = this.cpuInfo.LogicalProcessorCount;

            MemoryInfo memoryInfo = await this.systemManagement.GetMemoryInfoAsync(CancellationToken.None);
            this.totalMemoryKilobytes = memoryInfo.TotalMemory;

            this.ValidateParameters();

            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.HPLinpackPackagePath = workloadPackage.Path;

            await this.MakeFilesExecutableAsync(this.HPLinpackPackagePath, this.Platform, cancellationToken);

            if (!string.IsNullOrWhiteSpace(this.PerformanceLibrary))
            {
                DependencyPath performanceLibrariesPackage = await this.packageManager.GetPackageAsync(this.PerformanceLibraryPackageName, cancellationToken);
                this.PerformanceLibraryPackagePath = performanceLibrariesPackage.Path;

                await this.MakeFilesExecutableAsync(this.PerformanceLibraryPackagePath, this.Platform, cancellationToken);
                await this.ConfigurePerformanceLibrary(telemetryContext, cancellationToken);
            }

            if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
            {
                string intelMklPath = null;
                if (this.PerformanceLibraryVersion == "2024.2.2.17")
                {
                    intelMklPath = "/opt/intel/oneapi/mkl/2024.2/share/mkl/benchmarks/mp_linpack";
                }
                else if (this.PerformanceLibraryVersion == "2025.1.0.803")
                {
                    string homeDirectory = this.PlatformSpecifics.GetEnvironmentVariable("HOME");
                    intelMklPath = $"{homeDirectory}/intel/oneapi/mkl/2025.1/share/mkl/benchmarks/mp_linpack";
                }
                else
                {
                    throw new WorkloadException(
                        $"The HPL workload currently only supports 2024.2.2.17 and 2025.1.0.803 versions of INTEL Math Kernel Library",
                        ErrorReason.NotSupported);
                }

                await this.fileSystem.CopyDirectoryAsync(intelMklPath, this.Combine(this.intelPerfLibrariesPath, "mp_linpack"));
            }

            string setupPath = this.Combine(this.HPLinpackPackagePath, "setup");
            await this.DeleteFileAsync(this.Combine(this.HPLinpackPackagePath, this.makeFileName));
            await this.DeleteFileAsync(this.Combine(setupPath, this.makeFileName));
            using (IProcessProxy bash = await this.ExecuteCommandAsync("bash", "-c \"source make_generic\"", setupPath, telemetryContext, cancellationToken))
            {
                bash.ThrowIfWorkloadFailed();
            }

            await this.MakeFilesExecutableAsync(setupPath, this.Platform, cancellationToken);
            await this.ConfigureMakeFileAsync(telemetryContext, cancellationToken);
            using (IProcessProxy ln = await this.ExecuteCommandAsync("ln", $"-s {this.Combine(setupPath, this.makeFileName)} {this.makeFileName}", this.HPLinpackPackagePath, telemetryContext, cancellationToken))
            {
                ln.ThrowIfWorkloadFailed();
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
                using (IProcessProxy make = await this.ExecuteCommandAsync("make", $"arch=Linux_GCC", this.HPLinpackPackagePath, telemetryContext, cancellationToken))
                {
                    make.ThrowIfWorkloadFailed();
                }

                await this.ConfigureDatFileAsync(telemetryContext, cancellationToken);
                IProcessProxy process = null;

                if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
                {
                    this.commandArguments = this.Combine(this.intelPerfLibrariesPath, "mp_linpack", "runme_intel64_dynamic");

                    string intelVars = null;
                    if (this.PerformanceLibraryVersion == "2024.2.2.17")
                    {
                        intelVars = "/opt/intel/oneapi/mpi/latest/env/vars.sh";
                    }
                    else if (this.PerformanceLibraryVersion == "2025.1.0.803")
                    {
                        string homeDirectory = this.PlatformSpecifics.GetEnvironmentVariable("HOME");
                        intelVars = $"{homeDirectory}/intel/oneapi/mpi/latest/env/vars.sh";
                    }

                    process = await this.ExecuteCommandAsync(
                        "bash", 
                        $"-c \"{intelVars} && {this.commandArguments}\"", 
                        this.Combine(this.intelPerfLibrariesPath, "mp_linpack"), 
                        telemetryContext, 
                        cancellationToken,
                        runElevated: true);
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

                    process = await this.ExecuteCommandAsync(
                        "runuser", 
                        $"-u {Environment.UserName} -- mpirun {this.commandArguments} ./xhpl", 
                        this.Combine(this.HPLinpackPackagePath, "bin", "Linux_GCC"), 
                        telemetryContext, 
                        cancellationToken,
                        runElevated: true);
                }

                using (process)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "HPLinpack");

                        process.ThrowIfWorkloadFailed();
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
                throw new WorkloadException(
                    $"The '{nameof(this.NumberOfProcesses)}' parameter value should be less than or equal to number of logical processors on the system.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task ConfigurePerformanceLibrary(EventContext telemetryContext, CancellationToken cancellationToken)
        {
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

                this.armPerfLibrariesPath = this.Combine(this.PerformanceLibraryPackagePath, "linux-arm64");
                using (IProcessProxy init = await this.ExecuteCommandAsync($"{this.Combine(this.armPerfLibrariesPath, this.hplArmPerfLibrary)}", $"-a", this.armPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true))
                {
                    init.ThrowIfWorkloadFailed();
                }
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
                            this.amdPerfLibrariesPath = this.Combine(this.PerformanceLibraryPackagePath, "linux-x64");
                            break;
                        default:
                            throw new WorkloadException($"The HPL workload currently only supports 4.2.0, 5.0.0 and 5.1.0 versions of AMD performance libraries");
                    }

                    using (IProcessProxy install = await this.ExecuteCommandAsync($"{this.Combine(this.amdPerfLibrariesPath, "install.sh")}", $"-t {this.HPLinpackPackagePath} -i lp64", this.amdPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true))
                    {
                        install.ThrowIfWorkloadFailed();
                    }
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

                    this.intelPerfLibrariesPath = this.Combine(this.PerformanceLibraryPackagePath, "linux-x64");

                    using (IProcessProxy intelMkl = await this.ExecuteCommandAsync($"{this.Combine(this.intelPerfLibrariesPath, this.hplIntelMKL)}", "-a --silent --eula accept", this.intelPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true))
                    {
                        intelMkl.ThrowIfWorkloadFailed();
                    }

                    using (IProcessProxy toolkit = await this.ExecuteCommandAsync($"{this.Combine(this.intelPerfLibrariesPath, this.hplIntelHpcToolkit)}", "-a --silent --eula accept", this.intelPerfLibrariesPath, telemetryContext, cancellationToken, runElevated: true))
                    {
                        toolkit.ThrowIfWorkloadFailed();
                    }
                }
            }
        }

        private async Task ConfigureMakeFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string makeFilePath = this.Combine(this.HPLinpackPackagePath, "setup", this.makeFileName);
            await this.ExecuteCommandAsync("mv", $"Make.UNKNOWN {this.makeFileName}", this.Combine(this.HPLinpackPackagePath, $"setup"), telemetryContext, cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"ARCH *= *[^\n]*", "ARCH = Linux_GCC", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"TOPdir *= *[^\n]*", $"TOPdir = {this.HPLinpackPackagePath}", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"CCFLAGS *= *[^\n]*", $"CCFLAGS = $(HPL_DEFS) {this.CCFlags}", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"CC *= *[^\n]*", "CC = mpicc", cancellationToken);

            if (this.PerformanceLibrary == "ARM" && this.CpuArchitecture == Architecture.Arm64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAdir *=", "LAdir = $(ARMPL_DIR)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAinc *=", $"LAinc = $(ARMPL_INCLUDES)", cancellationToken);

                switch (this.PerformanceLibraryVersion)
                {
                    case "23.04.1":
                        await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_23.04.1_gcc-11.3/lib/libarmpl.a", cancellationToken);
                        break;
                    case "24.10":
                        await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_24.10_gcc/lib/libarmpl.a", cancellationToken);
                        break;
                    case "25.04.1":
                        await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_25.04.1_gcc/lib/libarmpl.a", cancellationToken);
                        break;
                    default:
                        throw new WorkloadException(
                            $"The HPL workload is currently only supports the perf libraries versions 23.04.1, 24.10 and 25.04.1 on the following platform/architectures: " +
                            $"'{PlatformSpecifics.LinuxArm64}'.",
                            ErrorReason.PlatformNotSupported);
                }

                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LINKER *= *[^\n]*", "LINKER = mpifort", cancellationToken);
            }
            else if (this.PerformanceLibrary == "AMD" && this.CpuArchitecture == Architecture.X64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAdir *=", $"LAdir = {this.Combine(this.HPLinpackPackagePath, this.PerformanceLibraryVersion, "gcc")}", cancellationToken);
                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAlib *= *[^\n]*", $"LAlib = {this.Combine(this.HPLinpackPackagePath, this.PerformanceLibraryVersion, "gcc", "lib", "libblis.a")}", cancellationToken);
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

                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"MPinc *=", $"MPinc =  -I/usr/lib/{architecture}-linux-gnu/openmpi", cancellationToken);
                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"MPlib *=", $"MPlib =  /usr/lib/{architecture}-linux-gnu/openmpi/lib/libmpi.so", cancellationToken);
                await this.fileSystem.File.ReplaceInFileAsync(makeFilePath, @"LAinc *=", $"LAinc = -I/usr/lib/{architecture}-linux-gnu", cancellationToken);
            }
        }

        private async Task ConfigureDatFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string hplDatFile;
            if (this.CpuArchitecture == Architecture.X64 && this.PerformanceLibrary == "INTEL")
            {
                this.SetParameters(this.cpuInfo.SocketCount);
                hplDatFile = this.Combine(this.intelPerfLibrariesPath, "mp_linpack", "HPL.dat");

                string hplRunmeFile = this.Combine(this.intelPerfLibrariesPath, "mp_linpack", "runme_intel64_dynamic");
                await this.fileSystem.File.ReplaceInFileAsync(hplRunmeFile, @"export MPI_PROC_NUM *= *[^\n]*", $"export MPI_PROC_NUM={this.cpuInfo.SocketCount}", cancellationToken);
                await this.fileSystem.File.ReplaceInFileAsync(hplRunmeFile, @"export MPI_PER_NODE *= *[^\n]*", $"export MPI_PER_NODE={this.cpuInfo.SocketCount}", cancellationToken);
                await this.fileSystem.File.ReplaceInFileAsync(hplRunmeFile, @"export NUMA_PER_MPI *= *[^\n]*", $"export NUMA_PER_MPI={this.cpuInfo.SocketCount}", cancellationToken);
            }
            else
            {
                hplDatFile = this.Combine(this.HPLinpackPackagePath, "bin", "Linux_GCC", "HPL.dat");
                this.SetParameters(this.NumberOfProcesses);
            }

            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of problems sizes", $"1   # of problems sizes", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+Ns", $"{this.ProblemSizeN} Ns", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of NBs", $"1   # of NBs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+NBs", $"{this.BlockSizeNB} NBs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of process grids", $"1   # of process grids", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+Ps", $"{this.ProcessRows}  Ps", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+Qs", $"{this.ProcessColumns}  Qs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of recursive stopping criterium", $"1   # of recursive stopping criterium", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+PFACTs", $"0    PFACTs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of panel fact", $"1   # of panel fact", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+NBMINs", $"1  NBMINs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of panels in recursion", $"1   # of panels in recursion", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+NDIVs", $"2   NDIVs", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+# of recursive panel fact.", $"1   # of recursive panel fact.", cancellationToken);
            await this.fileSystem.File.ReplaceInFileAsync(hplDatFile, @"([0-9]+\s+)+RFACTs", $"2   RFACTs", cancellationToken);
        }

        private async Task DeleteFileAsync(string filePath)
        {
            if (this.systemManagement.FileSystem.File.Exists(filePath))
            {
                await this.systemManagement.FileSystem.File.DeleteAsync(filePath);
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
