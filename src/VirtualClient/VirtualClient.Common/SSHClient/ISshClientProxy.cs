// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public interface ISshClientProxy : IDisposable
    {
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
