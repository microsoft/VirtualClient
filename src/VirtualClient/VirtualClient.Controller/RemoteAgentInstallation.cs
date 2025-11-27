// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using PlatformArchitecture = VirtualClient.Contracts.PlatformSpecifics;

    /// <summary>
    /// Component executes an SSH command on a target system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class RemoteAgentInstallation : RemotePackagesInstallation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoteAgentInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public RemoteAgentInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
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

                if (!cancellationToken.IsCancellationRequested)
                {
                    Tuple<PlatformID, Architecture> platformArchitecture = await sshTarget.GetTargetPlatformArchitectureAsync(cancellationToken);
                    PlatformID targetPlatform = platformArchitecture.Item1;
                    Architecture targetArchitecture = platformArchitecture.Item2;
                    PlatformArchitecture targetPlatformSpecifics = new PlatformArchitecture(targetPlatform, targetArchitecture);

                    telemetryContext.AddContext("agentTargetPlatformArchitecture", targetPlatformSpecifics);

                    if (!this.TryGetAgentPackagePath(targetPlatformSpecifics, out string agentPackagePath))
                    {
                        throw new DependencyException(
                            $"Agent package not found. A package of this agent for platform/architecture '{targetPlatformSpecifics.PlatformArchitectureName}' was not found " +
                            $"on the current system. The target system requires an installation of the agent for this platform/architecture in order " +
                            $"to execute remote workflows. Ensure that the agent installation on the current system is a complete package containing all required " +
                            $"platform/architecture folders (e.g. {{agent_package}}/content/linux-arm64, linux-x64, win-arm64, win-x64).",
                            ErrorReason.DependencyNotFound);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        string targetInstallationPath = AgentSpecifics.GetRemoteAgentInstallationPath(targetPlatformSpecifics);

                        telemetryContext.AddContext("agentPackagePath", agentPackagePath);
                        telemetryContext.AddContext("agentTargetInstallationPath", targetInstallationPath);

                        // The agent is copied fresh each time. Any directories or files on the target system
                        // will be deleted before copying.

                        sshTarget.StandardOutput?.WriteLine();
                        sshTarget.StandardOutput?.WriteLine($"[Copy Agent to Target]");
                        await this.CopyDirectoryToAsync(sshTarget, agentPackagePath, targetInstallationPath, telemetryContext, cancellationToken, force: true);

                        // e.g.
                        // chmod -R 2777 {target_path}
                        await this.SetFilePermissionsAsync(sshTarget, targetInstallationPath, targetPlatform, cancellationToken);
                    }
                }
            }
            finally
            {
                sshTarget.Disconnect();
            }
        }

        /// <summary>
        /// Returns true/false whether the agent package for the target platform architecture is found.
        /// </summary>
        /// <param name="targetPlatformArchitecture">The platform/architecture for the target/remote system (e.g. linux-arm64, win-x64).</param>
        /// <param name="agentPackagePath">The path to the folder that contains the build of the agent to install on the target/remote system.</param>
        /// <returns>True if the correct agent for the target platform/architecture is found.</returns>
        protected bool TryGetAgentPackagePath(PlatformArchitecture targetPlatformArchitecture, out string agentPackagePath)
        {
            agentPackagePath = null;
            IDirectoryInfo currentDirectory = this.FileSystem.DirectoryInfo.New(this.PlatformSpecifics.CurrentDirectory).Parent;

            while (currentDirectory != null)
            {
                string matchingDirectory = this.FileSystem.Directory.EnumerateDirectories(
                    currentDirectory.FullName, 
                    targetPlatformArchitecture.PlatformArchitectureName, 
                    SearchOption.TopDirectoryOnly)
                    ?.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(matchingDirectory))
                {
                    agentPackagePath = matchingDirectory;
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            return agentPackagePath != null;
        }
    }
}
