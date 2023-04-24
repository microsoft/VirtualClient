// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using Renci.SshNet;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Acts as a proxy for a <see cref="SshClient"/> running on the local
    /// system.
    /// </summary>
    public class SshClientProxy : ISshClientProxy
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SshCommandProxy"/> class.
        /// </summary>
        /// <param name="sshClient">The SSH command associated with the proxy.</param>
        public SshClientProxy(SshClient sshClient)
        {
            sshClient.ThrowIfNull(nameof(sshClient));
            this.UnderlyingSSHClient = sshClient;
        }

        /// <inheritdoc />
        public virtual ConnectionInfo ConnectionInfo => this.UnderlyingSSHClient.ConnectionInfo;

        /// <summary>
        /// Gets the underlying ssh client itself.
        /// </summary>
        protected SshClient UnderlyingSSHClient { get; }

        /// <inheritdoc />
        public void Connect()
        {
            this.UnderlyingSSHClient.Connect();
        }

        /// <inheritdoc />
        public ISshCommandProxy CreateCommand(string commandText)
        {
            SshCommand sshCommand = this.UnderlyingSSHClient.CreateCommand(commandText);
            SshCommandProxy sshCommandProxy = new SshCommandProxy(sshCommand);
            return sshCommandProxy;
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            this.UnderlyingSSHClient.Disconnect();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources used by the proxy.
        /// </summary>
        /// <param name="disposing">True to dispose of unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.UnderlyingSSHClient.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
