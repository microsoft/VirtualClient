// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The Generic Script executor for Powershell
    /// </summary>
    public class PowerShellExecutor : ScriptExecutor
    {
        private const string PowerShellExecutableName = "powershell";
        private const string PowerShell7ExecutableName = "pwsh";

        /// <summary>
        /// Constructor for <see cref="PowerShellExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public PowerShellExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.ApplyBackwardsCompatibility();
        }

        /// <summary>
        /// The name (or path) of the PowerShell executable to use (e.g. pwsh, PowerShell.exe)
        /// </summary>
        public string Executable
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Executable), PowerShellExecutor.PowerShellExecutableName);
            }
        }

        /// <summary>
        /// The parameter specifies whether to use pwsh, by default it is false
        /// </summary>
        private bool UsePwsh
        {
            get
            {
                // TODO:
                // Remove this property entirely.
                //
                // This is an indirect way to get to the name of the PowerShell executable (pwsh vs. PowerShell.exe).
                // There is no reason to do so indirectly. There are additional benefits to allowing the user to simply
                // specify the executable name:
                // 1) The usage is just as easy and the outcome is the same.
                // 2) A full path to a specific version of PowerShell can be supplied (i.e. flexibility for other use cases).
                throw new NotSupportedException("Design Correction. The PowerShell executable name or path should be specified explicitly (e.g. pwsh, PowerShell.exe).");
            }
        }

        /// <summary>
        /// Executes the PowerShell script.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = this.Executable;

                // Handle nested quotation mark parsing pitfalls.
                string commandLine = this.CommandLine.Replace("\"", "\\\"");
                string executablePath = this.ExecutablePath.Replace("\"", "\\\"");
                string commandArguments = $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -WindowStyle Hidden -Command \"cd '{this.ExecutableDirectory}';{executablePath} {commandLine}\"";

                telemetryContext
                    .AddContext(nameof(command), command)
                    .AddContext(nameof(commandArguments), SensitiveData.ObscureSecrets(commandArguments));

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.ExecutableDirectory, telemetryContext, cancellationToken, this.RunElevated))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName);
                            process.ThrowIfWorkloadFailed();
                        }
                        finally
                        {
                            await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                            await this.CaptureLogsAsync(cancellationToken);
                        }
                    }
                }
            }
        }

        private void ApplyBackwardsCompatibility()
        {
            if (this.Parameters.TryGetValue(nameof(this.UsePwsh), out IConvertible usePwsh))
            {
                bool usePowerShell7 = usePwsh.ToBoolean(CultureInfo.InvariantCulture);
                this.Parameters[nameof(this.Executable)] = usePowerShell7
                    ? PowerShellExecutor.PowerShell7ExecutableName
                    : PowerShellExecutor.PowerShellExecutableName;
            }
        }
    }
}