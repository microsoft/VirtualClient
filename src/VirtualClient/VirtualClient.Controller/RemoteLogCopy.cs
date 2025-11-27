// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Component copies logs from a remote system to the local system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class RemoteLogCopy : VirtualClientControllerComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAgentExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public RemoteLogCopy(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// True/false whether the executor should copy logs from the target agent system
        /// back to the controller system. Default = true.
        /// </summary>
        public bool CopyLogs
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.CopyLogs), true);
            }
        }

        /// <summary>
        /// Copies logs from the target agent/system to the current system through an SSH connection.
        /// </summary>
        /// <param name="sshTarget">The target agent from which to copy the logs.</param>
        /// <param name="telemetryContext">Provides context information to include in telemetry output.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task CopyLogsAsync(ISshClientProxy sshTarget, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                await sshTarget.ConnectAsync(cancellationToken);

                Tuple<PlatformID, Architecture> platformArchitecture = await sshTarget.GetTargetPlatformArchitectureAsync(cancellationToken);
                PlatformID targetPlatform = platformArchitecture.Item1;
                Architecture targetArchitecture = platformArchitecture.Item2;
                PlatformSpecifics targetPlatformSpecifics = new PlatformSpecifics(targetPlatform, targetArchitecture);

                // The user CAN set an alternate packages path on the target system using the 'VC_LOGS_DIR' 
                // environment variable.
                string targetHost = sshTarget.ConnectionInfo.Host;
                string targetLogsPath = VirtualClientControllerComponent.GetDefaultRemoteLogsPath(targetPlatformSpecifics);
                string localLogsPath = this.PlatformSpecifics.GetLogsPath(targetHost.ToLowerInvariant(), this.ExperimentId.ToLowerInvariant());

                telemetryContext.AddContext("copyLogsFrom", targetLogsPath);
                telemetryContext.AddContext("copyLogsTo", localLogsPath);

                if (await sshTarget.ExistsAsync(targetLogsPath))
                {
                    sshTarget.StandardOutput?.WriteLine();
                    sshTarget.StandardOutput?.WriteLine($"[Copy Logs to Controller]");
                    await this.CopyDirectoryFromAsync(sshTarget, targetLogsPath, localLogsPath, telemetryContext, cancellationToken);
                    await sshTarget.DeleteDirectoryAsync(targetLogsPath);
                }
            }
            finally
            {
                sshTarget.Disconnect();
            }
        }

        /// <summary>
        /// Executes the command on the target system through an SSH connection.
        /// </summary>
        /// <param name="sshTarget">The target agent on which to execute.</param>
        /// <param name="telemetryContext">Provides context information to include in telemetry output.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(ISshClientProxy sshTarget, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.CopyLogs)
            {
                try
                {
                    await sshTarget.ConnectAsync(cancellationToken);
                    await this.CopyLogsAsync(sshTarget, telemetryContext, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a cancellation is requested.
                }
                catch (Exception exc)
                {
                    sshTarget.StandardError?.WriteLine(exc.Message);
                    throw;
                }
                finally
                {
                    sshTarget.Disconnect();
                }
            }
        }

        /// <summary>
        /// Validates the parameters and initial state of the component.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.ComponentType == ComponentType.Monitor)
            {
                throw new NotSupportedException($"Invalid component usage. The '{nameof(RemoteLogCopy)}' component cannot be used as a monitor.");
            }
        }
    }
}
