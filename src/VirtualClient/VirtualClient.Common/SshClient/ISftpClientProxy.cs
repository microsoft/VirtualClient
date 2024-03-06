// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.IO;
    using Renci.SshNet;

    /// <summary>
    /// Acts as a limited proxy to provide information about a SFTP Client.
    /// </summary>
    public interface ISftpClientProxy : IDisposable
    {
        /// <summary>
        /// Remote connection information of the SftpClient.
        /// </summary>
        ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Upload files from SSH connection using SFTP Client.
        /// </summary>
        /// <param name="input">Data input stream.</param>
        /// <param name="remotePath">Remote file path.</param>
        void UploadFile(Stream input, string remotePath);

        /// <summary>
        /// Download files from SSH connection using SFTP Client.
        /// </summary>
        /// <param name="remoteFilePath">File to download.</param>
        /// <param name="output"> Stream to write the file into.</param>
        void DownloadFile(string remoteFilePath, Stream output);

        /// <summary>
        /// Connects client to the server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Disconnects client from the server.
        /// </summary>
        void Disconnect();
    }
}
