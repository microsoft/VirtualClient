// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using Renci.SshNet;
    using VirtualClient.Common;

    /// <summary>
    /// A mock/test Ssh Client manager.
    /// </summary>
    public class InMemorySshClientManager : ISshClientManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshClientManager"/> class.
        /// </summary>
        public InMemorySshClientManager()
        {
            this.SshClients = new List<ISshClientProxy>();
            this.SftpClients = new List<ISftpClientProxy>();
        }

        /// <summary>
        /// The set of ssh clients created by Ssh Client Manager.
        /// </summary>
        public IEnumerable<ISshClientProxy> SshClients { get; }

        /// <summary>
        /// The set of ssh clients created by Sftp Client Manager.
        /// </summary>
        public IEnumerable<ISftpClientProxy> SftpClients { get; }

        /// <summary>
        /// Delegate allows user to control the <see cref="ISshClientProxy"/> that is provided
        /// to the test.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> host - The command to execute.</item>
        /// <item><see cref="string"/> userName - The arguments to pass to the command on the command line.</item>
        /// <item><see cref="string"/> password - The working directory for the command execution.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, string, string, ISshClientProxy> OnCreateSshClient { get; set; }

        /// <summary>
        /// Delegate allows user to control the <see cref="ISftpClientProxy"/> that is provided
        /// to the test.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> host - The command to execute.</item>
        /// <item><see cref="string"/> userName - The arguments to pass to the command on the command line.</item>
        /// <item><see cref="string"/> password - The working directory for the command execution.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, string, string, ISftpClientProxy> OnCreateSftpClient { get; set; }

        /// <inheritdoc />
        public ISshClientProxy CreateSshClient(string host, string userName, string password)
        {
            ISshClientProxy sshClient = null;
            if (this.OnCreateSshClient != null)
            {
                sshClient = this.OnCreateSshClient?.Invoke(host, userName, password);
            }
            else
            {
                ConnectionInfo connectionInfo = new PasswordConnectionInfo(host, 22, userName, password);
                sshClient = new InMemorySshClient
                {
                    ConnectionInfo = connectionInfo
                };
            }

            (this.SshClients as List<ISshClientProxy>).Add(sshClient);
            return sshClient;
        }

        /// <inheritdoc />
        public ISftpClientProxy CreateSftpClient(string host, string userName, string password)
        {
            ISftpClientProxy sftpClient = null;
            if (this.OnCreateSftpClient != null)
            {
                sftpClient = this.OnCreateSftpClient?.Invoke(host, userName, password);
            }
            else
            {
                ConnectionInfo connectionInfo = new PasswordConnectionInfo(host, 22, userName, password);
                sftpClient = new InMemorySftpClient
                {
                    ConnectionInfo = connectionInfo
                };
            }

            (this.SftpClients as List<ISftpClientProxy>).Add(sftpClient);
            return sftpClient;
        }
    }
}