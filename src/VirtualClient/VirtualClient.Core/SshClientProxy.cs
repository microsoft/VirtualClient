// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Renci.SshNet;
    using Renci.SshNet.Common;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A proxy for an SSH client.
    /// </summary>
    public class SshClientProxy : ISshClientProxy
    {
        private static readonly Regex SshTargetExpression = new Regex(@"([0-9a-z_\-\. ]+)@([^;]+);([\x20-\x7E]+)", RegexOptions.IgnoreCase);
        private static readonly Regex FileDoesNotExistExpression = new Regex("no such|not found|cannot find", RegexOptions.IgnoreCase);

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SshClientProxy"/> class.
        /// </summary>
        /// <param name="connectionInfo">The SSH client connection information to use for SSH session operations.</param>
        public SshClientProxy(ConnectionInfo connectionInfo)
        {
            connectionInfo.ThrowIfNull(nameof(connectionInfo));
            this.SessionClient = new SshClient(connectionInfo);
            this.SessionScpClient = new ScpClient(connectionInfo);
        }

        /// <summary>
        /// Event is invoked as each file (or parts of a file) are successfully copied
        /// to the target system.
        /// </summary>
        public event EventHandler<ScpUploadEventArgs> CopyingTo;

        /// <summary>
        /// Event is invoked as each file (or parts of a file) are successfully copied
        /// from the target system.
        /// </summary>
        public event EventHandler<ScpDownloadEventArgs> CopyingFrom;

        /// <inheritdoc />
        public ConnectionInfo ConnectionInfo
        {
            get
            {
                return this.SessionClient?.ConnectionInfo;
            }
        }

        /// <summary>
        /// When defined, allows the capture of standard error from the execution of the commands through 
        /// the SSH session.
        /// </summary>
        public TextWriter StandardError { get; set; }

        /// <summary>
        /// When defined, allows the capture of standard output from the execution of the commands through 
        /// the SSH session.
        /// </summary>
        public TextWriter StandardOutput { get; set; }

        /// <summary>
        /// The underlying SSH client.
        /// </summary>
        internal SshClient SessionClient { get; }

        /// <summary>
        /// The underlying SFTP client.
        /// </summary>
        internal ScpClient SessionScpClient { get; }

        /// <summary>
        /// Returns true if the target format is correct and output the SSH host and username information.
        /// </summary>
        /// <param name="sshTarget">The SSH target information (e.g. user01@192.168.1.15).</param>
        /// <param name="host">The host name/IP address (e.g. 192.168.1.15) for the SSH session.</param>
        /// <param name="username">The username to use for the SSH session.</param>
        /// <param name="password">The password to use for the SSH session.</param>
        /// <returns>True if the host and username information can be determined from the SSH target value.</returns>
        public static bool TryGetSshTargetInformation(string sshTarget, out string host, out string username, out string password)
        {
            host = null;
            username = null;
            password = null;
            Match targetMatch = SshClientProxy.SshTargetExpression.Match(sshTarget);

            if (targetMatch.Success)
            {
                username = targetMatch.Groups[1].Value.Trim();
                host = targetMatch.Groups[2].Value.Trim();
                password = targetMatch.Groups[3].Value.Trim();
            }

            return host != null;
        }

        /// <inheritdoc />
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            await this.SessionClient.ConnectAsync(cancellationToken);
            await this.SessionScpClient.ConnectAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task CopyFromAsync(string remoteDirectoryPath, IDirectoryInfo destination, CancellationToken cancellationToken = default)
        {
            destination.ThrowIfNull(nameof(destination));
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(remoteDirectoryPath);

            this.ValidateSessionConnection();

            if (this.CopyingFrom != null)
            {
                this.SessionScpClient.Downloading += this.CopyingFrom;
            }

            try
            {
                await this.CopyDirectoryFromRemoteSystemAsync(remoteDirectoryPath, destination, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingFrom != null)
                {
                    this.SessionScpClient.Downloading -= this.CopyingFrom;
                }
            }
        }

        /// <inheritdoc />
        public async Task CopyFromAsync(string remoteFilePath, Stream destination, CancellationToken cancellationToken = default)
        {
            destination.ThrowIfNull(nameof(destination));
            remoteFilePath.ThrowIfNullOrWhiteSpace(remoteFilePath);

            this.ValidateSessionConnection();

            if (this.CopyingFrom != null)
            {
                this.SessionScpClient.Downloading += this.CopyingFrom;
            }

            try
            {
                await this.CopyFileFromRemoteSystemAsync(remoteFilePath, destination, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingFrom != null)
                {
                    this.SessionScpClient.Downloading -= this.CopyingFrom;
                }
            }
        }

        /// <inheritdoc />
        public async Task CopyFromAsync(string remoteFilePath, IFileInfo destination, CancellationToken cancellationToken = default)
        {
            destination.ThrowIfNull(nameof(destination));
            remoteFilePath.ThrowIfNullOrWhiteSpace(remoteFilePath);

            this.ValidateSessionConnection();

            if (this.CopyingFrom != null)
            {
                this.SessionScpClient.Downloading += this.CopyingFrom;
            }

            try
            {
                await this.CopyFileFromRemoteSystemAsync(remoteFilePath, destination, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingFrom != null)
                {
                    this.SessionScpClient.Downloading -= this.CopyingFrom;
                }
            }
        }

        /// <inheritdoc />
        public async Task CopyToAsync(Stream source, string remoteFilePath, CancellationToken cancellationToken = default)
        {
            source.ThrowIfNull(nameof(source));
            remoteFilePath.ThrowIfNullOrWhiteSpace(remoteFilePath);

            this.ValidateSessionConnection();

            try
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading += this.CopyingTo;
                }

                await this.CopyToFileOnRemoteSystemAsync(source, remoteFilePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading -= this.CopyingTo;
                }
            }
        }

        /// <inheritdoc />
        public async Task CopyToAsync(IFileInfo source, string remoteFilePath, CancellationToken cancellationToken = default)
        {
            source.ThrowIfNull(nameof(source));
            remoteFilePath.ThrowIfNullOrWhiteSpace(remoteFilePath);

            this.ValidateSessionConnection();

            try
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading += this.CopyingTo;
                }

                await this.CopyToFileOnRemoteSystemAsync(source, remoteFilePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading -= this.CopyingTo;
                }
            }
        }

        /// <inheritdoc />
        public async Task CopyToAsync(IDirectoryInfo source, string remoteDirectoryPath, CancellationToken cancellationToken = default)
        {
            source.ThrowIfNull(nameof(source));
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(remoteDirectoryPath);

            this.ValidateSessionConnection();

            try
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading += this.CopyingTo;
                }

                await this.CopyToDirectoryOnRemoteSystemAsync(source, remoteDirectoryPath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
            finally
            {
                if (this.CopyingTo != null)
                {
                    this.SessionScpClient.Uploading -= this.CopyingTo;
                }
            }
        }

        /// <inheritdoc />
        public async Task CreateDirectoryAsync(string remoteDirectoryPath, bool force = false, CancellationToken cancellationToken = default)
        {
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(remoteDirectoryPath));
            this.ValidateSessionConnection();

            try
            {
                Tuple<PlatformID, Architecture> platformArchitecture = await this.GetTargetPlatformArchitectureAsync(cancellationToken);
                PlatformID targetPlatform = platformArchitecture.Item1;

                bool directoryExists = await this.ExistsOnRemoteSystemAsync(remoteDirectoryPath, targetPlatform, cancellationToken);

                if (directoryExists && force)
                {
                    await this.DeleteDirectoryOnRemoteSystemAsync(remoteDirectoryPath, targetPlatform, cancellationToken);
                }

                if (!directoryExists || force)
                {
                    await this.CreateDirectoryOnRemoteSystemAsync(remoteDirectoryPath, targetPlatform, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
        }

        /// <inheritdoc />
        public async Task DeleteDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
        {
            remoteDirectoryPath.ThrowIfNullOrWhiteSpace(nameof(remoteDirectoryPath));
            this.ValidateSessionConnection();

            try
            {
                Tuple<PlatformID, Architecture> platformArchitecture = await this.GetTargetPlatformArchitectureAsync(cancellationToken);
                PlatformID targetPlatform = platformArchitecture.Item1;

                await this.DeleteDirectoryOnRemoteSystemAsync(remoteDirectoryPath, targetPlatform, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
        }

        /// <inheritdoc />
        public async Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default)
        {
            remoteFilePath.ThrowIfNullOrWhiteSpace(nameof(remoteFilePath));
            this.ValidateSessionConnection();

            try
            {
                Tuple<PlatformID, Architecture> platformArchitecture = await this.GetTargetPlatformArchitectureAsync(cancellationToken);
                PlatformID targetPlatform = platformArchitecture.Item1;

                await this.DeleteFileOnRemoteSystemAsync(remoteFilePath, targetPlatform, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            try
            {
                if (this.SessionClient.IsConnected)
                {
                    this.SessionClient.Disconnect();
                }
            }
            catch
            {
                // The sessions may not be connected. Handle this case.
            }

            try
            {
                if (this.SessionScpClient.IsConnected)
                {
                    this.SessionScpClient.Disconnect();
                }
            }
            catch
            {
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public async Task<ProcessDetails> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default, Action<SshCommandOutputInfo> outputReceived = null)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            this.ValidateSessionConnection();

            ProcessDetails result = null;

            try
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync(command, cancellationToken, outputReceived);

                if (result != null && this.StandardError != null && result.StandardError != null)
                {
                    await this.StandardError.WriteAsync(result.StandardError);
                }

                if (result != null && this.StandardOutput != null && result.StandardOutput != null)
                {
                    await this.StandardOutput.WriteAsync(result.StandardOutput);
                }
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            remotePath.ThrowIfNullOrWhiteSpace(nameof(remotePath));
            this.ValidateSessionConnection();

            bool exists = false;

            try
            {
                Tuple<PlatformID, Architecture> platformArchitecture = await this.GetTargetPlatformArchitectureAsync(cancellationToken);
                PlatformID targetPlatform = platformArchitecture.Item1;

                exists = await this.ExistsOnRemoteSystemAsync(remotePath, targetPlatform, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Happens if a cancellation request is issued.
            }

            return exists;
        }

        /// <summary>
        /// Copies the file on the remote system to a stream.
        /// </summary>
        /// <param name="remoteFilePath">The path on the remote system for the file to copy to the local system.</param>
        /// <param name="destination">The content stream to copy into.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyFileFromRemoteSystemAsync(string remoteFilePath, Stream destination, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                this.SessionScpClient.Download(remoteFilePath, destination);
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        /// Copies the file on the remote system to a file on the local system.
        /// </summary>
        /// <param name="remoteFilePath">The path on the remote system for the file to copy to the local system.</param>
        /// <param name="destination">The file to copy into on the local system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyFileFromRemoteSystemAsync(string remoteFilePath, IFileInfo destination, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                this.SessionScpClient.Download(remoteFilePath, new FileInfo(destination.FullName));
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        /// Copies the content from a directory on the remote system to a directory on the local system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to copy to the local system.</param>
        /// <param name="destination">The directory to copy into on the local system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyDirectoryFromRemoteSystemAsync(string remoteDirectoryPath, IDirectoryInfo destination, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                return Task.Factory.StartNew(() =>
                {
                    this.SessionScpClient.Download(remoteDirectoryPath, new DirectoryInfo(destination.FullName));
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            });
        }

        /// <summary>
        /// Copies the stream contents to a file on the remote system.
        /// </summary>
        /// <param name="source">The content stream to copy to the remote system.</param>
        /// <param name="remoteFilePath">The path on the remote system for the file to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyToFileOnRemoteSystemAsync(Stream source, string remoteFilePath, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                this.SessionScpClient.Upload(source, remoteFilePath);
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        /// Copies a file on the local system to a file on the remote system.
        /// </summary>
        /// <param name="source">The file to copy to the remote system.</param>
        /// <param name="remoteFilePath">The path on the remote system for the file to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyToFileOnRemoteSystemAsync(IFileInfo source, string remoteFilePath, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                this.SessionScpClient.Upload(new FileInfo(source.FullName), remoteFilePath);
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        /// Copies a directory on the local system to a directory on the remote system.
        /// </summary>
        /// <param name="source">The directory to copy to the remote system.</param>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task CopyToDirectoryOnRemoteSystemAsync(IDirectoryInfo source, string remoteDirectoryPath, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                this.SessionScpClient.Upload(new DirectoryInfo(source.FullName), remoteDirectoryPath);
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <summary>
        /// Creates a directory on the remote system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to create.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task CreateDirectoryOnRemoteSystemAsync(string remoteDirectoryPath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            ProcessDetails result = null;
            if (targetPlatform == PlatformID.Unix)
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"mkdir -p \"{remoteDirectoryPath}\"", cancellationToken);
            }
            else
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"mkdir \"{remoteDirectoryPath}\"", cancellationToken);
            }

            if (result.ExitCode != 0)
            {
                throw new SshException(
                    $"Directory creation request failed for '{remoteDirectoryPath}'." +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardOutput}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardError}".Trim());
            }
        }

        /// <summary>
        /// Deletes a directory (and all subdirectories) on the remote system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to delete.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task DeleteDirectoryOnRemoteSystemAsync(string remoteDirectoryPath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            ProcessDetails result = null;
            if (targetPlatform == PlatformID.Unix)
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"rm -rf \"{remoteDirectoryPath}\"", cancellationToken);
            }
            else
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"rmdir /s /q \"{remoteDirectoryPath}\"", cancellationToken);
            }

            if (result.ExitCode != 0)
            {
                throw new SshException(
                    $"Directory deletion request failed for '{remoteDirectoryPath}'." +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardOutput}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardError}".Trim());
            }
        }

        /// <summary>
        /// Deletes a file on the remote system.
        /// </summary>
        /// <param name="remoteFilePath">The path on the remote system for the file to delete.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task DeleteFileOnRemoteSystemAsync(string remoteFilePath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            ProcessDetails result = null;
            if (targetPlatform == PlatformID.Unix)
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"rm -f \"{remoteFilePath}\"", cancellationToken);
            }
            else
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"del /F /Q \"{remoteFilePath}\"", cancellationToken);
            }

            if (result.ExitCode != 0)
            {
                throw new SshException(
                    $"File deletion request failed for '{remoteFilePath}'." +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardOutput}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardError}".Trim());
            }
        }

        /// <summary>
        /// Disposes of resources used by the proxy.
        /// </summary>
        /// <param name="disposing">True to dispose of unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.SessionClient.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Executes the SSH command.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <param name="outputReceived">An action/delegate to invoke as output is received from the SSH session command stream.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage", "AsyncFixer04:Fire-and-forget async call inside a using block", Justification = "Intended usage")]
        protected virtual async Task<ProcessDetails> ExecuteCommandOnRemoteSystemAsync(string command, CancellationToken cancellationToken, Action<SshCommandOutputInfo> outputReceived = null)
        {
            using (SshCommand sshCommand = this.SessionClient.CreateCommand(command))
            {
                Task execution = sshCommand.ExecuteAsync(cancellationToken);
                StringBuilder standardOutput = new StringBuilder();
                DateTime startTime = DateTime.Now;

                using (var reader = new StreamReader(sshCommand.OutputStream))
                {
                    while (!execution.IsCompleted)
                    {
                        while (!reader.EndOfStream)
                        {
                            try
                            {
                                string line = await reader.ReadLineAsync();
                                standardOutput.AppendLine(line);

                                if (outputReceived != null)
                                {
                                    outputReceived.Invoke(new SshCommandOutputInfo(line, DateTime.Now - startTime));
                                }
                            }
                            catch
                            {
                                // Best case effort.
                            }
                            finally
                            {
                                await Task.Delay(100);
                            }
                        }
                    }

                    return new ProcessDetails
                    {
                        Id = $"{this.SessionClient.ConnectionInfo.Username}{this.SessionClient.ConnectionInfo.Host},{sshCommand.CommandText}".GetHashCode(),
                        CommandLine = sshCommand.CommandText,
                        ExitCode = sshCommand.ExitStatus ?? 0,
                        StandardOutput = standardOutput.ToString(),
                        StandardError = sshCommand.Error,
                        ToolName = "SSH"
                    };
                }
            }
        }

        /// <summary>
        /// Returns true/false whether the path (directory or file) exists on the remote system.
        /// </summary>
        /// <param name="remotePath">A directory or file path on the remote system.</param>
        /// <param name="targetPlatform">The target system platform (e.g. Linux, Windows).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>True if the directory or file exists, false if not.</returns>
        protected virtual async Task<bool> ExistsOnRemoteSystemAsync(string remotePath, PlatformID targetPlatform, CancellationToken cancellationToken)
        {
            bool exists = true;
            ProcessDetails result = null;
            if (targetPlatform == PlatformID.Unix)
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"ls \"{remotePath}\"", cancellationToken);
            }
            else
            {
                result = await this.ExecuteCommandOnRemoteSystemAsync($"dir \"{remotePath}\"", cancellationToken);
            }

            if (SshClientProxy.FileDoesNotExistExpression.IsMatch($"{result.StandardError}"))
            {
                exists = false;
            }
            else if (result.ExitCode != 0)
            {
                throw new SshException(
                    $"Directory or file existence request failed for '{remotePath}'." +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardOutput}" +
                    $"{Environment.NewLine}{Environment.NewLine}" +
                    $"{result.StandardError}".Trim());
            }

            return exists;
        }

        /// <summary>
        /// Validates that the SSH session is connected.
        /// </summary>
        /// <exception cref="SshConnectionException">The SSH session is NOT connected.</exception>
        protected virtual void ValidateSessionConnection()
        {
            if (!this.SessionClient.IsConnected)
            {
                throw new SshConnectionException($"SSH client not connected.");
            }
        }
    }
}