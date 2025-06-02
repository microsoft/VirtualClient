// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The Generic Script executor for Powershell
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
        /// Executes the PowerShell script.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "powershell";
                string commandArguments = SensitiveData.ObscureSecrets(
                    $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"cd '{this.ExecutableDirectory}';{this.ExecutablePath} {this.CommandLine}\"");

                telemetryContext
                    .AddContext(nameof(command), command)
                    .AddContext(nameof(commandArguments), commandArguments);

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.ExecutableDirectory, telemetryContext, cancellationToken, this.RunElevated))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName);
                        process.ThrowIfWorkloadFailed();

                        await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                        await this.CaptureLogsAsync(cancellationToken);
                    }
                }
            }
        }
    }
}