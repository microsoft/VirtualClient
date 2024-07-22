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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The LMbench workload virtual client action
    /// </summary>
    [UnixCompatible]
    public class LMbenchExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private string resultsDirectory;
        private string buildFilePath;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LMbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The path where the LMbench JSON results file should be output.
        /// </summary>
        public string LMbenchDirectory { get; set; }

        /// <summary>
        /// The identifier to use as the tested instance.
        /// </summary>
        public string TestedInstance
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LMbenchExecutor.TestedInstance), out IConvertible testedInstance);
                return testedInstance?.ToString();
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
        /// Libraries that should be linked with a program during the linking phase of compilation of lmbench.
        /// </summary>
        public string LDLIBS
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LMbenchExecutor.LDLIBS), out IConvertible ldlibs);
                return ldlibs?.ToString();
            }
        }

        /// <summary>
        /// Executes LMbench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                this.Cleanup();

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess("make", $"build {this.CompilerFlags}", this.LMbenchDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "LMbench", logToFile: true)
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                    }
                }

                await this.ExecuteWorkloadAsync("bash", "-c \"echo -e '\n\n\n\n\n\n\n\n\n\n\n\n\nnone' | make results\"", telemetryContext, cancellationToken).ConfigureAwait();

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess("make", "see", this.LMbenchDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "LMbench", logToFile: true)
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);

                        await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                    }
                }
            }
            finally
            {
                this.Cleanup();
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the LMbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();

            DependencyPath workloadPackage = await packageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            // On Linux systems, in order to allow the various GCC executables to be used in compilation (e.g. make, config),
            // they must be attributed as executable.
            await this.systemManagement.MakeFilesExecutableAsync(this.PlatformSpecifics.Combine(workloadPackage.Path, "scripts"), this.Platform, cancellationToken)
                .ConfigureAwait(false);

            this.LMbenchDirectory = workloadPackage.Path;
            this.resultsDirectory = this.PlatformSpecifics.Combine(this.LMbenchDirectory, "results");
            this.buildFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "scripts", "build");
            await this.ConfigureBuild(this.buildFilePath, cancellationToken);
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

        private async Task ConfigureBuild(string buildFilePath, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                FileSystemExtensions.ThrowIfFileDoesNotExist(this.fileSystem.File, buildFilePath);
                string fileContent = await this.fileSystem.File.ReadAllTextAsync(buildFilePath, cancellationToken)
                    .ConfigureAwait(false);

                Regex regexPattern = new Regex(@"LDLIBS=(.*)");

                fileContent = regexPattern.Replace(fileContent, $"LDLIBS=\"{this.LDLIBS}\"", 1);

                await this.fileSystem.File.WriteAllTextAsync(buildFilePath, fileContent, cancellationToken)
                    .ConfigureAwait(false);
            }   
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "LMbench",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string resultsPath = this.PlatformSpecifics.Combine(this.LMbenchDirectory, "results", "summary.out");

                string results = await this.LoadResultsAsync(resultsPath, cancellationToken);

                LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    toolName: "LMbench",
                    scenarioName: "LMbench",
                    process.StartTime,
                    process.ExitTime,
                    metrics,
                    metricCategorization: null,
                    scenarioArguments: "make results",
                    this.Tags,
                    telemetryContext);
            }
        }

        private void Cleanup()
        {
            // We cleanup any directories in the 'results' parent directory but leave the Makefile.
            string[] directories = this.fileSystem.Directory.GetDirectories(this.resultsDirectory, "*", SearchOption.TopDirectoryOnly);
            if (directories?.Any() == true)
            {
                foreach (string directory in directories)
                {
                    try
                    {
                        this.fileSystem.Directory.Delete(directory, true);
                    }
                    catch (Exception exc)
                    {
                        // Best Effort
                        this.Logger.LogErrorMessage(exc, EventContext.Persisted(), LogLevel.Warning);
                    }
                }
            }
        }

        private Task ExecuteWorkloadAsync(string pathToExe, string commandLineArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", pathToExe)
                .AddContext("commandArguments", commandLineArguments);

            return this.Logger.LogMessageAsync($"{nameof(LMbenchExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(pathToExe, commandLineArguments, this.LMbenchDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "LMbench", logToFile: true)
                                .ConfigureAwait(false);

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }
            });
        }
    }
}