// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using Renci.SshNet;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Acts as a proxy for a SSH Command.
    /// </summary>
    public class SshCommandProxy : ISshCommandProxy
    {
        /// <summary>
        /// The list of exit codes that indicate success by default.
        /// </summary>
        public static readonly IEnumerable<int> DefaultSuccessCodes = new int[] { 0 };

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SshCommandProxy"/> class.
        /// </summary>
        /// <param name="sshCommand">The SSH command associated with the proxy.</param>
        public SshCommandProxy(SshCommand sshCommand)
        {
            sshCommand.ThrowIfNull(nameof(sshCommand));
            this.UnderlyingSSHCommand = sshCommand;
        }

        /// <inheritdoc />
        public virtual string Error => this.UnderlyingSSHCommand.Error;

        /// <inheritdoc />
        public virtual int ExitStatus => this.UnderlyingSSHCommand.ExitStatus;

        /// <inheritdoc />
        public virtual string Result => this.UnderlyingSSHCommand.Result;

        /// <inheritdoc />
        public virtual string CommandText => this.UnderlyingSSHCommand.CommandText;

        /// <summary>
        /// Gets the underlying ssh command itself.
        /// </summary>
        protected SshCommand UnderlyingSSHCommand { get; }

        /// <inheritdoc />
        public virtual string Execute()
        {
            return this.UnderlyingSSHCommand.Execute();
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
                    this.UnderlyingSSHCommand.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}
