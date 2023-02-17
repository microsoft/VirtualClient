// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The CoreMark workload executor.
    /// </summary>
    [UnixCompatible]
    public class CoreMarkExecutor : VirtualClientComponent
    {
        private const string CoreMarkRunCommand = "make";
        private const string CoreMarkOutputFile1 = "run1.log";
        private const string CoreMarkOutputFile2 = "run2.log";

        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="CoreMarkExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CoreMarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Allos overwrite to Coremark process thread count. 
        /// </summary>
        public int ThreadCount
        {
            get
            {
                // Default to system core count, but overwritable with parameters.
                int threadCount = this.systemManagement.GetSystemCoreCount();

                if (this.Parameters.TryGetValue(nameof(this.ThreadCount), out IConvertible value) && value != null)
                {
                    threadCount = value.ToInt32(CultureInfo.InvariantCulture);
                }

                return threadCount;
            }
        }

        /// <summary>
        /// The path to CoreMark output file #1
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// The path to CoreMark output file #1
        /// </summary>
        protected string OutputFile1Path { get; set; }

        /// <summary>
        /// The path to CoreMark output file #2
        /// </summary>
        protected string OutputFile2Path { get; set; }

        /// <summary>
        /// Executes CoreMark
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();
                await this.ExecuteWorkloadAsync(CoreMarkExecutor.CoreMarkRunCommand, commandLineArguments, telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the workload environment.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.CheckPlatformSupport();

            this.PackagePath = this.GetPackagePath(this.PackageName);
            this.OutputFile1Path = this.Combine(this.PackagePath, CoreMarkExecutor.CoreMarkOutputFile1);
            this.OutputFile2Path = this.Combine(this.PackagePath, CoreMarkExecutor.CoreMarkOutputFile2);

            return Task.CompletedTask;
        }

        private async Task ExecuteWorkloadAsync(string pathToExe, string commandLineArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = await this.ExecuteCommandAsync(pathToExe, commandLineArguments, this.PackagePath, telemetryContext, cancellationToken, runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (process.IsErrored())
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "CoreMark", logToFile: true);
                        process.ThrowIfWorkloadFailed();
                    }

                    IEnumerable<string> results = await this.LoadResultsAsync(
                        new string[] { this.OutputFile1Path, this.OutputFile2Path },
                        cancellationToken);

                    await this.LogProcessDetailsAsync(process, telemetryContext, "CoreMark", results: results, logToFile: true);
                    await this.CaptureMetricsAsync(process, results, commandLineArguments, telemetryContext, cancellationToken);
                }
            }
        }

        private string GetCommandLineArguments()
        {
            return @$"XCFLAGS=""-DMULTITHREAD={this.ThreadCount} -DUSE_PTHREAD"" REBUILD=1 LFLAGS_END=-pthread";
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, IEnumerable<string> workloadResults, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    foreach (string results in workloadResults)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "CoreMark", results: results.AsArray(), logToFile: true);

                        if (!string.IsNullOrWhiteSpace(results))
                        {
                            CoreMarkMetricsParser parser = new CoreMarkMetricsParser(results);
                            IList<Metric> metrics = parser.Parse();

                            this.Logger.LogMetrics(
                                toolName: "CoreMark",
                                scenarioName: "CoreMark",
                                process.StartTime,
                                process.ExitTime,
                                metrics,
                                null,
                                commandArguments,
                                this.Tags,
                                telemetryContext);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, EventContext.Persisted());
            }
        }

        private void CheckPlatformSupport()
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    break;
                default:
                    throw new WorkloadException(
                        $"The CoreMark workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        $" Supported platform/architectures include: " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                        ErrorReason.PlatformNotSupported);
            }
        }
    }
}