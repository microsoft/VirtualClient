// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The LMbench workload virtual client action
    /// </summary>
    [UnixCompatible]
    public class LMbenchExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private string resultsDirectory;

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
        /// Executes LMbench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                this.Cleanup();
                await this.ExecuteWorkloadAsync("make", "results", telemetryContext, cancellationToken).ConfigureAwait();

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess("make", "see", this.LMbenchDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "LMbench", logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        if (process.StandardOutput.Length > 0)
                        {
                            this.CaptureMetrics(process, telemetryContext, cancellationToken);
                        }
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
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LMbenchMetricsParser parser = new LMbenchMetricsParser(process.StandardOutput.ToString());
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