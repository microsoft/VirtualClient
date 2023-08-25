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
        protected string ExecutableFilePath { get; set; }

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
        }

        /// <summary>
        /// Executes the Graph500 workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.ExecuteCommandAsync("make", null, this.PackageDirectory, cancellationToken);

                using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutableFilePath, this.Scale + " " + this.EdgeFactor, this.PackageDirectory, telemetryContext, cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Graph500", logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, telemetryContext, cancellationToken);
                    }
                }
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
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait();
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }).ConfigureAwait();
            }
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "Graph500",
                        process?.StartInfo?.Arguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    Graph500MetricsParser graph500Parser = new Graph500MetricsParser(process.StandardOutput.ToString());
                    IList<Metric> metrics = graph500Parser.Parse();

                    foreach (Metric result in metrics)
                    {
                        this.Logger.LogMetrics(
                            "Graph500",
                            "Graph500",
                            process.StartTime,
                            process.ExitTime,
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
                    throw new WorkloadException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }
    }
}