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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The StressAppTest workload executor.
    /// </summary>
    public class StressAppTestExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="StressAppTestExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public StressAppTestExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(StressAppTestExecutor.CommandLine));
            }
        }

        /// <summary>
        /// The TimeInSeconds argument defined in the profile.
        /// </summary>
        public int TimeInSeconds
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(StressAppTestExecutor.TimeInSeconds));
            }
        }

        /// <summary>
        /// The UseCpuStressfulMemoryCopy argument defined in the profile, Switch to toggle StressAppTest built-in option to use more CPU-Stressful memory copy
        /// </summary>
        public bool UseCpuStressfulMemoryCopy
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(StressAppTestExecutor.UseCpuStressfulMemoryCopy));
            }
        }

        /// <summary>
        /// The path to the StressAppTest package.
        /// </summary>
        private string PackageDirectory { get; set; }

        /// <summary>
        /// The path to the StressAppTest executable file.
        /// </summary>
        private string ExecutableName { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the StressAppTest workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(
                this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackageDirectory = workloadPackage.Path;

            switch (this.Platform)
            {
                case PlatformID.Unix:
                    this.ExecutableName = this.PlatformSpecifics.Combine(this.PackageDirectory, "stressapptest");
                    break;

                default:
                    throw new WorkloadException(
                        $"The StressAppTest workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }

            await this.systemManagement.MakeFileExecutableAsync(this.ExecutableName, this.Platform, cancellationToken)
                .ConfigureAwait(false);

            if (!this.fileSystem.File.Exists(this.ExecutableName))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.ExecutableName}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Executes the StressAppTest workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
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

            if (this.TimeInSeconds <= 0)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.TimeInSeconds)}' arguments defined. {nameof(this.TimeInSeconds)} should be an integer greater than 0",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.CommandLine.Contains("-l"))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.CommandLine)}' arguments defined. {nameof(this.CommandLine)} should not contain a custom log file, with " +
                    $"-l parameter. That is being appended programatically",
                    ErrorReason.InvalidProfileDefinition);
            }
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
                this.Logger.LogNotSupported("StressAppTest", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Executes the StressAppTest workload command and generates the results file for the logs
        /// </summary>
        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    string commandLineArguments = this.CommandLine;
                    commandLineArguments += " -s " + this.TimeInSeconds;
                    if (this.UseCpuStressfulMemoryCopy && !commandLineArguments.Contains("-W"))
                    {
                        commandLineArguments += " -W";
                    }

                    // Example command with arguments: ./stressapptest -s 60 -l stressapptestLogs_202301131037407031.txt
                    string resultsFileName = $"stressapptestLogs_{DateTime.UtcNow.ToString("yyyyMMddHHmmssffff")}.txt";
                    commandLineArguments += " -l " + resultsFileName;

                    using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutableName, commandLineArguments.Trim(), this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (process.IsErrored())
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "StressAppTest");
                                process.ThrowIfWorkloadFailed();
                            }

                            await this.CaptureMetricsAsync(process, commandLineArguments, resultsFileName, telemetryContext, cancellationToken);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Logs the StressAppTest workload metrics.
        /// </summary>
        private async Task CaptureMetricsAsync(IProcessProxy process, string commandLineArguments, string resultsFileName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "StressAppTest",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string resultsPath = this.PlatformSpecifics.Combine(this.PackageDirectory, resultsFileName);
                string results = await this.LoadResultsAsync(resultsPath, cancellationToken);

                await this.LogProcessDetailsAsync(process, telemetryContext, "StressAppTest", results.AsArray(), logToFile: true);

                if (string.IsNullOrWhiteSpace(results))
                {
                    throw new WorkloadResultsException($"Invalid results. The StressAppTest workload did not produce valid results.", ErrorReason.InvalidResults);
                }

                StressAppTestMetricsParser parser = new StressAppTestMetricsParser(results);
                IList<Metric> workloadMetrics = parser.Parse();

                foreach (Metric metric in workloadMetrics)
                {
                    telemetryContext.AddContext("testRunResult", metric.Tags[0] ?? string.Empty);

                    this.Logger.LogMetrics(
                        toolName: "StressAppTest",
                        scenarioName: this.Scenario,
                        process.StartTime,
                        process.ExitTime,
                        metric.Name,
                        metric.Value,
                        metric.Unit,
                        metricCategorization: "StressAppTest",
                        scenarioArguments: commandLineArguments,
                        metric.Tags,
                        telemetryContext);
                }
            }
        }
    }
}