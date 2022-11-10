// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Configuration
{
    /// <summary>
    /// Represents the 'EventHubLogSettings' section of the appsetting.json file
    /// for the application.
    /// </summary>
    public class EventHubLogSettings
    {
        /// <summary>
        /// True/false whether Event Hub logging is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// The name of the Event Hub where performance counter telemetry
        /// is written.
        /// </summary>
        public string CountersHubName { get; set; }

        /// <summary>
        /// The name of the Event Hub where system event monitoring telemetry
        /// is written.
        /// </summary>
        public string EventsHubName { get; set; }

        /// <summary>
        /// The name of the Event Hub where workload metrics telemetry
        /// is written.
        /// </summary>
        public string MetricsHubName { get; set; }

        /// <summary>
        /// The name of the Event Hub where general logs/tracing telemetry
        /// is written.
        /// </summary>
        public string TracesHubName { get; set; }
    }
}
