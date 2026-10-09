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
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Contracts.Metadata;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The MemoryLatencyChecker workload executor.
    /// </summary>
    public class MemoryLatencyCheckerExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

        private string packageDirectory;
        private string executableName;

        /// <summary>
        /// Constructor for <see cref="MemoryLatencyCheckerExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MemoryLatencyCheckerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string Benchmark
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MemoryLatencyCheckerExecutor.Benchmark));
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the MemoryLatencyChecker workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(this.Benchmark))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. The action in the profile does not contain the " +
                    $"required '{nameof(this.Benchmark)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            if (workloadPackage == null)
            {
                throw new WorkloadException(
                    $"Failed to retrieve the package for {this.PackageName} on {this.Platform}/{this.CpuArchitecture}.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.packageDirectory = workloadPackage.Path;

            this.InitializeExecutableNames();

            await this.systemManagement.MakeFileExecutableAsync(this.executableName, this.Platform, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the MemoryLatencyChecker workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandLineArguments = $"--{this.Benchmark}";
            string consoleOutput;
            DateTime startTime = DateTime.UtcNow;

            consoleOutput = await this.ExecuteCommandAsync(this.executableName, commandLineArguments, this.packageDirectory, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;
            this.CaptureMetrics(startTime, endTime, consoleOutput, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes the MemoryLatencyChecker workload command and captures standardOutput of process.
        /// </summary>
        private async Task<string> ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            string consoleOutput = string.Empty;
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(MemoryLatencyCheckerExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                        this.Platform,
                        pathToExe,
                        commandLineArguments,
                        workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        consoleOutput = process.StandardOutput.ToString();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "MLC", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }).ConfigureAwait(false);
            }

            return consoleOutput;
        }

        /// <summary>
        /// Logs the MemoryLatencyChecker workload metrics.
        /// </summary>
        private void CaptureMetrics(DateTime startTime, DateTime endTime, string consoleOutput, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "MLC",
                    this.Benchmark,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                if (string.IsNullOrWhiteSpace(consoleOutput))
                {
                    throw new WorkloadResultsException("The MLC workload did not produce valid results.");
                }

                MetricsParser parser = new MemoryLatencyCheckerMetricsParser(consoleOutput, this.Benchmark);

                this.Logger.LogMetrics(
                    toolName: "MLC",
                    scenarioName: this.Scenario,
                    startTime,
                    endTime,
                    parser.Parse(),
                    metricCategorization: "MemoryLatencyChecker",
                    scenarioArguments: this.Benchmark,
                    this.Tags,
                    telemetryContext);
            }
        }

        private void InitializeExecutableNames()
        {
            switch (this.Platform)
            {
                case PlatformID.Win32NT:

                    switch (this.CpuArchitecture)
                    {
                        case Architecture.Arm64:
                            this.executableName = this.PlatformSpecifics.Combine(this.packageDirectory, "memLat.exe");
                            break;

                        case Architecture.X64:
                            this.executableName = this.PlatformSpecifics.Combine(this.packageDirectory, "mlc.exe");
                            break;
                    }

                    break;

                case PlatformID.Unix:

                    switch (this.CpuArchitecture)
                    {
                        case Architecture.X64:
                            this.executableName = this.PlatformSpecifics.Combine(this.packageDirectory, "mlc");
                            break;

                        default:
                            throw new WorkloadException(
                            $"The MemoryLatencyChecker workload is not supported on the current platform/architecture " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                            ErrorReason.PlatformNotSupported);
                    }

                    break;

                default:
                    throw new WorkloadException(
                        $"The MemoryLatencyChecker workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }
        }
    }
}