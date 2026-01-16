// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can
    /// be used to log events/messages to an Application Insights endpoint.
    /// </summary>
    [LoggerSpecialization(Name = SpecializationConstant.Telemetry)]
    public sealed class EventHubTelemetryLoggerProvider : ILoggerProvider
    {
        private EventHubTelemetryChannel telemetryChannel;
        private LogLevel minimumLogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubTelemetryLoggerProvider"/> class.
        /// </summary>
        /// <param name="channel">
        /// The telemetry channel used to handle buffering events for transmission to the Azure Event Hub.
        /// </param>
        /// <param name="level">The minimum logging severity level.</param>
        public EventHubTelemetryLoggerProvider(EventHubTelemetryChannel channel, LogLevel level)
        {
            channel.ThrowIfNull(nameof(channel));
            this.telemetryChannel = channel;
            this.minimumLogLevel = level;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance that can be used to log events/messages
        /// to an Azure Event Hub endpoint.
        /// </summary>
        /// <param name="categoryName">The logger events category.</param>
        /// <returns>
        /// An <see cref="ILogger"/> instance that can log events/messages to an Azure Event Hub
        /// endpoint.
        /// </returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new EventHubTelemetryLogger(this.telemetryChannel, this.minimumLogLevel);
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}