// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using global::Azure.Messaging.EventHubs;

    /// <summary>
    /// Represents event arguments associated with Event Hub
    /// telemetry channel operations.
    /// </summary>
    public class EventHubChannelEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubChannelEventArgs"/> class.
        /// </summary>
        public EventHubChannelEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubChannelEventArgs"/> class.
        /// </summary>
        /// <param name="events">Telemetry events associated with the operation.</param>
        public EventHubChannelEventArgs(IEnumerable<EventData> events)
        {
            this.Events = events;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubChannelEventArgs"/> class.
        /// </summary>
        /// <param name="events">Telemetry events associated with the operation.</param>
        /// <param name="error">An error associated with the operation (or failure of the operation).</param>
        public EventHubChannelEventArgs(IEnumerable<EventData> events, Exception error)
        {
            this.Events = events;
            this.Error = error;
        }

        /// <summary>
        /// Gets the telemetry events associated with the operation.
        /// </summary>
        public IEnumerable<EventData> Events { get; }

        /// <summary>
        /// Gets any error associated with the operation (or failure thereof).
        /// </summary>
        public Exception Error { get; }
    }
}