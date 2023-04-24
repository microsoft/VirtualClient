// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
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
        }

        /// <summary>
        /// The user who has the ssh identity registered for.
        /// </summary>
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(HPLinpackExecutor.Username));
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = this.GetCurrentUserName(true);
                }

                return username;
            }
        }

        /// <summary>
        /// True if Hyperthreading is on
        /// </summary>
        public bool HyperThreadingON
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(HPLinpackExecutor.HyperThreadingON), true);
            }

            set
            {
                this.Parameters[nameof(HPLinpackExecutor.HyperThreadingON)] = value;
            }
        }

        /// <summary>
        /// The order of the coefficient matrix also known as problem size (N)
        /// </summary>
        public string ProblemSizeN
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HPLinpackExecutor.ProblemSizeN), Environment.ProcessorCount * 10000);
            }
        }

        /// <summary>
        /// The partitioning blocking factor also known as block size (NB)
        /// </summary>
        public string BlockSizeNB
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HPLinpackExecutor.BlockSizeNB), out IConvertible nb);
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
                return this.Parameters.GetValue<int>(nameof(HPLinpackExecutor.NumberOfProcesses), Environment.ProcessorCount);
            }
        }

        /// <summary>
        /// Version of HPL being used.
        /// </summary>
        public string Version
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HPLinpackExecutor.Version), out IConvertible version);
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
                this.Parameters.TryGetValue(nameof(HPLinpackExecutor.CCFlags), out IConvertible ccflags);
                return ccflags?.ToString();
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
            this.ThrowIfPlatformIsNotSupported();
            this.coreCount = Environment.ProcessorCount;

            this.ValidateParameters();

            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                                                    .ConfigureAwait(false);

            this.HPLDirectory = workloadPackage.Path;

            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, this.makeFileName))
                .ConfigureAwait(false);

            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", this.makeFileName))
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync("bash", "-c \"source make_generic\"", this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup"), telemetryContext, cancellationToken, runElevated: true);

            await this.ConfigureMakeFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

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

                await this.ExecuteCommandAsync("useradd", $" -m {this.Username}", this.HPLDirectory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait(false);

                this.SetParameters();
                await this.ConfigureDatFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                IProcessProxy process;
                if (this.HyperThreadingON)
                {
                    process = await this.ExecuteCommandAsync("runuser", $"-u {this.Username} -- mpirun --use-hwthread-cpus -np {this.NumberOfProcesses} ./xhpl", this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC"), telemetryContext, cancellationToken, runElevated: true);
                }
                else
                {
                    process = await this.ExecuteCommandAsync("runuser", $"-u {this.Username} -- mpirun -np {this.NumberOfProcesses} ./xhpl", this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC"), telemetryContext, cancellationToken, runElevated: true);
                }

                using (process)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ProcessDetails.ToolSet = "HPLinpack";
                        await this.LogProcessDetailsAsync(process.ProcessDetails, telemetryContext, logToFile: true)
                            .ConfigureAwait();

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        this.CaptureMetrics(process.StandardOutput.ToString(), startTime, DateTime.UtcNow, telemetryContext, cancellationToken);

                    }
                }
            }
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
            if (this.Platform != PlatformID.Unix)
            {
                throw new WorkloadException(
                    $"The HPL workload is currently only supported on the following platform/architectures: " +
                    $"'{PlatformSpecifics.LinuxX64}', '{PlatformSpecifics.LinuxArm64}'.",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private void ValidateParameters()
        {
            if (this.HyperThreadingON && this.NumberOfProcesses > this.coreCount)
            {
                throw new Exception(
                    $"NumberOfProcesses parameter value should be less than or equal to number of logical cores");
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

            await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"CCFLAGS *= *[^\n]*", $"CCFLAGS = $(HPL_DEFS) {this.CCFlags}", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"CC *= *[^\n]*", "CC = mpicc", cancellationToken); 
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

        private void CaptureMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
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
                    $"{this.ProblemSizeN}N_{this.BlockSizeNB}NB_{this.ProcessRows}P_{this.ProcessColumns}Q",
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
