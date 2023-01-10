// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// Represents event arguments associated with Event Hub
    /// telemetry channel operations.
    /// </summary>
    public class ProxyBlobEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBlobEventArgs"/> class.
        /// </summary>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyBlobEventArgs(object context = null)
        {
            this.Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBlobEventArgs"/> class.
        /// </summary>
        /// <param name="descriptor">The blob descriptor associated with the operation.</param>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyBlobEventArgs(ProxyBlobDescriptor descriptor, object context = null)
            : this(context)
        {
            this.Descriptor = descriptor;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBlobEventArgs"/> class.
        /// </summary>
        /// <param name="descriptor">The blob descriptor associated with the operation.</param>
        /// <param name="error">An error associated with the operation (or failure of the operation).</param>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyBlobEventArgs(ProxyBlobDescriptor descriptor, Exception error, object context = null)
            : this(descriptor, context)
        {
            this.Error = error;
        }

        /// <summary>
        /// Provides context information related to the message operations.
        /// </summary>
        public object Context { get; }

        /// <summary>
        /// Gets the blob descriptor associated with the operation.
        /// </summary>
        public ProxyBlobDescriptor Descriptor { get; }

        /// <summary>
        /// Gets any error associated with the operation (or failure thereof).
        /// </summary>
        public Exception Error { get; }
    }
}
