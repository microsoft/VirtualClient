// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Provides descriptive information for a system event data point.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "This is a pure data contract object.")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "This is a pure data contract object.")]
    public class EventDataPoint : TelemetryDataPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataPoint"/> class.
        /// </summary>
        public EventDataPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventDataPoint"/> class.
        /// </summary>
        public EventDataPoint(TelemetryDataPoint dataPoint)
            : base(dataPoint)
        {
        }

        /// <summary>
        /// A numeric code for the event.
        /// </summary>
        [JsonProperty("eventCode", Required = Required.Default)]
        [YamlMember(Alias = "eventCode", ScalarStyle = ScalarStyle.Plain)]
        public long EventCode;

        /// <summary>
        /// A description of the event.
        /// </summary>
        [JsonProperty("eventDescription", Required = Required.Default)]
        [YamlMember(Alias = "eventDescription", ScalarStyle = ScalarStyle.Plain)]
        public string EventDescription;

        /// <summary>
        /// An identifier for the event (e.g. eventlog.journalctl).
        /// </summary>
        [JsonProperty("eventId", Required = Required.Default)]
        [YamlMember(Alias = "eventId", ScalarStyle = ScalarStyle.Plain)]
        public string EventId;

        /// <summary>
        /// The source of the event (e.g. journalctl).
        /// </summary>
        [JsonProperty("eventSource", Required = Required.Default)]
        [YamlMember(Alias = "eventSource", ScalarStyle = ScalarStyle.Plain)]
        public string EventSource;

        /// <summary>
        /// The type of the event (e.g. EventLog).
        /// </summary>
        [JsonProperty("eventType", Required = Required.Default)]
        [YamlMember(Alias = "eventType", ScalarStyle = ScalarStyle.Plain)]
        public string EventType;

        /// <summary>
        /// A set of properties describing the details/information for the event.
        /// </summary>
        [JsonProperty("eventInfo", Required = Required.Default)]
        [YamlMember(Alias = "eventInfo", ScalarStyle = ScalarStyle.Plain)]
        public SortedMetadataDictionary EventInfo;

        /// <inheritdoc />
        protected override IList<string> GetValidationErrors()
        {
            IList<string> validationErrors = base.GetValidationErrors() ?? new List<string>();

            // Part C requirements
            if (string.IsNullOrWhiteSpace(this.EventId))
            {
                validationErrors.Add("The event ID is required (eventId).");
            }

            if (string.IsNullOrWhiteSpace(this.EventSource))
            {
                validationErrors.Add("The event source is required (eventSource).");
            }

            if (string.IsNullOrWhiteSpace(this.EventType))
            {
                validationErrors.Add("The event type is required (eventType).");
            }

            return validationErrors;
        }
    }
}
