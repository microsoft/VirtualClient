// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    internal class ProxyLoggerProvider : ILoggerProvider
    {
        private ProxyTelemetryChannel telemetryChannel;
        private string source;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLoggerProvider"/> class.
        /// </summary>
        /// <param name="channel">The background channel used to upload logged telemetry messages through a proxy endpoint.</param>
        /// <param name="source">The source to use when uploading telemetry through the proxy API.</param>
        public ProxyLoggerProvider(ProxyTelemetryChannel channel, string source = null)
        {
            channel.ThrowIfNull(nameof(channel));
            this.telemetryChannel = channel;
            this.source = source;
        }

        /// <summary>
        /// Creates a new <see cref="ProxyLogger"/> that uploads telemetry through a proxy API endpoint.
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            this.telemetryChannel.BeginMessageTransmission();
            return new ProxyLogger(this.telemetryChannel, this.source);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
        }
    }
}