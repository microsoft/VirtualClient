// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using Renci.SshNet;

    /// <summary>
    /// Provides methods for creating and managing ssh client.
    /// </summary>
    public class SshClientManager : ISshClientManager
    {
        /// <summary>
        /// Creates a ssh on the system.
        /// </summary>
        /// <param name="host">Host name.</param>
        /// <param name="userName">Host's username.</param>
        /// <param name="password">Host's password.</param>
        public virtual ISshClientProxy CreateSshClient(string host, string userName, string password)
        {
            SshClient sshClient = new SshClient(host, userName, password);
            ISshClientProxy proxy = new SshClientProxy(sshClient);
            return proxy;
        }

        /// <summary>
        /// Creates a sftp client on the system.
        /// </summary>
        /// <param name="host">Host name.</param>
        /// <param name="userName">Host's username.</param>
        /// <param name="password">Host's password.</param>
        public virtual ISftpClientProxy CreateSftpClient(string host, string userName, string password)
        {
            SftpClient sftpClient = new SftpClient(host, userName, password);
            ISftpClientProxy proxy = new SftpClientProxy(sftpClient);
            return proxy;
        }
    }
}