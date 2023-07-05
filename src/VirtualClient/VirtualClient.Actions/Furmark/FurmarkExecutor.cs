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
    /// The Furmark workload executor.
    /// </summary>
    [WindowsCompatible]
    public class FurmarkExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IPackageManager packageManager;
       
        private string packageDirectory;

        /// <summary>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public FurmarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// path to exe.
        /// </summary>
        public string ExecutableLocation { get; set; }

        /// <summary>
        /// path to exe.
        /// </summary>
        public string ResultsFilePath { get;  set; }

        /// <summary>
        /// time parameter
        /// </summary>
        public string Time
        {
            get
            {
                this.Parameters.TryGetValue(nameof(FurmarkExecutor.Time), out IConvertible time);
                return time?.ToString();
            }
        }

        /// <summary>
        /// Path to the results file after executing FURMARK workload.
        /// </summary>
        /// <summary>
        /// Initializes the environment for execution of the FURMARK workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform != PlatformID.Win32NT)
            {
                throw new NotSupportedException($"'{this.Platform}' is not currently supported");
            }

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.packageDirectory = workloadPackage.Path;

            this.ExecutableLocation = this.PlatformSpecifics.Combine(this.packageDirectory, "Geeks3D", "Benchmarks", "FurMark", "Furmark");
            this.ResultsFilePath = this.PlatformSpecifics.Combine(this.packageDirectory, "FurMark-Scores.txt");
            /* await this.systemManagement.MakeFileExecutableAsync(this.PlatformSpecifics.Combine(this.packageDirectory, @"Geeks3D\Benchmarks\FurMark\Furmark"), this.Platform, cancellationToken)
                 .ConfigureAwait(false);

             this.ScriptFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "C:\\Program Files (x86)\\Geeks3D\\Benchmarks\\FurMark\\Furmark\\width=640 \\height=480 \\msaa=4 \\max_time=180000\\nogui \\nomenubar \\noscore \\run_mode=1 \\log_score \\disable_catalyst_warning");
             this.ResultsFilePath = this.PlatformSpecifics.Combine(this.packageDirectory, "TESTING", "C:\\Program Files (x86)\\Geeks3D\\Benchmarks\\FurMark>FurMark-Scores.txt");*/
        }

        /// <summary>
        /// Executes the FURMARK workload.
        /// </summary>
        // protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
                     string commandArguments = $"/width=640 /height=480 /msaa=4 /max_time={this.Time} /nogui /nomenubar /noscore /run_mode=1 /log_score /disable_catalyst_warning";

                     using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutableLocation, commandArguments, this.packageDirectory, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                if (process.StandardError.Length == 0) 
                                {
                                    Console.WriteLine($"The resultfilepath is = '{this.ResultsFilePath}' ");
                                    await this.LogProcessDetailsAsync(process, telemetryContext, "Furmark", logToFile: true);
                                    await this.CaptureMetricsAsync(process, this.ResultsFilePath, telemetryContext, cancellationToken);
                                }
                                else
                                {    
                                    await this.LogProcessDetailsAsync(process, telemetryContext, "Furmark", logToFile: true);
                                    process.ThrowIfWorkloadFailed();
                                }

                            }
                      }
 
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string resultsFilePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!this.fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The Furmark results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string results = await this.LoadResultsAsync(resultsFilePath, cancellationToken);
                Console.WriteLine($"The resultfilepath is = '{results}' ");

                await this.LogProcessDetailsAsync(process, telemetryContext, "Furmark", results.AsArray(), logToFile: true);

                FurmarkMetricsParser furmarkParser = new FurmarkMetricsParser(results);
                IList<Metric> metrics = furmarkParser.Parse();

                this.Logger.LogMetrics(
                    "Furmark",
                    "Furmark",
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