// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Represents a component that performs remote execution on a target system through
    /// an SSH session.
    /// </summary>
    public abstract class VirtualClientControllerComponent : VirtualClientComponent
    {
        internal static readonly string AgentName = Path.GetFileNameWithoutExtension(VirtualClientRuntime.ExecutableName);

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientControllerComponent"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies by the component.</param>
        /// <param name="parameters">Parameters provided to the component.</param>
        protected VirtualClientControllerComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.FileSystem = dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Provides an interface to the local file system.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// Returns the default installation path for the remote agent.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{AgentFolder}/{PlatformArchitecture}<br/>
        /// (e.g. /home/user/Agent/linux-arm64).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{AgentFolder}\{PlatformArchitecture}<br/>
        /// (e.g. C:\Users\Agent\win-x64).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <returns>A path on the target system to which the remote agent exists or can be installed.</returns>
        protected static string GetDefaultRemoteAgentInstallationPath(PlatformSpecifics targetPlatformSpecifics)
        {
            string defaultPath = null;
            string agentName = VirtualClientControllerComponent.AgentName;

            if (targetPlatformSpecifics.Platform == PlatformID.Unix)
            {
                defaultPath = $"$HOME/{agentName}/{targetPlatformSpecifics.PlatformArchitectureName}";
            }
            else if (targetPlatformSpecifics.Platform == PlatformID.Win32NT)
            {
                defaultPath = $"%USERPROFILE%\\{agentName}\\{targetPlatformSpecifics.PlatformArchitectureName}";
            }

            if (string.IsNullOrWhiteSpace(defaultPath))
            {
                throw new NotSupportedException(
                    $"Unsupported platform '{targetPlatformSpecifics.PlatformArchitectureName}'. Supported platforms are Unix and Windows.");
            }

            return defaultPath;
        }

        /// <summary>
        /// Returns the default path for logs/log files on the remote system.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{AgentFolder}/{PlatformArchitecture}/logs<br/>
        /// (e.g. /home/user/Agent/linux-arm64/logs).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{AgentFolder}\{PlatformArchitecture}\logs<br/>
        /// (e.g. C:\Users\Agent\win-arm64\logs).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <returns>A path on the target system to which the remote packages exists or can be installed.</returns>
        protected static string GetDefaultRemoteLogsPath(PlatformSpecifics targetPlatformSpecifics)
        {
            string defaultPath = null;
            string agentName = VirtualClientControllerComponent.AgentName;

            if (targetPlatformSpecifics.Platform == PlatformID.Unix)
            {
                defaultPath = $"$HOME/{agentName}/logs";
            }
            else if (targetPlatformSpecifics.Platform == PlatformID.Win32NT)
            {
                defaultPath = $"%USERPROFILE%\\{agentName}\\logs";
            }

            if (string.IsNullOrWhiteSpace(defaultPath))
            {
                throw new NotSupportedException(
                    $"Unsupported platform '{targetPlatformSpecifics.PlatformArchitectureName}'. Supported platforms are Unix and Windows.");
            }

            return defaultPath;
        }

        /// <summary>
        /// Returns the default installation path for packages on the remote system.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{AgentFolder}/packages<br/>
        /// (e.g. /home/user/Agent/packages).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{AgentFolder}\packages<br/>
        /// (e.g. C:\Users\Agent\packages).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="packagesFolder">The folder name for the packages allowing for different packages.</param>
        /// <returns>A path on the target system to which the remote packages exists or can be installed.</returns>
        protected static string GetDefaultRemotePackagesInstallationPath(PlatformSpecifics targetPlatformSpecifics, string packagesFolder = "packages")
        {
            string defaultPath = null;
            string agentName = VirtualClientControllerComponent.AgentName;

            if (targetPlatformSpecifics.Platform == PlatformID.Unix)
            {
                defaultPath = $"$HOME/{agentName}/{packagesFolder}";
            }
            else if (targetPlatformSpecifics.Platform == PlatformID.Win32NT)
            {
                defaultPath = $"%USERPROFILE%\\{agentName}\\{packagesFolder}";
            }

            if (string.IsNullOrWhiteSpace(defaultPath))
            {
                throw new NotSupportedException(
                    $"Unsupported platform '{targetPlatformSpecifics.PlatformArchitectureName}'. Supported platforms are Unix and Windows.");
            }

            return defaultPath;
        }

        /// <summary>
        /// Adds default command line options to the command to provide to a target/remote system.
        /// </summary>
        protected string AddDefaultCommandLineOptions(PlatformSpecifics targetPlatformSpecifics, string command, string packagesFolder)
        {
            // TODO:
            // This is NOT going to fly for long. This is a temporary workaround to the fact that the
            // OptionFactory is defined in the VirtualClient.Main project and this is where the option names
            // are defined. Duplicating these here is be easy to break.
            string effectiveCommand = command;
            Regex experimentIdExpression = new Regex("--e=|--experiment=|--experiment-id=|--experimentId=|--experimentid=");
            Regex packageDirectoryExpression = new Regex("--pdir=|--package-dir=");
            Regex logDirectoryExpression = new Regex("--ldir=|--log-dir=");
            Regex logToFileExpression = new Regex("--ltf|--log-to-file");

            if (!experimentIdExpression.IsMatch(effectiveCommand))
            {
                effectiveCommand += $" --experiment-id={this.ExperimentId}";
            }

            if (!packageDirectoryExpression.IsMatch(effectiveCommand))
            {
                effectiveCommand += $" --package-dir={VirtualClientControllerComponent.GetDefaultRemotePackagesInstallationPath(targetPlatformSpecifics, packagesFolder)}";
            }

            if (!logDirectoryExpression.IsMatch(command))
            {
                effectiveCommand += $" --log-dir={VirtualClientControllerComponent.GetDefaultRemoteLogsPath(targetPlatformSpecifics)}";
            }

            if (!logToFileExpression.IsMatch(effectiveCommand))
            {
                effectiveCommand += $" --log-to-file";
            }

            return effectiveCommand;
        }

        /// <summary>
        /// Copies a directory on the target system to the local system via the SSH client session.
        /// </summary>
        /// <param name="sshClient">The SSH client to handle the copy operation.</param>
        /// <param name="remoteDirectoryPath">The target directory on the remote system from which the directory will be copied.</param>
        /// <param name="localDirectoryPath">The directory on the local system to copy into.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task CopyDirectoryFromAsync(ISshClientProxy sshClient, string remoteDirectoryPath, string localDirectoryPath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            sshClient.ThrowIfNull(nameof(sshClient));
            localDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(localDirectoryPath));
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(remoteDirectoryPath));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                EventContext relatedContext = telemetryContext.Clone();
                await this.Logger.LogMessageAsync($"{this.TypeName}.CopyDirectoryFrom", relatedContext, async () =>
                {
                    DateTime copyStartTime = DateTime.UtcNow;
                    relatedContext.AddContext("copyStartTime", copyStartTime);

                    try
                    {
                        ConsoleLogger.Default.LogMessage($"Copy From: {remoteDirectoryPath}", relatedContext);
                        ConsoleLogger.Default.LogMessage($"Copy To: {localDirectoryPath}", relatedContext);

                        IDirectoryInfo localDirectory = this.FileSystem.DirectoryInfo.New(localDirectoryPath);
                        if (!localDirectory.Exists)
                        {
                            localDirectory.Create();
                        }

                        await sshClient.CopyFromAsync(remoteDirectoryPath, localDirectory, cancellationToken);
                    }
                    finally
                    {
                        DateTime copyEndTime = DateTime.UtcNow;
                        TimeSpan copyTimeElapsed = copyEndTime - copyStartTime;
                        ConsoleLogger.Default.LogMessage($"Elapsed Time = {copyTimeElapsed}", relatedContext);

                        relatedContext.AddContext("copyEndTime", copyStartTime);
                        relatedContext.AddContext("totalCopyTime", copyTimeElapsed.ToString());
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Can occur if a cancellation is requested.
            }
        }

        /// <summary>
        /// Copies a source directory on the local system to the target system via the SSH client session.
        /// </summary>
        /// <param name="sshClient">The SSH client to handle the copy operation.</param>
        /// <param name="sourceDirectoryPath">The source directory on the local system.</param>
        /// <param name="targetDirectoryPath">The target directory on the remote system to which the directory will be copied.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="outputBuffer">A buffer to write standard output.</param>
        /// <param name="force">True to force the target directory to be recreated from scratch. The directory will be deleted if it exists.</param>
        protected async Task CopyDirectoryToAsync(ISshClientProxy sshClient, string sourceDirectoryPath, string targetDirectoryPath, EventContext telemetryContext, CancellationToken cancellationToken, TextWriter outputBuffer = null, bool force = false)
        {
            sshClient.ThrowIfNull(nameof(sshClient));
            sourceDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(sourceDirectoryPath));
            targetDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(targetDirectoryPath));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                EventContext relatedContext = telemetryContext.Clone();
                await this.Logger.LogMessageAsync($"{this.TypeName}.CopyDirectoryTo", relatedContext, async () =>
                {
                    IDirectoryInfo source = this.FileSystem.DirectoryInfo.New(sourceDirectoryPath);
                    IEnumerable<IFileInfo> allFiles = source.EnumerateFiles("*.*", SearchOption.AllDirectories);

                    if (allFiles?.Any() != true)
                    {
                        throw new DependencyException(
                            $"Invalid source directory. The source package/directory at the path '{sourceDirectoryPath}' does not contain any files.",
                            ErrorReason.DependencyNotFound);
                    }

                    double totalBytesToCopy = allFiles.Sum(file => file.Length);
                    double totalBytesCopied = 0;
                    double percentageCopied = 0;

                    relatedContext.AddContext("totalFilesToCopy", allFiles?.Count());
                    relatedContext.AddContext("totalBytesToCopy", totalBytesToCopy);

                    // e.g. 10, 20, 30...
                    List<double> percentages = Enumerable.Range(1, 10).Select(p => p * 10.0).ToList();

                    sshClient.CopyingTo += (sender, args) =>
                    {
                        // Note:
                        // The Uploading event sends notices as a given file is being uploaded. If the file
                        // is large enough, the event is fired multiple times as the file is being copied
                        // in parts/buffers. We can determine when a file is fully copied/uploaded when
                        // the ScpUploadEventArgs.Uploaded (bytes) equals the ScpUploadEventArgs.Size (bytes).
                        double percentageBytesCopied = ((totalBytesCopied + args.Uploaded) / totalBytesToCopy) * 100;
                        percentageCopied = Math.Floor(percentageBytesCopied);

                        if (percentageCopied > 0 && percentages.Contains(percentageCopied))
                        {
                            percentages.RemoveAt(0);
                            ConsoleLogger.Default.LogMessage($"Copied: {percentageCopied}%", relatedContext);
                        }

                        if (args.Uploaded >= args.Size)
                        {
                            totalBytesCopied += args.Size;
                        }
                    };

                    DateTime copyStartTime = DateTime.UtcNow;
                    relatedContext.AddContext("copyStartTime", copyStartTime);

                    try
                    {
                        ConsoleLogger.Default.LogMessage($"Copy From: {sourceDirectoryPath}", relatedContext);
                        ConsoleLogger.Default.LogMessage($"Copy To: {targetDirectoryPath}", relatedContext);

                        await sshClient.CreateDirectoryAsync(targetDirectoryPath, force: force);
                        await sshClient.CopyToAsync(source, targetDirectoryPath);
                    }
                    finally
                    {
                        DateTime copyEndTime = DateTime.UtcNow;
                        TimeSpan copyTimeElapsed = copyEndTime - copyStartTime;
                        ConsoleLogger.Default.LogMessage($"Elapsed Time = {copyTimeElapsed}", relatedContext);

                        relatedContext.AddContext("copyEndTime", copyStartTime);
                        relatedContext.AddContext("totalCopyTime", copyTimeElapsed.ToString());
                        relatedContext.AddContext("totalBytesCopied", totalBytesCopied);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Can occur if a cancellation is requested.
            }
        }

        /// <summary>
        /// Executes the operations on the target agent system through SSH connections.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.TryGetTargetAgentSshClient(out ISshClientProxy targetAgent))
            {
                throw new WorkloadException(
                    "Target agents not defined. One or more target agent SSH connections must be defined on the command line.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            EventContext relatedContext = telemetryContext.Clone();
            relatedContext.AddContext("targetAgent", $"{targetAgent.ConnectionInfo.Username}@{targetAgent.ConnectionInfo.Host}");

            return this.ExecuteAsync(targetAgent, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes the operations on the target agent through an SSH connection.
        /// </summary>
        /// <param name="targetAgent">The target agent on which to execute the operations.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(ISshClientProxy targetAgent, EventContext telemetryContext, CancellationToken cancellationToken);

        /// <summary>
        /// Writes text to standard output indicating remote execution beginning.
        /// </summary>
        protected void LogRemoteOperation(ISshClientProxy client, params string[] text)
        {
            ConsoleLogger.Default.LogMessage(" ", EventContext.None);
            ConsoleLogger.Default.LogMessage("**********************************************************", EventContext.None);

            foreach (string line in text)
            {
                ConsoleLogger.Default.LogMessage($"{client.ConnectionInfo.Host}: {line}", EventContext.None);
            }

            ConsoleLogger.Default.LogMessage("**********************************************************", EventContext.None);
            ConsoleLogger.Default.LogMessage(" ", EventContext.None);
        }

        /// <summary>
        /// Sets the file (and folder) permissions on the target system via the SSH client session
        /// (e.g. executable, read, write).
        /// </summary>
        /// <param name="sshClient">The SSH client to handle the copy operation.</param>
        /// <param name="targetPath">The source directory on the local system.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task SetFilePermissionsAsync(ISshClientProxy sshClient, string targetPath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            sshClient.ThrowIfNull(nameof(sshClient));
            targetPath.ThrowIfNullOrWhiteSpace(nameof(targetPath));

            if (targetPlatform == PlatformID.Unix)
            {
                await sshClient.ExecuteCommandAsync($"chmod -R 2777 \"{targetPath}\"");
            }
        }

        /// <summary>
        /// Throws an exception if the result indicates an error.
        /// </summary>
        protected void ThrowIfCommandFailed(ProcessDetails result, params int[] successCodes)
        {
            bool isErrored = result.ExitCode != 0;
            if (successCodes?.Any() == true)
            {
                isErrored = !successCodes.Contains(result.ExitCode);
            }

            if (isErrored)
            {
                StringBuilder errorMessage = new StringBuilder();
                errorMessage.AppendLine($"SSH command execution failed (exit code = {result.ExitCode}, command = {SensitiveData.ObscureSecrets(result.CommandLine)}).");

                if (!string.IsNullOrWhiteSpace(result.StandardError))
                {
                    errorMessage.AppendLine();
                    errorMessage.AppendLine(result.StandardError);
                }

                if (this.ComponentType == ComponentType.Action)
                {
                    throw new WorkloadException(errorMessage.ToString(), ErrorReason.WorkloadFailed);
                }
                else if (this.ComponentType == ComponentType.Dependency)
                {
                    throw new DependencyException(errorMessage.ToString(), ErrorReason.DependencyInstallationFailed);
                }
            }
        }

        /// <summary>
        /// Returns true if a target agent SSH client is defined (i.e. as supplied on the command line).
        /// </summary>
        /// <param name="sshClient">Target agent SSH client as defined on the command line.</param>
        /// <returns>True if target agent SSH clients exist. False if not.</returns>
        protected bool TryGetTargetAgentSshClient(out ISshClientProxy sshClient)
        {
            sshClient = null;
            if (this.Dependencies.TryGetService<ISshClientProxy>(out ISshClientProxy client))
            {
                sshClient = client;
            }

            return sshClient != null;
        }
    }
}
