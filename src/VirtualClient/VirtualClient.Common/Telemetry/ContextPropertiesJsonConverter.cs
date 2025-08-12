// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a JSON converter that can handle the serialization/deserialization of telemetry IDictionary object.
    /// </summary>
    public class ContextPropertiesJsonConverter : JsonConverter
    {
        private static Type supportedType = typeof(IEnumerable<KeyValuePair<string, object>>);
        private static Type serializerType = typeof(IDictionary<string, object>);

        /// <summary>
        /// Returns true/false whether the object type is supported for JSON serialization/deserialization.
        /// </summary>
        /// <param name="objectType">The type of object to serialize/deserialize.</param>
        /// <returns>
        /// True if the object is supported, false if not.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            bool canConvert = false;
            if (objectType != null && !objectType.IsPrimitive && objectType.IsGenericType)
            {
                canConvert = objectType == ContextPropertiesJsonConverter.supportedType
                    || ContextPropertiesJsonConverter.supportedType.IsAssignableFrom(objectType);
            }

            return canConvert;
        }

        /// <summary>
        /// Reads the JSON text from the reader and converts it into an <see cref="IEnumerable{T}"/>
        /// object instance.
        /// </summary>
        /// <param name="reader">Contains the JSON text defining the <see cref="IDictionary{String, IConvertible}"/> object.</param>
        /// <param name="objectType">The type of object (in practice this will only be an <see cref="IDictionary{String, IConvertible}"/> type).</param>
        /// <param name="existingValue">Unused.</param>
        /// <param name="serializer">Unused.</param>
        /// <returns>
        /// A deserialized <see cref="IEnumerable{T}"/> object converted from JSON text.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentException("The reader parameter is required.", nameof(reader));
            }

            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject providerJsonObject = JObject.Load(reader);
                ContextPropertiesJsonConverter.ReadDictionaryEntries(providerJsonObject, dictionary);
            }

            return dictionary;
        }

        /// <summary>
        /// Writes a <see cref="IEnumerable{T}"/> object to JSON text.
        /// </summary>
        /// <param name="writer">Handles the writing of the JSON text.</param>
        /// <param name="value">The <see cref="IEnumerable{T}"/> object to serialize to JSON text.</param>
        /// <param name="serializer">The JSON serializer handling the serialization to JSON text.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.ThrowIfNull(nameof(writer));
            serializer.ThrowIfNull(nameof(serializer));

            IEnumerable<KeyValuePair<string, object>> contextProperties = value as IEnumerable<KeyValuePair<string, object>>;

            if (contextProperties != null)
            {
                ContextPropertiesJsonConverter.WriteDictionaryEntries(writer, serializer, contextProperties);
            }
        }

        private static void ReadDictionaryEntries(JToken providerJsonObject, IDictionary<string, object> dictionary)
        {
            IEnumerable<JToken> children = providerJsonObject.Children();
            if (children.Any())
            {
                foreach (JToken child in children)
                {
                    if (child.Type == JTokenType.Property)
                    {
                        if (child.First != null)
                        {
                            JValue propertyValue = child.First as JValue;
                            object settingValue = propertyValue.Value;

                            // JSON properties that have periods (.) in them will have a path representation
                            // like this:  ['this.is.a.path'].  We have to account for that when adding the key
                            // to the dictionary. The key we want to add is 'this.is.a.path'
                            string key = child.Path;
                            if (key.IndexOf(".", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // ['this.is.a.path'] -> this.is.a.path
                                key = child.Path.Trim('[', '\'', ']');
                            }

                            dictionary.Add(key, settingValue);
                        }
                    }
                }
            }
        }

        private static void WriteDictionaryEntries(JsonWriter writer, JsonSerializer serializer, IEnumerable<KeyValuePair<string, object>> contextProperties)
        {
            writer.WriteStartObject();
            if (contextProperties.Any())
            {
                // There is a flaw/bug in some of the contract resolvers or naming strategies (e.g. CamelCaseNamingStrategy)
                // that causes the logic to not apply the casing preferences. We are forcing the resolution of the appropriate
                // JsonContract here to account for the discrepancy.
                JsonDictionaryContract contract = serializer.ContractResolver.ResolveContract(ContextPropertiesJsonConverter.serializerType) as JsonDictionaryContract;

                foreach (KeyValuePair<string, object> property in contextProperties.OrderBy(prop => prop.Key))
                {
                    writer.WritePropertyName(contract != null ? contract.DictionaryKeyResolver.Invoke(property.Key) : property.Key);
                    object propertyValue = property.Value as IConvertible;

                    if (propertyValue != null)
                    {
                        writer.WriteValue(propertyValue);
                    }
                    else
                    {
                        StringBuilder jsonBuilder = new StringBuilder();
                        using (TextWriter textWriter = new StringWriter(jsonBuilder))
                        {
                            using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                            {
                                serializer.Serialize(jsonWriter, property.Value);
                                writer.WriteRawValue(jsonBuilder.ToString());
                            }
                        }
                    }
                }
            }

            writer.WriteEndObject();
        }
    }
}