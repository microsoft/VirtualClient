// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can
    /// be used to log events/messages to an Application Insights endpoint.
    /// </summary>
    [LoggerSpecialization(Name = SpecializationConstant.Telemetry)]
    public sealed class EventHubTelemetryLoggerProvider : ILoggerProvider
    {
        private EventHubTelemetryChannel telemetryChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubTelemetryLoggerProvider"/> class.
        /// </summary>
        /// <param name="channel">
        /// The telemetry channel used to handle buffering events for transmission to the Azure Event Hub.
        /// </param>
        public EventHubTelemetryLoggerProvider(EventHubTelemetryChannel channel)
        {
            channel.ThrowIfNull(nameof(channel));
            this.telemetryChannel = channel;
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
            return new EventHubTelemetryLogger(this.telemetryChannel);
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