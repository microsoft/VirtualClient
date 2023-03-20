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
    /// The CoreMarkPro workload executor.
    /// </summary>
    [UnixCompatible]
    public class CoreMarkProExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Constructor for <see cref="CoreMarkProExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CoreMarkProExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
        }

        private string CoreMarkProDirectory
        {
            get
            {
                return this.PlatformSpecifics.GetPackagePath(this.PackageName);
            }
        }

        /// <summary>
        /// Executes CoreMarkPro
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.CheckPlatformSupport();

            string commandLineArguments = this.GetCommandLineArguments();

            this.StartTime = DateTime.UtcNow;
            string output = await this.ExecuteCommandAsync("make", commandLineArguments, telemetryContext, cancellationToken);
            this.EndTime = DateTime.UtcNow;

            CoreMarkProMetricsParser parser = new CoreMarkProMetricsParser(output);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                toolName: "CoreMarkPro",
                scenarioName: this.Scenario,
                this.StartTime,
                this.EndTime,
                metrics,
                metricCategorization: this.Scenario,
                scenarioArguments: this.Parameters.ToString(),
                this.Tags,
                telemetryContext);
        }

        /// <summary>
        /// Executes the given command.
        /// </summary>
        /// <returns>Output of the command.</returns>
        private Task<string> ExecuteCommandAsync(string command, string argument, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command);

            string output = string.Empty;

            return this.Logger.LogMessageAsync($"{nameof(CoreMarkProExecutor)}.ExecuteCommand", relatedContext, async () =>
            {
                ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
                using (IProcessProxy process = systemManagement.ProcessManager.CreateProcess(command, argument, this.CoreMarkProDirectory))
                {
                    SystemManagement.CleanupTasks.Add(() => process.SafeKill());

                    await process.StartAndWaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.Logger.LogProcessDetails<CoreMarkProExecutor>(process, telemetryContext);

                        process.ThrowIfErrored<WorkloadException>(
                            ProcessProxy.DefaultSuccessCodes,
                            errorReason: ErrorReason.WorkloadFailed);
                    }

                    output = process.StandardOutput.ToString();
                }

                return output;
            });
        }

        private string GetCommandLineArguments()
        {
            // guide: https://github.com/eembc/coremark-pro/blob/main/docs/EEMBC%20Symmetric%20Multicore%20Benchmark%20User%20Guide%202.1.4.pdf
            // make TARGET=linux64 XCMD='-c4' certify-all
            // Even when using cygwin, the TARGET is still linux64.
            int coreCount = this.Dependencies.GetService<ISystemManagement>().GetSystemCoreCount();

            return @$"TARGET=linux64 XCMD='-c{coreCount}' certify-all";
        }

        private void CheckPlatformSupport()
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    break;
                default:
                    throw new WorkloadException(
                        $"The CoreMarkPro workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        $" Supported platform/architectures include: " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                        ErrorReason.PlatformNotSupported);
            }
        }
    }
}