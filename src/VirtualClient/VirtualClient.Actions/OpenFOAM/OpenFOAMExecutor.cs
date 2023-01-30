﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    /// OpenFOAM workload executor.
    /// </summary>
    [UnixCompatible]
    public class OpenFOAMExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private List<string> executionCommands;
        private List<string> executables;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public OpenFOAMExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Path to OpenFOAM Package.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// The path to the OpenFOAM executable.
        /// </summary>
        public string AllRunExecutablePath { get; set; }

        /// <summary>
        /// The path to the OpenFOAM executable Wrapper.
        /// </summary>
        public string AllRunWrapperExecutablePath { get; set; }

        /// <summary>
        /// The path to the OpenFOAM cleaning logs executable.
        /// </summary>
        public string AllCleanExecutablePath { get; set; }

        /// <summary>
        /// File Path to update iterations.
        /// </summary>
        public string IterationsFilePath { get; set; }

        /// <summary>
        /// Iterations for which simulation need to be run.
        /// </summary>
        public string Iterations
        {
            get
            {
                this.Parameters.TryGetValue(nameof(OpenFOAMExecutor.Iterations), out IConvertible iterations);
                return iterations?.ToString();
            }
        }

        /// <summary>
        /// Specific Simulation Folder
        /// </summary>
        public string SimulationFolder
        {
            get
            {
                this.Parameters.TryGetValue(nameof(OpenFOAMExecutor.Scenario), out IConvertible simulationFolder);
                return simulationFolder?.ToString();
            }
        }

        /// <summary>
        /// Solver corresponding to the simulation.
        /// </summary>
        public string Solver
        {
            get
            {
                this.Parameters.TryGetValue(nameof(OpenFOAMExecutor.Solver), out IConvertible solver);
                return solver?.ToString();
            }
        }

        /// <summary>
        /// Results file name after executing OpenFOAM workload.
        /// </summary>
        public string ResultsFileName { get; set; }

        /// <summary>
        /// Path to the results file after executing OpenFOAM workload.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        public ISystemManagement SystemManager
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        /// Executes OpenFOAM Workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.CpuArchitecture == Architecture.Arm64 && this.SimulationFolder.Equals("motorBike"))
            {
                this.Logger.LogMessage($"{nameof(OpenFOAMExecutor)}.MotorBikeTemporarilyDisabled", telemetryContext);
            }
            else
            {
                DateTime startTime = DateTime.UtcNow, endTime = DateTime.UtcNow;

                foreach (var command in this.executables)
                {
                    await this.SystemManager.MakeFileExecutableAsync(command, this.Platform, cancellationToken)
                        .ConfigureAwait(false);
                }

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    foreach (var command in this.executionCommands)
                    {
                        startTime = DateTime.UtcNow;
                        await this.ExecuteWorkloadAsync("sudo", command, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);

                        endTime = DateTime.UtcNow;
                    }

                    await this.CaptureWorkloadResultsAsync(this.ResultsFilePath, startTime, endTime, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the OpenFOAM workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ThrowIfPlatformOrArchitectureIsNotSupported();

            await this.InitializeExecutorPropertiesAsync(cancellationToken, telemetryContext)
                .ConfigureAwait(false);

            await this.InitializeSimulationIterationsAsync(this.IterationsFilePath, cancellationToken)
                .ConfigureAwait(false);

            this.ThrowIfExeNotPresent();
        }

        private async Task InitializeExecutorPropertiesAsync(CancellationToken cancellationToken, EventContext telemetryContext)
        {
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();

            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackagePath = workloadPackage.Path;

            if (this.CpuArchitecture == Architecture.Arm64)
            {
                this.Logger.LogMessage($"{nameof(OpenFOAMExecutor)}.CopyingFilesForArm64", telemetryContext);
                this.CopySimulationFilesFromDownloadedPackage();
            }
                
            this.AllRunWrapperExecutablePath = this.PlatformSpecifics.Combine(this.PackagePath, "tools", "AllrunWrapper");
            this.AllRunExecutablePath = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "Allrun");
            string allRunExecutionCommand = $"{this.AllRunWrapperExecutablePath} {this.AllRunExecutablePath}";
            this.AllCleanExecutablePath = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "Allclean");
            this.IterationsFilePath = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "system", "controlDict");

            this.ResultsFileName = "log." + this.Solver;
            this.ResultsFilePath = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, this.ResultsFileName);

            this.executionCommands = new List<string>
            {
                this.AllCleanExecutablePath,
                allRunExecutionCommand
            };

            this.executables = new List<string>
            {
                this.AllCleanExecutablePath,
                this.AllRunExecutablePath,
                this.AllRunWrapperExecutablePath
            };
        }

        private void CopySimulationFilesFromDownloadedPackage()
        {
            // Move 0, constant and system folders from package to its respective simulation folder
            string simulationBaseDirectory = this.PlatformSpecifics.Combine("/usr", "share", "doc", "openfoam-examples", "examples");

            Dictionary<string, string> simulationPaths = new Dictionary<string, string>()
            {
                { "airFoil2D", this.PlatformSpecifics.Combine("incompressible", "simpleFoam") },
                { "elbow", this.PlatformSpecifics.Combine("incompressible", "icoFoam") },
                { "pitzDaily", this.PlatformSpecifics.Combine("incompressible", "simpleFoam") },
                { "lockExchange", this.PlatformSpecifics.Combine("multiphase", "twoLiquidMixingFoam") },
                { "motorBike", this.PlatformSpecifics.Combine("incompressible", "simpleFoam") }
            };

            string simulationSourceDir0;
            string simulationSourceDirConstant = this.PlatformSpecifics.Combine(simulationBaseDirectory, simulationPaths[this.SimulationFolder], this.SimulationFolder, "constant");
            string simulationSourceDirSystem = this.PlatformSpecifics.Combine(simulationBaseDirectory, simulationPaths[this.SimulationFolder], this.SimulationFolder, "system");
            
            if (this.SimulationFolder.Equals("lockExchange") || this.SimulationFolder.Equals("motorBike"))
            {
                simulationSourceDir0 = this.PlatformSpecifics.Combine(simulationBaseDirectory, simulationPaths[this.SimulationFolder], this.SimulationFolder, "0.orig");
            }
            else
            {
                simulationSourceDir0 = this.PlatformSpecifics.Combine(simulationBaseDirectory, simulationPaths[this.SimulationFolder], this.SimulationFolder, "0");
            }

            string simulationDestDir0 = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "0");
            string simulationDestDirConstant = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "constant");
            string simulationDestDirSystem = this.PlatformSpecifics.Combine(this.PackagePath, this.SimulationFolder, "system");

            this.fileSystem.Directory.CreateDirectory(simulationDestDir0);
            this.fileSystem.Directory.CreateDirectory(simulationDestDirConstant);
            this.fileSystem.Directory.CreateDirectory(simulationDestDirSystem);

            this.CopyAllFiles(simulationSourceDir0, simulationDestDir0);
            this.CopyAllFiles(simulationSourceDirConstant, simulationDestDirConstant);
            this.CopyAllFiles(simulationSourceDirSystem, simulationDestDirSystem);

        }

        private void CopyAllFiles(string source, string target)
        {
            foreach (string dir in this.fileSystem.Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                this.fileSystem.Directory.CreateDirectory(this.PlatformSpecifics.Combine(target, dir.Substring(source.Length + 1)));
            }

            foreach (string fileName in this.fileSystem.Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                this.fileSystem.File.Copy(fileName, this.PlatformSpecifics.Combine(target, fileName.Substring(source.Length + 1)), true);
            }
        }

        private void ThrowIfPlatformOrArchitectureIsNotSupported()
        {
            if (this.Platform != PlatformID.Unix)
            {
                throw new WorkloadException($"The OpenFOAM workload only supported on the following platform/architectures: {PlatformID.Unix}{Architecture.X64} and {PlatformID.Unix}{Architecture.Arm64}.", ErrorReason.PlatformNotSupported);
            }

            if ((this.CpuArchitecture != Architecture.X64) && (this.CpuArchitecture != Architecture.Arm64))
            {
                throw new WorkloadException($"The OpenFOAM workload only supported on the following platform/architectures: {PlatformID.Unix}{Architecture.X64} and {PlatformID.Unix}{Architecture.Arm64}.", ErrorReason.ProcessorArchitectureNotSupported);
            }
        }

        private void ThrowIfExeNotPresent()
        {
            foreach (var exe in this.executables)
            {
                if (!this.fileSystem.File.Exists(exe))
                {
                    throw new DependencyException(
                        $"OpenFOAM executable not found at path '{exe}'.",
                        ErrorReason.WorkloadDependencyMissing);
                }
            }
        }

        private async Task InitializeSimulationIterationsAsync(string iterationsFilePath, CancellationToken cancellationToken)
        {
            if (this.Iterations != null)
            {
                await this.fileSystem.File.ReplaceInFileAsync(
                    iterationsFilePath, @"endTime(\s)+[0-9]+", $"endTime      {this.Iterations}", cancellationToken);
            }
        }

        private async Task ExecuteWorkloadAsync(string command, string arguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.SystemManager.ProcessManager.CreateProcess(command, arguments))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());

                await process.StartAndWaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<OpenFOAMExecutor>(process, telemetryContext);
                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                }
            }
        }

        private async Task CaptureWorkloadResultsAsync(string resultsFilePath, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!this.fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The OpenFOAM results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string resultsContent = await this.fileSystem.File.ReadAllTextAsync(resultsFilePath, cancellationToken)
                    .ConfigureAwait(false);

                OpenFOAMMetricsParser openFOAMResultsParser = new OpenFOAMMetricsParser(resultsContent);
                IList<Metric> metrics = openFOAMResultsParser.Parse();
                this.Logger.LogMetrics(
                        "OpenFOAM",
                        this.SimulationFolder,
                        startTime,
                        endTime,
                        metrics,
                        null,
                        this.Parameters.ToString(),
                        this.Tags,
                        telemetryContext);
            }
        }
    }
}
