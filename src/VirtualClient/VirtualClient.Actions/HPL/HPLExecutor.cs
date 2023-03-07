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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.LzbenchExecutor;

    /// <summary>
    /// The HPL(High Performance Linpack) workload executor.
    /// </summary>
    [UnixCompatible]
    public class HPLExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;
        private IPackageManager packageManager;

        /// <summary>
        /// Constructor for <see cref="HPLExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HPLExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// The order of the coefficient matrix also known as problem size (N)
        /// </summary>
        public string N
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HPLExecutor.N), out IConvertible n);
                return n?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.N)] = value;
            }
        }

        /// <summary>
        /// The partitioning blocking factor also known as block size (NB)
        /// </summary>
        public string NB
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HPLExecutor.NB), out IConvertible nb);
                return nb?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.NB)] = value;
            }
        }

        /// <summary>
        /// The name of the package where the ARMPerformanceLibraries package is downloaded.
        /// </summary>
        public string ARMPerformanceLibrariesPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HPLExecutor.ARMPerformanceLibrariesPackageName), "arm_performance_libraries");
            }
        }

        /// <summary>
        /// Version of HPL being used
        /// </summary>
        public string HPLVersion
        {
            get
            {
                this.Parameters.TryGetValue(nameof(HPLExecutor.HPLVersion), out IConvertible hplversion);
                return hplversion?.ToString();
            }
        }

        /// <summary>
        /// The path where the HPL directory.
        /// </summary>
        public string HPLDirectory { get; set; }

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

            if (this.CpuArchitecture == Architecture.Arm64)
            {
                DependencyPath armPerformanceLibrariesPackage = await this.packageManager.GetPackageAsync(this.ARMPerformanceLibrariesPackageName, cancellationToken)
                .ConfigureAwait(false);
                string armPackageLibrariesPath = armPerformanceLibrariesPackage.Path;
                await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(armPackageLibrariesPath, "arm-performance-libraries_22.1_Ubuntu-20.04.sh"), this.Platform, cancellationToken).ConfigureAwait(false);

                await this.ExecuteElevatedCommandAsync($"./arm-performance-libraries_22.1_Ubuntu-20.04.sh", $"-a", armPackageLibrariesPath, telemetryContext, cancellationToken);
            }
            
            HPLState state = await this.stateManager.GetStateAsync<HPLState>($"{nameof(HPLState)}", cancellationToken)
                ?? new HPLState();

            if (!state.HPLInitialized)
            {
                await this.ExecuteCommandAsync("wget", $"http://www.netlib.org/benchmark/hpl/hpl-{this.HPLVersion}.tar.gz -O {this.PackageName}.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("tar", $"-zxvf {this.PackageName}.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken);
                state.HPLInitialized = true;

            }

            this.HPLDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, $"hpl-{this.HPLVersion}");
            await this.stateManager.SaveStateAsync<HPLState>($"{nameof(HPLState)}", state, cancellationToken);

            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, "Make.Linux_GCC"))
                .ConfigureAwait(false);

            await this.DeleteFileAsync(this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", "Make.Linux_GCC"))
                .ConfigureAwait(false);

            await this.ExecuteElevatedCommandAsync("bash", "-c \"source make_generic\"", this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup"), telemetryContext, cancellationToken);

            await this.ConfigureMakeFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            await this.ExecuteCommandAsync("ln", $"-s {this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup", $"Make.Linux_GCC ")} Make.Linux_GCC", this.HPLDirectory, telemetryContext, cancellationToken);

        }

        /// <summary>
        /// Executes the HPL workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DateTime startTime = DateTime.UtcNow;
            await this.ExecuteCommandAsync("make", $"arch=Linux_GCC", this.HPLDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.SetParameters();
            await this.ConfigureHPLdatFileAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            string results;
            if (this.CpuArchitecture == Architecture.X64)
            {
                results = await this.ExecuteElevatedCommandAsync("runuser", $"-u azureuser -- mpirun --use-hwthread-cpus -np {Environment.ProcessorCount} ./xhpl", this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC"), telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            }
            else
            {
                results = await this.ExecuteElevatedCommandAsync("runuser", $"-u azureuser -- mpirun -np {Environment.ProcessorCount} ./xhpl", this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC"), telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            }

            DateTime endTime = DateTime.UtcNow;
            this.LogMetrics(results, startTime, endTime, telemetryContext, cancellationToken);
        }

        private void SetParameters()
        {
            if (this.N == null)
            {
                this.N = (Environment.ProcessorCount * 10000).ToString();
            }

            // gives you P*Q = Environment.ProcessorCount and P <= Q and  Q-P to be the minimum possible value.
            this.ProcessRows = 1;
            this.ProcessColumns = Environment.ProcessorCount;
            for (int i = 2; i <= Math.Sqrt(Environment.ProcessorCount); i++)
            {
                if (Environment.ProcessorCount % i == 0)
                {
                    int j = Environment.ProcessorCount / i;
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
                    $"'{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}', '{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}'. ",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private async Task ConfigureMakeFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string makeFilePath = this.PlatformSpecifics.Combine(this.HPLDirectory, "setup", "Make.Linux_GCC");
            await this.ExecuteCommandAsync("mv", $"Make.UNKNOWN Make.Linux_GCC", this.PlatformSpecifics.Combine(this.HPLDirectory, $"setup"), telemetryContext, cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"ARCH *= *[^\n]*", "ARCH = Linux_GCC", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"TOPdir *= *[^\n]*", $"TOPdir = {this.HPLDirectory}", cancellationToken);

            if (this.CpuArchitecture == Architecture.Arm64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"LAdir *=", "LAdir = $(ARMPL_DIR)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", $"LAinc = $(ARMPL_INCLUDES)", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAlib *= *[^\n]*", "LAlib = /opt/arm/armpl_22.1_gcc-11.2/lib/libarmpl.a", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LINKER *= *[^\n]*", "LINKER = mpifort", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"CCFLAGS *= *[^\n]*", "CCFLAGS = $(HPL_DEFS) -Ofast -march=armv8-a", cancellationToken);
            }
            else if (this.CpuArchitecture == Architecture.X64)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"MPinc *=", $"MPinc =  -I/usr/lib/x86_64-linux-gnu/openmpi", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"MPlib *=", "MPlib =  /usr/lib/x86_64-linux-gnu/openmpi/lib/libmpi.so", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        makeFilePath, @"LAinc *=", "LAinc = -I/usr/lib/x86_64-linux-gnu", cancellationToken);
            }

            await this.fileSystem.File.ReplaceInFileAsync(
                    makeFilePath, @"CC *= *[^\n]*", "CC = mpicc", cancellationToken); 
        }

        private async Task ConfigureHPLdatFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string hplDatFile = this.PlatformSpecifics.Combine(this.HPLDirectory, "bin", "Linux_GCC", "HPL.dat");
            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of problems sizes", $"1   # of problems sizes", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+Ns", $"{this.N} Ns", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+# of NBs", $"1   # of NBs", cancellationToken);

            await this.fileSystem.File.ReplaceInFileAsync(
                    hplDatFile, @"([0-9]+\s+)+NBs", $"{this.NB} NBs", cancellationToken);

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

        private async Task<string> ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string output = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");
                EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", pathToExe)
                .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(HPLExecutor)}.ExecuteWorkload", relatedContext, async () =>
                {
                    DateTime start = DateTime.Now;

                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(pathToExe, commandLineArguments, workingDirectory))
                    {
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());

                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<HPLExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();

                    }
                }).ConfigureAwait(false);
            }

            return output;
        }

        private async Task<string> ExecuteElevatedCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string output = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(HPLExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<HPLExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();

                    }
                }).ConfigureAwait(false);
            }

            return output;

        }

        private void LogMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            HPLMetricsParser parser = new HPLMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            foreach (Metric result in metrics)
            {
                this.Logger.LogMetrics(
                    "HPL",
                    this.Scenario,
                    startTime,
                    endTime,
                    result.Name,
                    result.Value,
                    result.Unit,
                    null,
                    $"{this.N}_{this.NB}_{this.ProcessRows}_{this.ProcessColumns}",
                    this.Tags,
                    telemetryContext,
                    result.Relativity);
            }
        }

        internal class HPLState : State
        {
            public HPLState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool HPLInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HPLState.HPLInitialized), false);
                }

                set
                {
                    this.Properties[nameof(HPLState.HPLInitialized)] = value;
                }
            }
        }
    }
}
