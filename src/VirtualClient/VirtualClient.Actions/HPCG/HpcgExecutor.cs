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
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Hpcg workload executor.
    /// </summary>
    public class HpcgExecutor : VirtualClientComponent
    {
        private const int DurationInSecond = 1800;
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        private string spackFilePath;
        private string spackDirectory;
        private string hpcgRunShellPath;
        private string hpcgDirectory;
        private string runShellText;
        private string hpcgDatText;

        /// <summary>
        /// Constructor for <see cref="HpcgExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HpcgExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.stateManager = this.systemManagement.StateManager;
            this.fileSystem = this.systemManagement.FileSystem;

            this.hpcgDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, this.PackageName);
            this.hpcgRunShellPath = this.PlatformSpecifics.Combine(this.hpcgDirectory, "runhpcg.sh");
        }

        /// <summary>
        /// The HPCG version.
        /// </summary>
        public string HpcgVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HpcgExecutor.HpcgVersion), "3.1");
            }
        }

        /// <summary>
        /// The name of the package where the Hpcg package is downloaded.
        /// </summary>
        public string OpenMpiVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HpcgExecutor.OpenMpiVersion), "4.1.1");
            }
        }

        /// <summary>
        /// The name of the Linux spack package manager package.
        /// </summary>
        public string SpackPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HpcgExecutor.SpackPackageName));
            }
        }

        /// <summary>
        /// Executes the Hpcg workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.ExecuteWorkloadAsync(cancellationToken).ConfigureAwait();
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Hpcg workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath spackPackage = await this.packageManager.GetPackageAsync(this.SpackPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (spackPackage == null)
            {
                throw new DependencyException(
                    $"The expected spack executable does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.spackDirectory = spackPackage.Path.ToString();
            this.spackFilePath = this.PlatformSpecifics.Combine(this.spackDirectory, "bin", "spack");

            if (!this.fileSystem.Directory.Exists(this.hpcgDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.hpcgDirectory);
            }

            await this.WriteHpcgDatFileAsync(cancellationToken).ConfigureAwait();
            await this.WriteHpcgRunShellAsync(cancellationToken).ConfigureAwait();

            await this.systemManagement.MakeFileExecutableAsync(this.hpcgRunShellPath, this.Platform, cancellationToken)
                .ConfigureAwait();
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
                this.Logger.LogNotSupported("Hpcg", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<string> outputFiles = this.GetHpcgResultsFiles();

                if (outputFiles?.Any() == true)
                {
                    foreach (string file in outputFiles)
                    {
                        string results = await this.LoadResultsAsync(file, cancellationToken);

                        await this.LogProcessDetailsAsync(process, telemetryContext, "Hpcg", results: results.AsArray(), logToFile: true);

                        this.MetadataContract.AddForScenario(
                            "HPCG",
                            $"{this.runShellText}|{this.hpcgDatText}",
                            toolVersion: null,
                            this.PackageName);

                        this.MetadataContract.Apply(telemetryContext);

                        HpcgMetricsParser parser = new HpcgMetricsParser(results);
                        IList<Metric> metrics = parser.Parse();

                        this.Logger.LogMetrics(
                            toolName: "HPCG",
                            scenarioName: "Hpcg",
                            process.StartTime,
                            process.ExitTime,
                            metrics,
                            null,
                            scenarioArguments: $"{this.runShellText}|{this.hpcgDatText}",
                            this.Tags,
                            telemetryContext);

                        await this.fileSystem.File.DeleteAsync(file);
                    }
                }
            }
        }

        private async Task ExecuteWorkloadAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", "bash")
                    .AddContext("commandArguments", this.hpcgRunShellPath);

                await this.Logger.LogMessageAsync($"{nameof(HpcgExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync("bash", this.hpcgRunShellPath, this.hpcgDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (process.IsErrored())
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Hpcg", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }

                            await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken).ConfigureAwait();
                        }
                    }
                }).ConfigureAwait();
            }
        }

        private IEnumerable<string> GetHpcgResultsFiles()
        {
            // HPCG-Benchmark_3.1_2022-04-26_04-49-59.txt
            return this.fileSystem.Directory.GetFiles(this.hpcgDirectory, "HPCG-Benchmark*.txt", SearchOption.TopDirectoryOnly);
        }

        private async Task WriteHpcgDatFileAsync(CancellationToken cancellationToken)
        {
            string filePath = this.PlatformSpecifics.Combine(this.hpcgDirectory, "hpcg.dat");
            if (!this.fileSystem.File.Exists(filePath))
            {
                MemoryInfo memoryInfo = await this.systemManagement.GetMemoryInfoAsync(CancellationToken.None);
                long totalMemoryKiloBytes = memoryInfo.TotalMemory;

                // The standard of the HPCG size is to set it to consume 25% of the total memory.
                // The memory the benchmark uses is propotional to the 3rd-power of the size.
                // In another word, the size is propotional to the cubic root of total memory.
                // 200 * 200 * 200 = 8M size cost about 104GB of memory. Each size cost  3.4KB
                // 160 * 160 * 160 = 4M size cost about 52GB of memory. Each size cost  3.4KB
                // The size needs to be dividable by 8. So it needs the /8 inside toint() and *8 outside.
                int size = Convert.ToInt32(Math.Cbrt(totalMemoryKiloBytes * 0.25 / 3.4) / 8) * 8;

                this.hpcgDatText = "HPCG benchmark input file" + Environment.NewLine
                    + "HPC Benchmarking team, Microsoft Azure" + Environment.NewLine
                    + $"{size} {size} {size}" + Environment.NewLine
                    + $"{HpcgExecutor.DurationInSecond}";

                await this.systemManagement.FileSystem.File.WriteAllTextAsync(filePath, this.hpcgDatText, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task WriteHpcgRunShellAsync(CancellationToken cancellationToken)
        {
            if (!this.fileSystem.File.Exists(this.hpcgRunShellPath))
            {
                // . spack/share/spack/setup-env.sh
                string spackSetupCommand = $". {this.spackDirectory}/share/spack/setup-env.sh";

                // If gcc>= 9, use zen2. if <=8, use zen.
                // spack install -n -y hpcg %gcc@10.3.0 +openmp target=zen2 ^openmpi@4.1.1
                string installCommand = $"spack install --reuse -n -y hpcg@{this.HpcgVersion} %gcc +openmp ^openmpi@{this.OpenMpiVersion}";

                // spack load hpcg %gcc@10.3.0
                string loadCommand = $"spack load hpcg@{this.HpcgVersion} %gcc ^openmpi@{this.OpenMpiVersion}";

                // mpirun --np 4 --use-hwthread-cpus xhpcg
                CpuInfo cpuInfo = await this.systemManagement.GetCpuInfoAsync(CancellationToken.None);
                int coreCount = cpuInfo.PhysicalCoreCount;
                string mpirunCommand = $"mpirun --np {coreCount} --use-hwthread-cpus --allow-run-as-root xhpcg";

                this.runShellText = string.Join(Environment.NewLine, spackSetupCommand, installCommand, loadCommand, mpirunCommand);

                await this.systemManagement.FileSystem.File.WriteAllTextAsync(this.hpcgRunShellPath, this.runShellText, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}