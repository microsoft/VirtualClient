// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Component executes the Virtual Client command line on a target system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class RemoteAgentLogCopy : VirtualClientControllerComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteSshCommand"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public RemoteAgentLogCopy(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes the command on the target system through an SSH connection.
        /// </summary>
        /// <param name="targetAgent">The target agent on which to execute.</param>
        /// <param name="telemetryContext">Provides context information to include in telemetry output.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(ISshClientProxy targetAgent, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(this.TypeName)}.ExecuteOnTarget", telemetryContext, async () =>
            {
                try
                {
                    await targetAgent.ConnectAsync(cancellationToken);
                    // this.LogRemoteOperation("Copy Logs");

                    Tuple<PlatformID, Architecture> platformArchitecture = await targetAgent.GetTargetPlatformArchitectureAsync(cancellationToken);
                    PlatformID targetPlatform = platformArchitecture.Item1;
                    Architecture targetArchitecture = platformArchitecture.Item2;
                    PlatformSpecifics targetPlatformSpecifics = new PlatformSpecifics(targetPlatform, targetArchitecture);

                    // The user CAN set an alternate packages path on the target system using the 'VC_LOGS_DIR' 
                    // environment variable.
                    string targetHost = targetAgent.ConnectionInfo.Host;
                    string targetLogsPath = VirtualClientControllerComponent.GetDefaultRemoteLogsPath(targetPlatformSpecifics);
                    string localLogsPath = this.PlatformSpecifics.GetLogsPath(targetHost.ToLowerInvariant(), this.ExperimentId.ToLowerInvariant());

                    telemetryContext.AddContext("copyLogsFrom", targetLogsPath);
                    telemetryContext.AddContext("copyLogsTo", localLogsPath);

                    if (await targetAgent.ExistsAsync(targetLogsPath))
                    {
                        await this.CopyDirectoryFromAsync(targetAgent, targetLogsPath, localLogsPath, telemetryContext, cancellationToken);
                        await targetAgent.DeleteDirectoryAsync(targetLogsPath);
                    }
                }
                finally
                {
                    targetAgent.Disconnect();
                }
            });
        }

        /// <summary>
        /// Validates the parameters and initial state of the component.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.ComponentType == ComponentType.Monitor)
            {
                throw new NotSupportedException($"Invalid component usage. The '{nameof(ExecuteSshCommand)}' component cannot be used as a monitor.");
            }
        }
    }
}
