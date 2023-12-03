// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{    
    /// <summary>
    /// Provides methods for creating and managing ssh client.
    /// </summary>
    public interface ISshClientManager
    {
        /// <summary>
        /// Creates a ssh on the system.
        /// </summary>
        /// <param name="host">THost name.</param>
        /// <param name="userName">Host's username.</param>
        /// <param name="password">Host's password.</param>
        public ISshClientProxy CreateSshClient(string host, string userName, string password);

        /// <summary>
        /// Creates a sftp client on the system.
        /// </summary>
        /// <param name="host">Host name.</param>
        /// <param name="userName">Host's username.</param>
        /// <param name="password">Host's password.</param>
        public ISftpClientProxy CreateSftpClient(string host, string userName, string password);
    }
}
