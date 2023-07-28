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
    public class InMemorySshClientManager : SshClientManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshClientManager"/> class.
        /// </summary>
        public InMemorySshClientManager()
        {
            this.SshClients = new List<ISshClientProxy>();
        }

        /// <summary>
        /// The set of ssh clients created by Ssh Client Manager.
        /// </summary>
        public IEnumerable<ISshClientProxy> SshClients { get; }

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

        /// <inheritdoc />
        public override ISshClientProxy CreateSshClient(string host, string userName, string password)
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
    }
}