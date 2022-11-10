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
    public class ProxyChannelEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        public ProxyChannelEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="message">Telemetry message associated with the operation.</param>
        public ProxyChannelEventArgs(ProxyTelemetryMessage message)
        {
            this.Message = message;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyChannelEventArgs"/> class.
        /// </summary>
        /// <param name="message">Telemetry message associated with the operation.</param>
        /// <param name="error">An error associated with the operation (or failure of the operation).</param>
        public ProxyChannelEventArgs(ProxyTelemetryMessage message, Exception error)
        {
            this.Message = message;
            this.Error = error;
        }

        /// <summary>
        /// Gets the telemetry messages associated with the operation.
        /// </summary>
        public ProxyTelemetryMessage Message { get; }

        /// <summary>
        /// Gets any error associated with the operation (or failure thereof).
        /// </summary>
        public Exception Error { get; }
    }
}
