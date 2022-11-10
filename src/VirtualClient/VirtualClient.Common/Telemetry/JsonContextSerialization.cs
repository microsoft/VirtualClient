// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Provides a method for serializing event context objects into a
    /// JSON-formatted string.
    /// </summary>
    public static class JsonContextSerialization
    {
        private static JsonSerializerSettings defaultJsonSerializerSettings = JsonContextSerialization.InitializeJsonContextPropertySerialization();
        private static JsonSerializerSettings jsonSerializerSettings;

        /// <summary>
        /// Gets or sets the default JSON serializer settings.
        /// </summary>
        public static JsonSerializerSettings SerializerSettings
        {
            get
            {
                return JsonContextSerialization.jsonSerializerSettings ?? JsonContextSerialization.defaultJsonSerializerSettings;
            }

            set
            {
                JsonContextSerialization.jsonSerializerSettings = value;
            }
        }

        /// <summary>
        /// Serializes the context properties into JSON string/text.
        /// </summary>
        /// <param name="context">The context object to serialize.</param>
        /// <param name="maxSizeInChars">
        /// The maximum size of the serialized JSON (in bytes).  Note that ETW events have a maximum size and thus the
        /// context property JSON may need to be trimmed to fit within that size constraint.
        /// </param>
        /// <returns>
        /// A JSON-formatted string representing the serialized context properties.
        /// </returns>
        public static string Serialize(object context, int maxSizeInChars = -1)
        {
            string contextPropertyJson = JsonConvert.SerializeObject(context, JsonContextSerialization.SerializerSettings);

            if (maxSizeInChars >= 0
                && contextPropertyJson != null
                && contextPropertyJson.Length > maxSizeInChars)
            {
                contextPropertyJson = JsonPruner.Prune(contextPropertyJson, maxSizeInChars);
            }

            return contextPropertyJson;
        }

        private static JsonSerializerSettings InitializeJsonContextPropertySerialization()
        {
            return new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                FloatParseHandling = FloatParseHandling.Decimal,
                Converters = new List<JsonConverter>
                {
                    new ContextPropertiesJsonConverter()
                }
            };
        }
    }
}