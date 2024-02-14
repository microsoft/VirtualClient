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

        private ProcessDetails processDetails;

        /// <summary>
        /// Initializes a new instance of the <see cref="SshCommandProxy"/> class.
        /// </summary>
        /// <param name="sshCommand">The SSH command associated with the proxy.</param>
        public SshCommandProxy(SshCommand sshCommand)
        {
            sshCommand.ThrowIfNull(nameof(sshCommand));
            this.processDetails = new ProcessDetails();
            this.processDetails.GeneratedResults = new List<string>();
            this.UnderlyingSshCommand = sshCommand;
        }

        /// <inheritdoc />
        public virtual string Error => this.UnderlyingSshCommand.Error;

        /// <inheritdoc />
        public virtual int ExitStatus => this.UnderlyingSshCommand.ExitStatus;

        /// <inheritdoc />
        public virtual string Result => this.UnderlyingSshCommand.Result;

        /// <inheritdoc />
        public virtual string CommandText => this.UnderlyingSshCommand.CommandText;

        /// <inheritdoc />
        public ProcessDetails ProcessDetails
        {
            get
            {
                this.processDetails.CommandLine = SensitiveData.ObscureSecrets($"{this.CommandText}".Trim());
                this.processDetails.ExitCode = this.ExitStatus;
                this.processDetails.StandardError = this.Error;
                this.processDetails.StandardOutput = this.Result;

                return this.processDetails;
            }
        }

        /// <summary>
        /// Gets the underlying ssh command itself.
        /// </summary>
        protected SshCommand UnderlyingSshCommand { get; }

        /// <inheritdoc />
        public virtual string Execute()
        {
            return this.UnderlyingSshCommand.Execute();
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
                    this.UnderlyingSshCommand.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}