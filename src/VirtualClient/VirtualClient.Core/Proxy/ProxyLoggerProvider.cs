// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// An implementation of the <see cref="ILoggerProvider"/> interface for logging via
    /// a Proxy API endpoint.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Nothing to dispose.")]
    public class ProxyLoggerProvider : ILoggerProvider
    {
        private ProxyTelemetryChannel telemetryChannel;
        private string source;
        private TimeSpan flushTimeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLoggerProvider"/> class.
        /// </summary>
        /// <param name="channel">The background channel used to upload logged telemetry messages through a proxy endpoint.</param>
        /// <param name="source">The source to use when uploading telemetry through the proxy API.</param>
        /// <param name="flushTimeout">A timeout to apply to flush operations.</param>
        public ProxyLoggerProvider(ProxyTelemetryChannel channel, string source = null, TimeSpan? flushTimeout = null)
        {
            channel.ThrowIfNull(nameof(channel));
            this.telemetryChannel = channel;
            this.source = source;
            this.flushTimeout = flushTimeout ?? TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Creates a new <see cref="ProxyLogger"/> that uploads telemetry through a proxy API endpoint.
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            this.telemetryChannel.BeginMessageTransmission();
            ProxyLogger logger = new ProxyLogger(this.telemetryChannel, this.source);

            VirtualClientRuntime.CleanupTasks.Add(new Action_(() =>
            {
                this.telemetryChannel.Flush(this.flushTimeout);
                this.telemetryChannel.Dispose();
            }));

            return logger;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
        }
    }
}