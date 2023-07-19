// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The StressNg workload executor.
    /// </summary>
    public class StressNgExecutor : VirtualClientComponent
    {
        private const int DefaultRuntimeInSeconds = 60;
        private const string StressNg = "stress-ng";
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// The path to the StressNg package.
        /// </summary>
        private string stressNgDirectory;

        /// <summary>
        /// The path to stressNg output file
        /// </summary>
        private string stressNgOutputFilePath;

        /// <summary>
        /// Constructor for <see cref="StressNgExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public StressNgExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The name of the package where the SPECJbb package is downloaded.
        /// </summary>
        public int DurationInSecond
        {
            get
            {
                 return this.Parameters.GetValue<int>(nameof(StressNgExecutor.DurationInSecond), DefaultRuntimeInSeconds);
            }
        }

        /// <summary>
        /// Executes the StressNg workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                using (IProcessProxy process = await this.ExecuteCommandAsync(StressNgExecutor.StressNg, commandLineArguments, this.stressNgDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Stress-ng", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Hpcg workload.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.stressNgDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "stressNg");
            if (!this.fileSystem.Directory.Exists(this.stressNgDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.stressNgDirectory);
            }

            this.stressNgOutputFilePath = this.PlatformSpecifics.Combine(this.stressNgDirectory, "vcStressNg.yaml");

            return Task.CompletedTask;
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string results = await this.LoadResultsAsync(this.stressNgOutputFilePath, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Stress-ng", results: results.AsArray(), logToFile: true);

                    StressNgMetricsParser parser = new StressNgMetricsParser(results);

                    this.Logger.LogMetrics(
                        toolName: "StressNg",
                        scenarioName: "StressNg",
                        process.StartTime,
                        process.ExitTime,
                        parser.Parse(),
                        metricCategorization: "StressNg",
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(this.stressNgOutputFilePath);
                }
                catch (Exception exc)
                {
                    throw new WorkloadException($"Failed to parse file at '{this.stressNgOutputFilePath}'.", exc, ErrorReason.InvalidResults);
                }
            }
        }

        private string GetCommandLineArguments()
        {
            int coreCount = Environment.ProcessorCount;

            // stress-ng --cpu 16 -vm 2 --timeout 60 --metrics --yaml vcStressNg.yaml
            return @$"--cpu {coreCount} --timeout {this.DurationInSecond} --metrics --yaml {this.stressNgOutputFilePath}";
        }
    }
}