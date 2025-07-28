// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
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

        /// <summary>
        /// Delegate defines logic to execute when the 'CreateClient' method is called.
        /// </summary>
        public Func<ConnectionInfo, ISshClientProxy> OnCreateClient { get; set; }

        /// <inheritdoc />
        public ISshClientProxy CreateClient(ConnectionInfo connection)
        {
            ISshClientProxy sshClient = this.OnCreateClient?.Invoke(connection) ?? new InMemorySshClient(connection);
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