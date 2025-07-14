// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using Renci.SshNet;

    /// <summary>
    /// A mock/test SSH client factory.
    /// </summary>
    public class InMemorySshClientFactory : ISshClientFactory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshClientFactory"/> class.
        /// </summary>
        public InMemorySshClientFactory()
        {
            this.SshClients = new List<ISshClientProxy>();
        }

        /// <summary>
        /// The set of SSH clients created.
        /// </summary>
        public IEnumerable<ISshClientProxy> SshClients { get; }

        /// <inheritdoc />
        public ISshClientProxy CreateClient(ConnectionInfo connection)
        {
            ISshClientProxy sshClient = new InMemorySshClient(connection);
            (this.SshClients as List<ISshClientProxy>).Add(sshClient);

            return sshClient;
        }

        /// <inheritdoc />
        public ISshClientProxy CreateClient(string host, string userName, string password, int port = 22)
        {
            return this.CreateClient(new PasswordConnectionInfo(host, port, userName, password));
        }
    }
}