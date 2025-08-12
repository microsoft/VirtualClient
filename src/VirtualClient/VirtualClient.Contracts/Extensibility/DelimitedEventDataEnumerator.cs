// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides an enumerator for delimited events data content.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Implemented sufficiently")]
    public class DelimitedEventDataEnumerator : DelimitedDataEnumerator<EventDataPoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DelimitedEventDataEnumerator"/> class.
        /// </summary>
        /// <param name="content">Content containing delimited events data points.</param>
        /// <param name="format">The format of the events data points (e.g. CSV, JSON, YAML).</param>
        public DelimitedEventDataEnumerator(string content, DataFormat format)
            : base(content, format)
        {
        }

        /// <inheritdoc />
        protected override EventDataPoint ParseFields(string[] columns, string[] values)
        {
            EventDataPoint eventData = new EventDataPoint();
            this.ApplyTo(eventData, columns, values);

            for (int i = 0; i < values.Length; i++)
            {
                string fieldName = columns[i];
                string fieldValue = values[i];

                switch (fieldName.ToLowerInvariant())
                {
                    case "eventcode":
                        eventData.EventCode = int.Parse(fieldValue);
                        break;

                    case "eventdescription":
                        eventData.EventDescription = fieldValue;
                        break;

                    case "eventid":
                        eventData.EventId = fieldValue;
                        break;

                    case "eventsource":
                        eventData.EventSource = fieldValue;
                        break;

                    case "eventtype":
                        eventData.EventType = fieldValue;
                        break;

                    case "eventinfo":
                        IDictionary<string, IConvertible> eventInfo = TextParsingExtensions.ParseDelimitedValues(fieldValue);
                        if (eventInfo?.Any() == true)
                        {
                            eventData.EventInfo = new SortedMetadataDictionary(eventInfo.ToDictionary(
                                entry => entry.Key, 
                                entry => entry.Value as object));
                        }

                        break;
                }
            }

            return eventData;
        }

        /// <inheritdoc />
        protected override EventDataPoint ParseJson(string json)
        {
            json.ThrowIfNullOrWhiteSpace(nameof(json));
            return json.FromJson<EventDataPoint>();
        }

        /// <inheritdoc />
        protected override EventDataPoint ParseYaml(string yaml)
        {
            yaml.ThrowIfNullOrWhiteSpace(nameof(yaml));
            return YamlDeserializer.Deserialize<EventDataPoint>(yaml);
        }
    }
}
