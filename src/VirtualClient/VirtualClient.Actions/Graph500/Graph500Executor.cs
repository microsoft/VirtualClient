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
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Graph500 Workload executor.
    /// </summary>
    [UnixCompatible]
    public class Graph500Executor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IPackageManager packageManager;

        /// <summary>
        /// Constructor for <see cref="Graph500Executor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Graph500Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// Scale is logarithm base two of the number of vertices to be present in graph that we want to construct.
        /// </summary>
        public string Scale
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Graph500Executor.Scale), out IConvertible scale);
                return scale?.ToString();
            }
        }

        /// <summary>
        /// The ratio of the graph’s edge count to its vertex count (i.e., half the average degree of a vertex in the graph).
        /// </summary>
        public string EdgeFactor
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Graph500Executor.EdgeFactor), out IConvertible edgeFactor);
                return edgeFactor?.ToString();
            }
        }

        /// <summary>
        /// The path to the Graph500 executable file.
        /// </summary>
        public string ExecutableFilePath { get; set; }

        /// <summary>
        /// Path to the results file after executing Graph500 workload.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// The path to the Graph500 package.
        /// </summary>
        private string PackageDirectory { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the Graph500 workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ThrowIfPlatformIsNotSupported();

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackageDirectory = this.PlatformSpecifics.Combine(workloadPackage.Path, "src");
            this.ExecutableFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "graph500_reference_bfs_sssp");
            this.ResultsFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "results.txt");

            if (this.fileSystem.File.Exists(this.ResultsFilePath))
            {
                await this.fileSystem.File.DeleteAsync(this.ResultsFilePath);
            }
        }

        /// <summary>
        /// Executes the Graph500 workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                DateTime startTime = DateTime.UtcNow;

                await this.ExecuteCommandAsync("make", null, this.PackageDirectory, cancellationToken)
                        .ConfigureAwait(false);

                string executeScriptCommandArguments = this.Scale + " " + this.EdgeFactor;
                await this.ExecuteCommandAsync(this.ExecutableFilePath, executeScriptCommandArguments, this.PackageDirectory, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;
                this.ResultsFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "results.txt");
                await this.CaptureWorkloadResultsAsync(this.ResultsFilePath, startTime, endTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private void ThrowIfPlatformIsNotSupported()
        {
            if (this.Platform != PlatformID.Unix)
            {
                throw new WorkloadException(
                    $"The Graph500 workload is currently only supported on the following platform/architectures: " +
                    $"'{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}', '{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}'. ",
                    ErrorReason.PlatformNotSupported);
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

                await this.Logger.LogMessageAsync($"{nameof(Graph500Executor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(pathToExe, commandLineArguments, workingDirectory))
                    {
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        this.fileSystem.File.WriteAllText(this.ResultsFilePath, process.StandardOutput.ToString());

                        await this.WaitAsync(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<Graph500Executor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private async Task CaptureWorkloadResultsAsync(string resultsFilePath, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!this.fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The Graph500 results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string resultsContent = await this.WaitForResultsAsync(resultsFilePath, TimeSpan.FromMinutes(30), cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultsContent))
                {
                    try
                    {
                        Graph500MetricsParser graph500Parser = new Graph500MetricsParser(resultsContent);
                        IList<Metric> metrics = graph500Parser.Parse();
                        foreach (Metric result in metrics)
                        {
                            this.Logger.LogMetrics(
                                "Graph500",
                                "Graph500",
                                startTime,
                                endTime,
                                result.Name,
                                result.Value,
                                result.Unit,
                                null,
                                null,
                                this.Tags,
                                telemetryContext,
                                result.Relativity);
                        }
                    }
                    catch (SchemaException exc)
                    {
                        throw new WorkloadException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadFailed);
                    }
                }
                else
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        throw new WorkloadException(
                                $"Missing results. Workload results were not emitted by the workload.",
                                ErrorReason.WorkloadFailed);
                    }
                }
            }
        }

        private async Task<string> WaitForResultsAsync(string resultsFilePath, TimeSpan timeout, CancellationToken cancellationToken)
        {
            string results = null;
            DateTime waitTimeout = DateTime.UtcNow.Add(timeout);

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < waitTimeout)
            {
                results = await this.fileSystem.File.ReadAllTextAsync(resultsFilePath, cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(results))
                {
                    break;
                }

                await Task.Delay(500).ConfigureAwait(false);
            }

            return results;
        }
    }
}