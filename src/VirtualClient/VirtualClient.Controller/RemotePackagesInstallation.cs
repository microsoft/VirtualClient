// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Component installs packages from the local system to the remote system in the 
    /// remote agent 'packages' directory.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class RemotePackagesInstallation : VirtualClientControllerComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemotePackagesInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        public RemotePackagesInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
                    PlatformSpecifics targetPlatformSpecifics = new PlatformSpecifics(targetPlatform, targetArchitecture);

                    await this.InstallPackagesAsync(sshTarget, targetPlatformSpecifics, telemetryContext, cancellationToken);
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
        /// Installs packages on the target system.
        /// </summary>
        /// <param name="sshTarget">The SSH client to interface with the remote system.</param>
        /// <param name="targetPlatformArchitecture">The platform and CPU architecture for the target system (e.g. Windows, Linux, X64, ARM64).</param>
        /// <param name="telemetryContext">Provides context information to include in telemetry output.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task InstallPackagesAsync(ISshClientProxy sshTarget, PlatformSpecifics targetPlatformArchitecture, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("agentTargetPlatformArchitecture", targetPlatformArchitecture.PlatformArchitectureName);

            if (!cancellationToken.IsCancellationRequested)
            {
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
                string targetPackagesInstallationPath = VirtualClientControllerComponent.GetDefaultRemotePackagesInstallationPath(targetPlatformArchitecture, sourcePackageName);

                telemetryContext.AddContext("sourcePackagesPath", sourcePackagesPath);
                telemetryContext.AddContext("targetPackagesInstallationPath", targetPackagesInstallationPath);

                // The packages are copied fresh each time. Any directories or files on the target system
                // will be deleted before copying.
                sshTarget.StandardOutput?.WriteLine();
                sshTarget.StandardOutput?.WriteLine($"[Copy Packages to Target]");
                await this.CopyDirectoryToAsync(sshTarget, sourcePackagesPath, targetPackagesInstallationPath, telemetryContext, cancellationToken, force: true);

                // e.g.
                // chmod -R 2777 {target_path}
                await this.SetFilePermissionsAsync(sshTarget, targetPackagesInstallationPath, targetPlatformArchitecture.Platform, cancellationToken);
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
                throw new NotSupportedException($"Invalid component usage. The '{this.TypeName}' component cannot be used as a monitor.");
            }
        }
    }
}
