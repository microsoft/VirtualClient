// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
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

    /// <summary>
    /// Installation component for IIS
    /// </summary>
    public class IISInstallation : VirtualClientComponent
    {
        private const string InstallIISCommand = "Install-WindowsFeature -name Web-Server,Net-Framework-45-Core,Web-Asp-Net45,NET-Framework-45-ASPNET -IncludeManagementTools";
        private const string DisableCompressioncommand = "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic";
        private const string DisableLoggingCommand = "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging";
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="IISInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public IISInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Installs IIS and disable compression and logging 
        /// </summary>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            State installationState = await this.stateManager.GetStateAsync<State>(nameof(IISInstallation), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
            {
                // Installing IIS
                this.Logger.LogMessage($"{nameof(IISInstallation)}.InstallIIS", LogLevel.Information, telemetryContext.Clone()
                    .AddContext("command", $"powershell {InstallIISCommand}"));

                await this.ExecutePowerShellCommandAsync(InstallIISCommand, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

                // Disable Compression and Logging
                this.Logger.LogMessage($"{nameof(IISInstallation)}.DisableIISCompression", LogLevel.Information, telemetryContext.Clone()
                    .AddContext("command", $"powershell {DisableCompressioncommand}"));

                await this.ExecutePowerShellCommandAsync(DisableCompressioncommand, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                this.Logger.LogMessage($"{nameof(IISInstallation)}.DisableIISLogging", LogLevel.Information, telemetryContext.Clone()
                    .AddContext("command", $"powershell {DisableLoggingCommand}"));

                await this.ExecutePowerShellCommandAsync(DisableLoggingCommand, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                await this.stateManager.SaveStateAsync(nameof(IISInstallation), new State(), cancellationToken)
                    .ConfigureAwait(false);
            }

        }

        private async Task ExecutePowerShellCommandAsync(string command, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, "powershell", command))
            {
                await process.StartAndWaitAsync(cancellationToken)
                     .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext)
                        .ConfigureAwait(false);

                    process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}
