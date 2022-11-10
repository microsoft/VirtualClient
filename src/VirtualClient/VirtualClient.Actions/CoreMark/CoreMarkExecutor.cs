// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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

        /// <summary>
        /// Constructor for <see cref="CoreMarkExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CoreMarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
        }

        private string CoreMarkDirectory
        {
            get
            {
                return this.PlatformSpecifics.GetPackagePath(this.PackageName);
            }
        }

        /// <summary>
        /// Executes CoreMark
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandLineArguments = this.GetCommandLineArguments();

            this.CheckPlatformSupport();

            this.StartTime = DateTime.UtcNow;
            await this.StartCoreMarkProcessAsync(CoreMarkExecutor.CoreMarkRunCommand, commandLineArguments, telemetryContext, cancellationToken);
            this.EndTime = DateTime.UtcNow;

            this.LogCoreMarkOutput(
                this.Combine(this.CoreMarkDirectory, CoreMarkExecutor.CoreMarkOutputFile1),
                this.StartTime,
                this.EndTime,
                telemetryContext,
                cancellationToken);

            this.LogCoreMarkOutput(
                this.Combine(this.CoreMarkDirectory, CoreMarkExecutor.CoreMarkOutputFile2),
                this.StartTime,
                this.EndTime,
                telemetryContext,
                cancellationToken);
        }

        private async Task StartCoreMarkProcessAsync(string pathToExe, string commandLineArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, this.CoreMarkDirectory))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());

                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<CoreMarkExecutor>(process, telemetryContext);
                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                }
            }

            this.Logger.LogTraceMessage($"CoreMark process {pathToExe} {commandLineArguments}", telemetryContext);
        }

        private string GetCommandLineArguments()
        {
            return @$"XCFLAGS=""-DMULTITHREAD={Environment.ProcessorCount} -DUSE_PTHREAD"" REBUILD=1 LFLAGS_END=-pthread";
        }

        private void LogCoreMarkOutput(string filePath, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    string text = File.ReadAllText(filePath);
                    CoreMarkMetricsParser parser = new CoreMarkMetricsParser(text);
                    parser.Parse();

                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        toolName: "CoreMark",
                        scenarioName: "CoreMark",
                        startTime,
                        endTime,
                        metrics,
                        metricCategorization: string.Empty,
                        scenarioArguments: this.GetCommandLineArguments(),
                        this.Tags,
                        telemetryContext);
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