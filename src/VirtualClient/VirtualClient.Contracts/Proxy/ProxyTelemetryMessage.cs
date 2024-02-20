// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Represents a telemetry message that needs to be emitted to a remote/proxy
    /// endpoint.
    /// </summary>
    public class ProxyTelemetryMessage
    {
        /// <summary>
        /// The source of the telemetry event/message (e.g. VirtualClient).
        /// </summary>
        [JsonProperty(PropertyName = "source", Required = Required.Always, Order = 0)]
        public string Source { get; set; }

        /// <summary>
        /// The type of event (e.g. Events, Metrics, Traces). Default = Traces.
        /// </summary>
        [JsonProperty(PropertyName = "eventType", Required = Required.Always, Order = 1)]
        public string EventType { get; set; }

        /// <summary>
        /// The telemetry message or event name.
        /// </summary>
        [JsonProperty(PropertyName = "message", Required = Required.Always, Order = 2)]
        public string Message { get; set; }

        /// <summary>
        /// The severity level for the message or event (e.g. Informational, Error).
        /// </summary>
        [JsonProperty(PropertyName = "severityLevel", Required = Required.Always, Order = 3)]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel SeverityLevel { get; set; }

        /// <summary>
        /// The type of message or event (e.g. trace).
        /// </summary>
        [JsonProperty(PropertyName = "itemType", Required = Required.Always, Order = 4)]
        public string ItemType { get; set; }

        /// <summary>
        /// The correlation activity/operation ID for the event.
        /// </summary>
        [JsonProperty(PropertyName = "operationId", Required = Required.Always, Order = 5)]
        public string OperationId { get; set; }

        /// <summary>
        /// The parent correlation activity/operation ID for the message/event.
        /// </summary>
        [JsonProperty(PropertyName = "parentOperationId", Required = Required.Always, Order = 6)]
        public string OperationParentId { get; set; }

        /// <summary>
        /// The name of the application that is emitting the telemetry.
        /// </summary>
        [JsonProperty(PropertyName = "appName", Required = Required.Always, Order = 7)]
        public string AppName { get; set; }

        /// <summary>
        /// The name of the host on which the application emitting the telemetry
        /// is running.
        /// </summary>
        [JsonProperty(PropertyName = "appHost", Required = Required.Always, Order = 8)]
        public string AppHost { get; set; }

        /// <summary>
        /// The version of the telemetry SDK.
        /// </summary>
        [JsonProperty(PropertyName = "sdkVersion", Required = Required.Always, Order = 9)]
        public string SdkVersion { get; set; }

        /// <summary>
        /// Custom dimensions associated with the telemetry message/event that provide additional
        /// context into the scenario.
        /// </summary>
        [JsonProperty(PropertyName = "customDimensions", Required = Required.Default, Order = 20)]
        public IDictionary<string, object> CustomDimensions { get; set; }
    }
}
