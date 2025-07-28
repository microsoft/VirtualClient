// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Renci.SshNet;
    using Renci.SshNet.Common;
    using VirtualClient.Common;

    /// <summary>
    /// Represents a mock/fake SSH client.
    /// </summary>
    public class InMemorySshClient : ISshClientProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshClient"/>
        /// </summary>
        public InMemorySshClient(ConnectionInfo connection)
        {
            this.ConnectionInfo = connection;
            this.CommandsExecuted = new List<string>();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public event EventHandler<ScpUploadEventArgs> CopyingTo;

        /// <summary>
        /// Not Implemented
        /// </summary>
        public event EventHandler<ScpDownloadEventArgs> CopyingFrom;

        /// <summary>
        /// The set of commands executed through the SSH client.
        /// </summary>
        public IEnumerable<string> CommandsExecuted { get; }

        /// <inheritdoc />
        public ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'ConnectAsync' method is called.
        /// </summary>
        public Action OnConnect { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'CopyFromAsync' method is called.
        /// </summary>
        public Action<string, Stream, IFileInfo, IDirectoryInfo> OnCopyFrom { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'CopyToAsync' method is called.
        /// </summary>
        public Action<Stream, IFileInfo, IDirectoryInfo, string> OnCopyTo { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'CreateDirectoryAsync' method is called.
        /// </summary>
        public Action<string, bool> OnCreateDirectory { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'DeleteDirectoryAsync' method is called.
        /// </summary>
        public Action<string> OnDeleteDirectory { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'DeleteFileAsync' method is called.
        /// </summary>
        public Action<string> OnDeleteFile { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'Disconnect' method is called.
        /// </summary>
        public Action OnDisconnect { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'ExecuteCommandAsync' method is called.
        /// </summary>
        public Func<string, ProcessDetails> OnExecuteCommand { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'ExistsAsync' method is called.
        /// </summary>
        public Func<string, bool> OnExists { get; set; }

        /// <summary>
        /// Delegate defines logic to execute when the 'GetTargetPlatformArchitectureAsync' method is called.
        /// </summary>
        public Func<Tuple<PlatformID, Architecture>> OnGetTargetPlatformArchitecture { get; set; }

        /// <inheritdoc />
        public Task ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.OnConnect?.Invoke();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyFromAsync(string remoteFilePath, Stream destination, CancellationToken cancellationToken = default)
        {
            this.CopyingFrom?.Invoke(this, new ScpDownloadEventArgs(Path.GetFileName(remoteFilePath), 12345, 12345));
            this.OnCopyFrom?.Invoke(remoteFilePath, destination, null, null);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyFromAsync(string remoteFilePath, IFileInfo destination, CancellationToken cancellationToken = default)
        {
            this.CopyingFrom?.Invoke(this, new ScpDownloadEventArgs(Path.GetFileName(remoteFilePath), 12345, 12345));
            this.OnCopyFrom?.Invoke(remoteFilePath, null, destination, null);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyFromAsync(string remoteDirectoryPath, IDirectoryInfo destination, CancellationToken cancellationToken = default)
        {
            this.CopyingFrom?.Invoke(this, new ScpDownloadEventArgs($"{Path.GetFileName(remoteDirectoryPath)}.dll", 12345, 12345));
            this.OnCopyFrom?.Invoke(remoteDirectoryPath, null, null, destination);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyToAsync(Stream source, string remoteFilePath, CancellationToken cancellationToken = default)
        {
            this.CopyingTo?.Invoke(this, new ScpUploadEventArgs(Path.GetFileName(remoteFilePath), 12345, 12345));
            this.OnCopyTo?.Invoke(source, null, null, remoteFilePath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyToAsync(IFileInfo source, string remoteFilePath, CancellationToken cancellationToken = default)
        {
            this.CopyingTo?.Invoke(this, new ScpUploadEventArgs(Path.GetFileName(source.FullName), 12345, 12345));
            this.OnCopyTo?.Invoke(null, source, null, remoteFilePath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CopyToAsync(IDirectoryInfo source, string remoteDirectoryPath, CancellationToken cancellationToken = default)
        {
            this.CopyingTo?.Invoke(this, new ScpUploadEventArgs($"{Path.GetFileName(source.FullName)}.dll", 12345, 12345));
            this.OnCopyTo?.Invoke(null, null, source, remoteDirectoryPath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task CreateDirectoryAsync(string remoteDirectoryPath, bool force = false, CancellationToken cancellationToken = default)
        {
            this.OnCreateDirectory?.Invoke(remoteDirectoryPath, force);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default)
        {
            this.OnDeleteDirectory?.Invoke(remoteDirectoryPath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default)
        {
            this.OnDeleteFile?.Invoke(remoteFilePath);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            this.OnDisconnect?.Invoke();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public Task<ProcessDetails> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default, Action<SshCommandOutputInfo> outputReceived = null)
        {
            (this.CommandsExecuted as List<string>).Add(command);
            outputReceived?.Invoke(new SshCommandOutputInfo("Command output", TimeSpan.FromSeconds(20)));

            return Task.FromResult(this.OnExecuteCommand?.Invoke(command) ?? new ProcessDetails
            {
                Id = command.GetHashCode(),
                CommandLine = command,
                ExitCode = 0,
                StandardOutput = $"Command '{command}' executed successfully",
                ToolName = "SSH"
            });
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.OnExists?.Invoke(remotePath) ?? false);
        }

        public Task<Tuple<PlatformID, Architecture>> GetTargetPlatformArchitectureAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(this.OnGetTargetPlatformArchitecture?.Invoke()
               ?? new Tuple<PlatformID, Architecture>(PlatformID.Unix, Architecture.X64));
        }
    }
}