// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Serilog.Events;
    using Serilog.Formatting;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Formats Serilog framework log/trace messages as JSON text.
    /// </summary>
    public class SerilogJsonTextFormatter : ITextFormatter
    {
        internal static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None,

            // By default, ALL properties in the JSON structure will be camel-cased including
            // dictionary keys.
            ContractResolver = new CamelCasePropertyNamesContractResolver(),

            // We will be serializing IEnumerable<KeyValuePair<string, object>> instances
            Converters = new List<JsonConverter> { new ContextPropertiesJsonConverter() }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogJsonTextFormatter"/> class.
        /// </summary>
        /// <param name="excludeProperties">A set of JSON properties to exclude from final output.</param>
        public SerilogJsonTextFormatter(IEnumerable<string> excludeProperties = null)
            : base()
        {
            if (excludeProperties?.Any() == true)
            {
                this.ExcludeProperties = new List<string>(excludeProperties);
            }
        }

        /// <summary>
        /// A list of properties to exclude from output.
        /// </summary>
        public List<string> ExcludeProperties { get; }

        /// <summary>
        /// Formats the log information as a JSON-structured object.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent != null)
            {
                // Ensure that we can represent the timestamp in universal, round-trip format. VC libraries
                // always emit the timestamps in UTC form but that designation may not be passed onward by
                // the Serilog framework.
                DateTime timestamp = new DateTime(logEvent.Timestamp.Ticks, DateTimeKind.Utc);

                // Note:
                // There is a bug in the Newtonsoft libraries for serializing JObject instances
                // with camel-casing. We are using a dictionary as a reasonable workaround.
                IDictionary<string, object> outputProperties = new Dictionary<string, object>();

                foreach (var property in logEvent.Properties)
                {
                    if (this.ExcludeProperties == null || !this.ExcludeProperties.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        try
                        {
                            SerilogJsonTextFormatter.AddProperties(outputProperties, property.Key, property.Value);
                        }
                        catch
                        {
                            // Best effort.
                        }
                    }
                }
                
                output.WriteLine(outputProperties.OrderBy(entry => entry.Key).ToJson(SerilogJsonTextFormatter.SerializationSettings));
                output.WriteLine("---");
            }
        }

        private static void AddProperties(IDictionary<string, object> properties, string propertyName, LogEventPropertyValue propertyValue)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    if (propertyValue == null)
                    {
                        properties.Add(propertyName, null);
                    }
                    else if (propertyValue is ScalarValue)
                    {
                        ScalarValue scalarValue = propertyValue as ScalarValue;
                        if (scalarValue.Value == null)
                        {
                            properties.Add(propertyName, null);
                        }
                        else
                        {
                            properties.Add(propertyName, scalarValue.Value);
                        }
                    }
                    else if (propertyValue is DictionaryValue)
                    {
                        IDictionary<string, object> nestedProperties = new Dictionary<string, object>();

                        foreach (var entry in (propertyValue as DictionaryValue).Elements)
                        {
                            SerilogJsonTextFormatter.AddProperties(nestedProperties, entry.Key?.Value?.ToString(), entry.Value);
                        }

                        properties.Add(propertyName, nestedProperties);
                    }
                }
            }
            catch
            {
                // Best Effort
            }
        }

        private class SdkDateTimeConverter : JsonConverter
        {
            private static readonly Type DateTimeType = typeof(DateTime);
            private static readonly Type DateTimeOffsetType = typeof(DateTimeOffset);

            public override bool CanConvert(Type objectType)
            {
                return objectType == SdkDateTimeConverter.DateTimeType || objectType == SdkDateTimeConverter.DateTimeOffsetType;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null)
                {
                    if (value.GetType() == SdkDateTimeConverter.DateTimeOffsetType)
                    {
                        serializer.Serialize(writer, ((DateTimeOffset)value).DateTime.ToString("o"));
                    }
                }
            }
        }
    }
}
