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
    public class PythonExecutor : ScriptExecutor
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="PythonExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public PythonExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The parameter specifies whether to use python3, by default it is true
        /// </summary>
        public bool UsePython3
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.UsePython3), true);
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string pythonVersion = "python";
            if (this.UsePython3)
            {
                pythonVersion = "python3";
            }

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    pythonVersion,
                    $"{this.ExecutablePath} {this.CommandLine}",
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