// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Renci.SshNet;

    /// <summary>
    /// Provides methods for creating SSH clients.
    /// </summary>
    public interface ISshClientFactory
    {
        /// <summary>
        /// Creates an SSH client.
        /// </summary>
        /// <param name="connection">Defines the remote host connection and authentication information.</param>
        ISshClientProxy CreateClient(ConnectionInfo connection);

        /// <summary>
        /// Creates a ssh on the system.
        /// </summary>
        /// <param name="host">THost name.</param>
        /// <param name="userName">Host's username.</param>
        /// <param name="password">Host's password.</param>
        /// <param name="port">The port on which SSH is listening. Default = 22.</param>
        public ISshClientProxy CreateClient(string host, string userName, string password, int port = 22);
    }
}
