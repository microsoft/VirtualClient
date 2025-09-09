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
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Executes the FLOPS workload.
    /// </summary>
    [SupportedPlatforms("win-x64,win-arm64")]
    public class DXFLOPSExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// ConstructorD
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DXFLOPSExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Number of iterations to run the shader
        /// </summary>
        protected int ShaderIterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ShaderIterations), 1000000);

            }
        }

        /// <summary>
        /// Number of threads per block
        /// </summary>
        protected int ThreadsPerBlock
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadsPerBlock), 256);

            }
        }

        /// <summary>
        /// Number of data elements
        /// </summary>
        protected int NumDataElements
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.NumDataElements), 1048576);

            }
        }

        /// <summary>
        /// Number of data elements
        /// </summary>
        protected string Precision
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Precision), "f32").ToLower();
            }
        }

        /// <summary>
        /// Defines the path to the package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Path to the workload executable
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Runs the workload
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, "Benchmarks", "GPUCore.exe");
        }

        private IList<Metric> CaptureResults(IProcessProxy workloadProcess, string output, EventContext telemetryContext)
        {
            if (workloadProcess.ExitCode == 0)
            {
                DXFLOPSParser resultsParser = new DXFLOPSParser(output);
                return resultsParser.Parse();
            }
            else
            {
                return new List<Metric>();
            }

        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IList<Metric> metrics = new List<Metric>();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    DateTime startTime = DateTime.UtcNow;

                    string cmdargs = $"--num_loops 1 --num_loops_in_shader {this.ShaderIterations} --thread_num_per_block {this.ThreadsPerBlock} --num_data_elements {this.NumDataElements} {(this.Precision == "f16" ? "--f16 1" : "--f32 1")}";
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, cmdargs))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill(this.Logger));
                        process.RedirectStandardOutput = true;
                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext);
                                process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                                string output = process.StandardOutput.ToString();
                                foreach (Metric metric in this.CaptureResults(process, output, telemetryContext))
                                {
                                    metrics.Add(metric);
                                }
                            }
                        }
                        finally
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                    }

                    DateTime endTime = DateTime.UtcNow;

                    this.MetadataContract.AddForScenario(
                        "DXFLOPS",
                        $"{this.ExecutablePath} {cmdargs}",
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        "DXFLOPS",
                        "GPU FLOPS",
                        startTime,
                        endTime,
                        metrics,
                        null,
                        string.Empty,
                        this.Tags,
                        telemetryContext);
                }
            });
        }

        /// <summary>
        /// Validates the package installation
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="DependencyException"></exception>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                if (workloadPackage == null)
                {
                    throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                }

                workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

                this.Package = workloadPackage;
            }
        }

    }
}