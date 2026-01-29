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
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.ProcessAffinity;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Example workload executor demonstrating CPU core affinity binding.
    /// This is a reference implementation showing how to use the ProcessAffinityConfiguration
    /// infrastructure to bind workload processes to specific CPU cores.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExampleWorkloadWithAffinityExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadWithAffinityExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleWorkloadWithAffinityExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.processManager = this.systemManagement.ProcessManager;
        }

        /// <summary>
        /// Gets or sets whether to bind the workload to specific CPU cores.
        /// </summary>
        public bool BindToCores
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.BindToCores), defaultValue: false);
            }
        }

        /// <summary>
        /// Gets the command line arguments to pass to the workload executable.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// Gets the CPU core affinity specification (e.g., "0-3", "0,2,4,6").
        /// </summary>
        public string CoreAffinity
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.CoreAffinity), out IConvertible value);
                return value?.ToString();
            }
        }

        /// <summary>
        /// Gets the test name for metric categorization.
        /// </summary>
        public string TestName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TestName), defaultValue: "ExampleAffinityTest");
            }
        }

        /// <summary>
        /// The path to the workload executable.
        /// </summary>
        protected string WorkloadExecutablePath { get; set; }

        /// <summary>
        /// The workload package containing the executable.
        /// </summary>
        protected DependencyPath WorkloadPackage { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                this.Logger.LogTraceMessage($"{nameof(ExampleWorkloadWithAffinityExecutor)}.Starting", telemetryContext);

                DateTime startTime = DateTime.UtcNow;

                string workloadResults = await this.ExecuteWorkloadAsync(this.CommandLine, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                DateTime finishTime = DateTime.UtcNow;

                await this.CaptureMetricsAsync(workloadResults, this.CommandLine, startTime, finishTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.WorkloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            if (this.Platform == PlatformID.Win32NT)
            {
                this.WorkloadExecutablePath = this.Combine(this.WorkloadPackage.Path, "ExampleWorkload.exe");
            }
            else
            {
                this.WorkloadExecutablePath = this.Combine(this.WorkloadPackage.Path, "ExampleWorkload");
                await this.systemManagement.MakeFileExecutableAsync(this.WorkloadExecutablePath, this.Platform, cancellationToken);
            }

            if (!this.fileSystem.File.Exists(this.WorkloadExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package at '{this.WorkloadExecutablePath}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Validates the component parameters.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            this.ThrowIfParameterNotDefined(nameof(this.PackageName));
            this.ThrowIfParameterNotDefined(nameof(this.CommandLine));

            if (this.BindToCores)
            {
                this.ThrowIfParameterNotDefined(nameof(this.CoreAffinity));
            }
        }

        private Task CaptureMetricsAsync(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage($"{nameof(ExampleWorkloadWithAffinityExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                ExampleWorkloadMetricsParser resultsParser = new ExampleWorkloadMetricsParser(results);
                IList<Metric> workloadMetrics = resultsParser.Parse();

                foreach (var metric in workloadMetrics)
                {
                    metric.Metadata.Add("bindToCores", this.BindToCores.ToString());
                    if (this.BindToCores && !string.IsNullOrWhiteSpace(this.CoreAffinity))
                    {
                        metric.Metadata.Add("coreAffinity", this.CoreAffinity);
                    }
                }

                this.Logger.LogMetrics(
                    toolName: "ExampleWorkload",
                    scenarioName: this.Scenario,
                    scenarioStartTime: startTime,
                    scenarioEndTime: endTime,
                    metrics: workloadMetrics,
                    metricCategorization: null,
                    scenarioArguments: commandArguments,
                    this.Tags,
                    telemetryContext);
            }

            return Task.CompletedTask;
        }

        private Task<string> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext("packagePath", this.WorkloadPackage.Path)
                .AddContext("command", this.WorkloadExecutablePath)
                .AddContext("commandArguments", commandArguments)
                .AddContext("bindToCores", this.BindToCores)
                .AddContext("coreAffinity", this.CoreAffinity);

            return this.Logger.LogMessageAsync($"{nameof(ExampleWorkloadWithAffinityExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    IProcessProxy workloadProcess;
                    ProcessAffinityConfiguration affinityConfig = null;

                    // CPU Affinity Binding Example:
                    // If BindToCores is enabled, create the process with CPU affinity configuration.
                    // This demonstrates the two different approaches for Windows vs. Linux.
                    if (this.BindToCores && !string.IsNullOrWhiteSpace(this.CoreAffinity))
                    {
                        affinityConfig = ProcessAffinityConfiguration.Create(
                            this.Platform,
                            this.CoreAffinity);

                        relatedContext.AddContext("affinityMask", affinityConfig.ToString());

                        if (this.Platform == PlatformID.Win32NT)
                        {
                            // Windows: Create process normally - affinity will be applied after starting
                            workloadProcess = this.processManager.CreateProcess(
                                this.WorkloadExecutablePath,
                                commandArguments,
                                this.WorkloadPackage.Path);
                        }
                        else
                        {
                            // Linux: Wrap command with numactl for CPU binding
                            LinuxProcessAffinityConfiguration linuxConfig = (LinuxProcessAffinityConfiguration)affinityConfig;
                            string fullCommand = linuxConfig.GetCommandWithAffinity(
                                this.WorkloadExecutablePath,
                                commandArguments);

                            workloadProcess = this.processManager.CreateProcess(
                                "/bin/bash",
                                $"-c \"{fullCommand}\"",
                                this.WorkloadPackage.Path);
                        }
                    }
                    else
                    {
                        // No affinity binding - create process normally
                        workloadProcess = this.processManager.CreateProcess(
                            this.WorkloadExecutablePath,
                            commandArguments,
                            this.WorkloadPackage.Path);
                    }

                    using (workloadProcess)
                    {
                        this.CleanupTasks.Add(() => workloadProcess.SafeKill(this.Logger));

                        // Windows affinity must be applied after process starts but before it exits
                        if (affinityConfig != null && this.Platform == PlatformID.Win32NT)
                        {
                            // Start process first
                            workloadProcess.Start();
                            
                            // Apply affinity immediately while process is running
                            workloadProcess.ApplyAffinity((WindowsProcessAffinityConfiguration)affinityConfig);
                            
                            // Wait for completion
                            await workloadProcess.WaitForExitAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            // Linux (already wrapped with numactl) or no affinity: start and wait normally
                            await workloadProcess.StartAndWaitAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(workloadProcess, telemetryContext, "ExampleWorkload", logToFile: true)
                                .ConfigureAwait(false);

                            workloadProcess.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);

                            if (workloadProcess.StandardOutput.Length == 0)
                            {
                                throw new WorkloadException(
                                    $"Unexpected workload results outcome. The workload did not produce any results to standard output.",
                                    ErrorReason.WorkloadResultsNotFound);
                            }
                        }

                        return workloadProcess.StandardOutput.ToString();
                    }
                }
            });
        }
    }
}
