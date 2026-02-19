// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.Options;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Renci.SshNet;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Component executes the Virtual Client command line on a target system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class RemoteAgentExecutor : VirtualClientControllerComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAgentExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public RemoteAgentExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            // Prevent any of the parameters from being evaluated. They MUST be shipped to the target system
            // as-is and resolved on that target system.
            this.ParametersEvaluated = true;
        }

        /// <summary>
        /// The command to execute on the remote system. When not defined, the full command
        /// line pass to Virtual Client will be executed.
        /// </summary>
        public string Command
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Command), out IConvertible command);
                return command?.ToString();
            }
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
            try
            {
                await sshTarget.ConnectAsync(cancellationToken);
                await this.ExecuteAgentAsync(sshTarget, telemetryContext, cancellationToken);

                if (this.CopyLogs)
                {
                    await this.CopyLogsAsync(sshTarget, telemetryContext, cancellationToken);
                }
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

        /// <summary>
        /// Executes the agent command line on the target system.
        /// </summary>
        /// <param name="sshTarget">The SSH client to interface with the remote system.</param>
        /// <param name="telemetryContext">Provides context information to include in telemetry output.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ExecuteAgentAsync(ISshClientProxy sshTarget, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Tuple<PlatformID, Architecture> platformArchitecture = await sshTarget.GetTargetPlatformArchitectureAsync(cancellationToken);
            PlatformID targetPlatform = platformArchitecture.Item1;
            Architecture targetArchitecture = platformArchitecture.Item2;
            PlatformSpecifics targetPlatformSpecifics = new PlatformSpecifics(targetPlatform, targetArchitecture);

            if (this.SupportedPlatforms?.Any() == true && !this.SupportedPlatforms.Any(platform => string.Equals(platform, targetPlatformSpecifics.PlatformArchitectureName)))
            {
                return;
            }

            // The default packages directory for controller scenarios will be a peer directory
            // of the agent folder.
            //
            // e.g.
            // C:\Users\Any\AgentPackage\linux-arm64
            // C:\Users\Any\AgentPackage\linux-x64
            // C:\Users\Any\AgentPackage\win-arm64
            // C:\Users\Any\AgentPackage\win-x64
            // C:\Users\Any\AgentPackage\packages
            string sourcePackagesPath = this.GetPackagePath();

            // We support different packages folders which can be defined on the command line
            // (e.g. packages, packages-installers).
            string sourcePackageName = Path.GetFileNameWithoutExtension(sourcePackagesPath);

            string agentName = RemoteAgentExecutor.AgentName;
            if (targetPlatform == PlatformID.Unix)
            {
                agentName = $"./{agentName}";
            }

            string agentInstallationPath = VirtualClientControllerComponent.GetDefaultRemoteAgentInstallationPath(targetPlatformSpecifics);
            string targetCommand = $"{agentName} {this.Command}";
            targetCommand = this.AddDefaultCommandLineOptions(targetPlatformSpecifics, targetCommand, sourcePackageName);

            sshTarget.StandardOutput?.WriteLine();
            sshTarget.StandardOutput?.WriteLine($"[Execute on Target]");
            sshTarget.StandardOutput?.WriteLine($"Command: {SensitiveData.ObscureSecrets(this.Command)}");

            telemetryContext.AddContext("agentInstallationPath", agentInstallationPath);
            telemetryContext.AddContext("agentTargetCommand", SensitiveData.ObscureSecrets(targetCommand));

            string fullTargetCommand = $"cd \"{agentInstallationPath}\"&&{targetCommand}";

            // Execute the VC/agent command on the target system. In practice, we are simply passing in the command line
            // provided on the controller to the agent.
            ProcessDetails result = await sshTarget.ExecuteCommandAsync(fullTargetCommand, cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(result, telemetryContext);
                this.ThrowIfCommandFailed(result);
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
                throw new NotSupportedException($"Invalid component usage. The '{nameof(RemoteAgentExecutor)}' component cannot be used as a monitor.");
            }
        }
    }
}
