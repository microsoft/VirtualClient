// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using Renci.SshNet;

    /// <summary>
    /// Proxy for a Ssh Client.
    /// </summary>
    public interface ISshClientProxy : IDisposable
    {
        /// <summary>
        /// Remote connection information of the SshClient.
        /// </summary>
        ConnectionInfo ConnectionInfo { get; }

        /// <summary>
        /// Creates the command to be executed.
        /// </summary>
        /// <param name="commandText">The command text.</param>
        /// <returns>A Ssh Command object.</returns>
        ISshCommandProxy CreateCommand(string commandText);

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
