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
    using Microsoft.Extensions.Logging;
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
        private static readonly SemaphoreSlim AgentOutputLock = new SemaphoreSlim(1, 1);

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
        /// <param name="sshTarget">The SSH client to handle the copy operation.</param>
        /// <param name="remoteDirectoryPath">The target directory on the remote system from which the directory will be copied.</param>
        /// <param name="localDirectoryPath">The directory on the local system to copy into.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected Task CopyDirectoryFromAsync(ISshClientProxy sshTarget, string remoteDirectoryPath, string localDirectoryPath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            sshTarget.ThrowIfNull(nameof(sshTarget));
            localDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(localDirectoryPath));
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(remoteDirectoryPath));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.CopyDirectoryFrom", LogLevel.Trace, relatedContext, async () =>
            {
                DateTime copyStartTime = DateTime.UtcNow;
                relatedContext.AddContext("copyStartTime", copyStartTime);

                try
                {
                    sshTarget.StandardOutput?.WriteLine($"Copy From: {remoteDirectoryPath}");
                    sshTarget.StandardOutput?.WriteLine($"Copy To: {localDirectoryPath}");

                    IDirectoryInfo localDirectory = this.FileSystem.DirectoryInfo.New(localDirectoryPath);
                    if (!localDirectory.Exists)
                    {
                        localDirectory.Create();
                    }

                    await sshTarget.CopyFromAsync(remoteDirectoryPath, localDirectory, cancellationToken);
                }
                finally
                {
                    DateTime copyEndTime = DateTime.UtcNow;
                    TimeSpan copyTimeElapsed = copyEndTime - copyStartTime;
                    sshTarget.StandardOutput?.WriteLine($"Elapsed Time = {copyTimeElapsed}");

                    relatedContext.AddContext("copyEndTime", copyStartTime);
                    relatedContext.AddContext("totalCopyTime", copyTimeElapsed.ToString());
                }
            });
        }

        /// <summary>
        /// Copies a source directory on the local system to the target system via the SSH client session.
        /// </summary>
        /// <param name="sshTarget">The SSH client to handle the copy operation.</param>
        /// <param name="sourceDirectoryPath">The source directory on the local system.</param>
        /// <param name="targetDirectoryPath">The target directory on the remote system to which the directory will be copied.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="outputBuffer">A buffer to write standard output.</param>
        /// <param name="force">True to force the target directory to be recreated from scratch. The directory will be deleted if it exists.</param>
        protected Task CopyDirectoryToAsync(ISshClientProxy sshTarget, string sourceDirectoryPath, string targetDirectoryPath, EventContext telemetryContext, CancellationToken cancellationToken, TextWriter outputBuffer = null, bool force = false)
        {
            sshTarget.ThrowIfNull(nameof(sshTarget));
            sourceDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(sourceDirectoryPath));
            targetDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(targetDirectoryPath));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.CopyDirectoryTo", LogLevel.Trace, relatedContext, async () =>
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
                // double percentageCopied = 0;

                relatedContext.AddContext("totalFilesToCopy", allFiles?.Count());
                relatedContext.AddContext("totalBytesToCopy", totalBytesToCopy);

                // e.g. 10, 20, 30...
                List<double> percentages = Enumerable.Range(1, 10).Select(p => p * 10.0).ToList();

                ////sshClient.CopyingTo += (sender, args) =>
                ////{
                ////    // Note:
                ////    // The Uploading event sends notices as a given file is being uploaded. If the file
                ////    // is large enough, the event is fired multiple times as the file is being copied
                ////    // in parts/buffers. We can determine when a file is fully copied/uploaded when
                ////    // the ScpUploadEventArgs.Uploaded (bytes) equals the ScpUploadEventArgs.Size (bytes).
                ////    double percentageBytesCopied = ((totalBytesCopied + args.Uploaded) / totalBytesToCopy) * 100;
                ////    percentageCopied = Math.Floor(percentageBytesCopied);

                ////    if (percentageCopied > 0 && percentages.Contains(percentageCopied))
                ////    {
                ////        percentages.RemoveAt(0);
                ////        ConsoleLogger.Default.LogMessage($"Copied: {percentageCopied}%", relatedContext);
                ////    }

                ////    if (args.Uploaded >= args.Size)
                ////    {
                ////        totalBytesCopied += args.Size;
                ////    }
                ////};

                DateTime copyStartTime = DateTime.UtcNow;
                relatedContext.AddContext("copyStartTime", copyStartTime);

                try
                {
                    sshTarget.StandardOutput?.WriteLine($"Copy From: {sourceDirectoryPath}");
                    sshTarget.StandardOutput?.WriteLine($"Copy To: {targetDirectoryPath}");

                    await sshTarget.CreateDirectoryAsync(targetDirectoryPath, force: force);
                    await sshTarget.CopyToAsync(source, targetDirectoryPath);
                }
                finally
                {
                    DateTime copyEndTime = DateTime.UtcNow;
                    TimeSpan copyTimeElapsed = copyEndTime - copyStartTime;
                    sshTarget.StandardOutput?.WriteLine($"Elapsed Time: {copyTimeElapsed}", relatedContext);

                    relatedContext.AddContext("copyEndTime", copyStartTime);
                    relatedContext.AddContext("totalCopyTime", copyTimeElapsed.ToString());
                    relatedContext.AddContext("totalBytesCopied", totalBytesCopied);
                }
            });
        }

        /// <summary>
        /// Executes the operations on the target agent system through SSH connections.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.TryGetTargetAgentClients(out IEnumerable<ISshClientProxy> sshClients))
            {
                throw new WorkloadException(
                    "Target agents not defined. One or more target agent SSH connections must be defined on the command line.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            foreach (ISshClientProxy sshClient in sshClients)
            {
                ConsoleLogger.Default.LogMessage($"Target Agent: {sshClient.ConnectionInfo.Username}@{sshClient.ConnectionInfo.Host}", telemetryContext);
            }

            Console.WriteLine();
            List<Task> agentExecutionTasks = new List<Task>();

            foreach (ISshClientProxy sshClient in sshClients)
            {
                EventContext relatedContext = telemetryContext.Clone();
                relatedContext.AddContext("targetAgent", $"{sshClient.ConnectionInfo.Username}@{sshClient.ConnectionInfo.Host}");

                agentExecutionTasks.Add(Task.Run(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteOnTarget", LogLevel.Trace, relatedContext, async () =>
                        {
                            using (TextWriter standardOutput = new StringWriter())
                            {
                                using (TextWriter standardError = new StringWriter())
                                {
                                    sshClient.StandardError = standardOutput;
                                    sshClient.StandardOutput = standardError;

                                    try
                                    {
                                        await this.ExecuteAsync(sshClient, relatedContext, cancellationToken);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        // Expected when a cancellation is requested.
                                    }
                                    catch (Exception exc)
                                    {
                                        sshClient.StandardError?.WriteLine();
                                        sshClient.StandardError?.WriteLine(exc.Message);
                                        throw;
                                    }
                                    finally
                                    {
                                        relatedContext.AddContext("standardOutput", sshClient.StandardOutput);
                                        relatedContext.AddContext("standardError", sshClient.StandardError);

                                        if (!cancellationToken.IsCancellationRequested)
                                        {
                                            VirtualClientControllerComponent.ConsoleLogAgentResultsAsync(sshClient, cancellationToken);
                                        }
                                    }
                                }
                            }
                        });
                    }
                }));
            }

            return Task.WhenAll(agentExecutionTasks);
        }

        /// <summary>
        /// Executes the operations on the target agent through an SSH connection.
        /// </summary>
        /// <param name="sshTarget">The target agent on which to execute the operations.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry data.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(ISshClientProxy sshTarget, EventContext telemetryContext, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the file (and folder) permissions on the target system via the SSH client session
        /// (e.g. executable, read, write).
        /// </summary>
        /// <param name="sshTarget">The SSH client to handle the copy operation.</param>
        /// <param name="targetPath">The source directory on the local system.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task SetFilePermissionsAsync(ISshClientProxy sshTarget, string targetPath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            sshTarget.ThrowIfNull(nameof(sshTarget));
            targetPath.ThrowIfNullOrWhiteSpace(nameof(targetPath));

            if (targetPlatform == PlatformID.Unix)
            {
                sshTarget.StandardOutput?.WriteLine($"Set Permissions: {targetPath}");
                await sshTarget.ExecuteCommandAsync($"chmod -R 2777 \"{targetPath}\"");
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
        /// <param name="sshClients">Target agent SSH client as defined on the command line.</param>
        /// <returns>True if target agent SSH clients exist. False if not.</returns>
        protected bool TryGetTargetAgentClients(out IEnumerable<ISshClientProxy> sshClients)
        {
            sshClients = null;
            if (this.Dependencies.TryGetService<IEnumerable<ISshClientProxy>>(out IEnumerable<ISshClientProxy> client))
            {
                sshClients = client;
            }

            return sshClients != null;
        }

        private static async Task ConsoleLogAgentResultsAsync(ISshClientProxy sshClient, CancellationToken cancellationToken)
        {
            await VirtualClientControllerComponent.AgentOutputLock.WaitAsync(cancellationToken);

            try
            {
                Console.WriteLine($"Agent Results: {sshClient.ConnectionInfo.Username}@{sshClient.ConnectionInfo.Host}");
                Console.WriteLine($"------------------------------------------------------------");
                Console.WriteLine($"{sshClient.StandardOutput}{Environment.NewLine}{Environment.NewLine}{sshClient.StandardError}".Trim());
                Console.WriteLine();
            }
            finally
            {
                VirtualClientControllerComponent.AgentOutputLock.Release();
            }
        }
    }
}
