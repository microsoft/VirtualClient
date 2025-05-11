// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
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
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The StressNg workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
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
        /// The Cool down period for Virtual Client Component.
        /// </summary>
        public TimeSpan CoolDownPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.CoolDownPeriod), TimeSpan.FromSeconds(0));
            }
        }
        
        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(StressNgExecutor.CommandLine));
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

            // TO DO: Remove once we have Loop Executor.
            await this.WaitAsync(this.CoolDownPeriod, cancellationToken);
        }

        /// <summary>
        /// Initializes the environment for execution of the StressNg workload.
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

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. The action in the profile does not contain the " +
                    $"required '{nameof(this.Scenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.CommandLine.Contains("--yaml") || this.CommandLine.Contains("-Y "))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.CommandLine)}' arguments defined. {nameof(this.CommandLine)} should not contain a custom log file, with " +
                    $"--yaml or -Y parameter. That is being appended programatically",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "StressNg",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

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
            string commandLineArgs = string.Empty;

            if (!this.CommandLine.Contains("--cpu ") && !this.CommandLine.Contains("-c "))
            {
                commandLineArgs += $" --cpu {Environment.ProcessorCount}";
            }

            if (!this.CommandLine.Contains("--timeout") && !this.CommandLine.Contains("-t "))
            {
                commandLineArgs += $" --timeout {DefaultRuntimeInSeconds}";
            }

            if (!this.CommandLine.Contains("--metrics"))
            {
                commandLineArgs += $" --metrics";
            }

            commandLineArgs += $" --yaml {this.stressNgOutputFilePath}";
            commandLineArgs += $" {this.CommandLine}";
            // Example: stress-ng --cpu 16 --timeout 60 --metrics --yaml vcStressNg.yaml
            return commandLineArgs.Trim();
        }
    }
}