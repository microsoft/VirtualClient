// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Generic Script executor for Python
    /// </summary>
    public class PowershellExecutor : ScriptExecutor
    {
        /// <summary>
        /// Constructor for <see cref="PowershellExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public PowershellExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "powershell",
                    $"-NoProfile -Command \"cd '{this.WorkloadPackage.Path}';{this.ExecutablePath} {this.CommandLine}\"",
                    this.WorkloadPackage.Path,
                    telemetryContext,
                    cancellationToken,
                    false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName, logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        if (!string.IsNullOrWhiteSpace(process.StandardError.ToString()))
                        {
                            this.Logger.LogWarning($"StandardError: {process.StandardError}", telemetryContext);
                        }

                        await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                        await this.CaptureLogsAsync(cancellationToken);
                    }
                }
            }
        }
    }
}