// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Renci.SshNet;
    using Renci.SshNet.Common;
    using VirtualClient.Common;

    /// <summary>
    /// A proxy for an SSH client.
    /// </summary>
    public interface ISshClientProxy : IDisposable
    {
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

        /// <summary>
        /// Defines the target host connection information.
        /// </summary>
        ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Establishes a connection to target host.
        /// </summary>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies the content from a directory on the remote system to a directory on the local system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to copy to the local system.</param>
        /// <param name="destination">The directory to copy into on the local system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyFromAsync(string remoteDirectoryPath, IDirectoryInfo destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies the file on the remote system to a stream.
        /// </summary>
        /// <param name="remoteFilePath">The path on the remote system for the file to copy to the local system.</param>
        /// <param name="destination">The content stream to copy into.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyFromAsync(string remoteFilePath, Stream destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies the file on the remote system to a file on the local system.
        /// </summary>
        /// <param name="remoteFilePath">The path on the remote system for the file to copy to the local system.</param>
        /// <param name="destination">The file to copy into on the local system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyFromAsync(string remoteFilePath, IFileInfo destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies the stream contents to a file on the remote system.
        /// </summary>
        /// <param name="source">The content stream to copy to the remote system.</param>
        /// <param name="remoteFilePath">The path on the remote system for the file to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyToAsync(Stream source, string remoteFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a file on the local system to a file on the remote system.
        /// </summary>
        /// <param name="source">The file to copy to the remote system.</param>
        /// <param name="remoteFilePath">The path on the remote system for the file to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyToAsync(IFileInfo source, string remoteFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a directory on the local system to a directory on the remote system.
        /// </summary>
        /// <param name="source">The directory to copy to the remote system.</param>
        /// <param name="remoteDirectoryPath">The path on the remote system for the directory to which the content will be copied.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CopyToAsync(IDirectoryInfo source, string remoteDirectoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a directory on the remote system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path to the directory on the remote system to create.</param>
        /// <param name="force">True to force the creation of the folder even if it already exists on the remote system. The existing folder will be deleted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CreateDirectoryAsync(string remoteDirectoryPath, bool force = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a directory on the remote system.
        /// </summary>
        /// <param name="remoteDirectoryPath">The path to the directory on the remote system to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task DeleteDirectoryAsync(string remoteDirectoryPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file on the remote system.
        /// </summary>
        /// <param name="remoteFilePath">The path to the file on the remote system to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task DeleteFileAsync(string remoteFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from the target host.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Executes the command over the SSH session.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="outputReceived">An action/delegate to invoke as output is received from the SSH session command stream.</param>
        /// <returns>The results of the command execution.</returns>
        Task<ProcessDetails> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default, Action<SshCommandOutputInfo> outputReceived = null);

        /// <summary>
        /// Returns true/false whether the file or directory exists on the remote system.
        /// </summary>
        /// <param name="remotePath">A file or directory path on the remote system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>True if the file or directory exists on the remote system, false if not.</returns>
        Task<bool> ExistsAsync(string remotePath, CancellationToken cancellationToken = default);
    }
}