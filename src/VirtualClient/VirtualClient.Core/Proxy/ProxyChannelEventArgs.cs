// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// Represents event arguments associated with Event Hub
    /// telemetry channel operations.
    /// </summary>
    public class ProxyChannelEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyChannelEventArgs(object context = null)
        {
            this.Context = context;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="message">Telemetry message associated with the operation.</param>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyChannelEventArgs(ProxyTelemetryMessage message, object context = null)
            : this(context)
        {
            if (message != null)
            {
                this.Messages = new List<ProxyTelemetryMessage> { message };
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="messages">Telemetry message(s) associated with the operation.</param>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyChannelEventArgs(IEnumerable<ProxyTelemetryMessage> messages, object context = null)
            : this(context)
        {
            if (messages?.Any() == true)
            {
                this.Messages = new List<ProxyTelemetryMessage>(messages);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="messages">Telemetry message(s) associated with the operation.</param>
        /// <param name="error">An error associated with the operation (or failure of the operation).</param>
        /// <param name="context">Provides context information related to the message operations.</param>
        public ProxyChannelEventArgs(IEnumerable<ProxyTelemetryMessage> messages, Exception error, object context = null)
            : this(messages, context)
        {
            this.Error = error;
        }

        /// <summary>
        /// Provides context information related to the message operations.
        /// </summary>
        public object Context { get; }

        /// <summary>
        /// Gets the telemetry messages associated with the operation.
        /// </summary>
        public IEnumerable<ProxyTelemetryMessage> Messages { get; }

        /// <summary>
        /// Gets any error associated with the operation (or failure thereof).
        /// </summary>
        public Exception Error { get; }
    }
}
