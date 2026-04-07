// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Renci.SshNet;

    /// <summary>
    /// Provides methods for creating SSH and SCP clients.
    /// </summary>
    public class SshClientFactory : ISshClientFactory
    {
        /// <summary>
        /// Creates an SSH client.
        /// </summary>
        /// <param name="connection">Defines the remote host connection and authentication information.</param>
        public ISshClientProxy CreateClient(ConnectionInfo connection)
        {
            return new SshClientProxy(connection);
        }

        /// <summary>
        /// Creates an SSH client.
        /// </summary>
        /// <param name="host">The remote host name or IP address.</param>
        /// <param name="userName">The user name/account to use for authentication on the remote host.</param>
        /// <param name="password">The password to use for authentication on the remote host.</param>
        /// <param name="port">The port on which SSH is listening. Default = 22.</param>
        public ISshClientProxy CreateClient(string host, string userName, string password, int port = 22)
        {
            return new SshClientProxy(new PasswordConnectionInfo(host, port, userName, password));
        }
    }
}