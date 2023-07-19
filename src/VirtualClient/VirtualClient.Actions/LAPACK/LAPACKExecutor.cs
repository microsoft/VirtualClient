// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The LAPACK workload executor.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class LAPACKExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IPackageManager packageManager;
        private string cygwinPackageDirectory;
        private string packageDirectory;

        /// <summary>
        /// Constructor for <see cref="LAPACKExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LAPACKExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The path to the LAPACK script file.
        /// </summary>
        public string ScriptFilePath { get; set; }

        /// <summary>
        /// Path to the results file after executing LAPACK workload.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the LAPACK workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform != PlatformID.Win32NT && this.Platform != PlatformID.Unix)
            {
                throw new NotSupportedException($"'{this.Platform}' is not currently supported");
            }

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.packageDirectory = workloadPackage.Path;

            if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath cygwinPackage = await this.packageManager.GetPackageAsync("cygwin", CancellationToken.None)
                    .ConfigureAwait(false);

                this.cygwinPackageDirectory = cygwinPackage.Path;
            }

            await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.packageDirectory, @"lapack_testing.py"), this.Platform, cancellationToken)
                .ConfigureAwait(false);

            this.ScriptFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "LapackTestScript.sh");
            this.ResultsFilePath = this.PlatformSpecifics.Combine(this.packageDirectory, "TESTING", "testing_results.txt");
        }

        /// <summary>
        /// Executes the LAPACK workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                if (this.Platform == PlatformID.Unix)
                {
                    // Run make to generate all object files for fortran subroutines.
                    await this.ExecuteCommandAsync("make", null, this.packageDirectory, cancellationToken)
                            .ConfigureAwait(false);

                    // Delete results file that gets generated.
                    if (this.fileSystem.File.Exists(this.ResultsFilePath))
                    {
                        await this.fileSystem.File.DeleteAsync(this.ResultsFilePath);
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync("bash", this.ScriptFilePath, this.packageDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (process.IsErrored())
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "LAPACK", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }

                            await this.CaptureMetricsAsync(process, this.ResultsFilePath, telemetryContext, cancellationToken);
                        }
                    }
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    await this.ExecuteCygwinBashAsync("./cmakescript.sh", this.packageDirectory, this.cygwinPackageDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    // Delete results file that gets generated.
                    if (this.fileSystem.File.Exists(this.ResultsFilePath))
                    {
                        await this.fileSystem.File.DeleteAsync(this.ResultsFilePath);
                    }

                    using (IProcessProxy process = await this.ExecuteCygwinBashAsync("./LapackTestScript.sh", this.packageDirectory, this.cygwinPackageDirectory, telemetryContext, cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (process.IsErrored())
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "LAPACK", logToFile: true);
                                process.ThrowIfWorkloadFailed();
                            }

                            await this.CaptureMetricsAsync(process, this.ResultsFilePath, telemetryContext, cancellationToken);
                        }
                    }
                }
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(LAPACKExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.LogProcessDetailsAsync(process, telemetryContext)
                                .ConfigureAwait();

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait();
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string resultsFilePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!this.fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The LAPACK results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string results = await this.LoadResultsAsync(resultsFilePath, cancellationToken);
                await this.LogProcessDetailsAsync(process, telemetryContext, "LAPACK", results.AsArray(), logToFile: true);

                LAPACKMetricsParser lapackParser = new LAPACKMetricsParser(results);
                IList<Metric> metrics = lapackParser.Parse();

                this.Logger.LogMetrics(
                    "LAPACK",
                    "LAPACK",
                    process.StartTime,
                    process.ExitTime,
                    metrics,
                    null,
                    null,
                    this.Tags,
                    telemetryContext);
            }
        }
    }
}