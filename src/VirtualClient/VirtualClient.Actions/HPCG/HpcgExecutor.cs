// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

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
                DateTime startTime = DateTime.UtcNow;
                await this.ExecuteCommandAsync("bash", this.hpcgRunShellPath, this.hpcgDirectory, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;
                this.LogHpcgOutput(startTime, endTime, telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Hpcg workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath spackExecutable = await this.packageManager.GetPackageAsync(this.SpackPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (spackExecutable == null || !spackExecutable.Metadata.ContainsKey(PackageMetadata.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected spack executable does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.spackFilePath = spackExecutable.Metadata[PackageMetadata.ExecutablePath].ToString();
            this.spackDirectory = spackExecutable.Path.ToString();

            if (!this.fileSystem.Directory.Exists(this.hpcgDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.hpcgDirectory);
            }

            await this.WriteHpcgDatFileAsync(cancellationToken).ConfigureAwait(false);
            await this.WriteHpcgRunShellAsync(cancellationToken).ConfigureAwait(false);
            await this.systemManagement.MakeFileExecutableAsync(this.hpcgRunShellPath, this.Platform, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            string output = string.Empty;
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(HpcgExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<HpcgExecutor>(process, telemetryContext);
                            output = process.StandardOutput.ToString();
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }

            return output;
        }

        private void LogHpcgOutput(DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // HPCG-Benchmark_3.1_2022-04-26_04-49-59.txt
                string[] outputFiles = this.fileSystem.Directory.GetFiles(this.hpcgDirectory, "HPCG-Benchmark*.txt", SearchOption.TopDirectoryOnly);

                foreach (string file in outputFiles)
                {
                    string text = this.fileSystem.File.ReadAllText(file);
                    try
                    {
                        HpcgMetricsParser parser = new HpcgMetricsParser(text);
                        this.Logger.LogMetrics(
                            toolName: "Hpcg",
                            scenarioName: "Hpcg",
                            startTime,
                            endTime,
                            parser.Parse(),
                            metricCategorization: "Hpcg",
                            scenarioArguments: $"{this.runShellText}|{this.hpcgDatText}",
                            this.Tags,
                            telemetryContext);

                        this.fileSystem.File.Delete(file);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadException($"Failed to parse file at '{file}' with text '{text}'.", exc, ErrorReason.InvalidResults);
                    }
                }
            }
        }

        private async Task WriteHpcgDatFileAsync(CancellationToken cancellationToken)
        {
            string filePath = this.PlatformSpecifics.Combine(this.hpcgDirectory, "hpcg.dat");
            if (!this.fileSystem.File.Exists(filePath))
            {
                long totalMemoryKiloBytes = this.systemManagement.GetTotalSystemMemoryKiloBytes();
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
                int coreCount = this.systemManagement.GetSystemCoreCount();
                string mpirunCommand = $"mpirun --np {coreCount} --use-hwthread-cpus --allow-run-as-root xhpcg";

                this.runShellText = string.Join(Environment.NewLine, spackSetupCommand, installCommand, loadCommand, mpirunCommand);

                await this.systemManagement.FileSystem.File.WriteAllTextAsync(this.hpcgRunShellPath, this.runShellText, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}