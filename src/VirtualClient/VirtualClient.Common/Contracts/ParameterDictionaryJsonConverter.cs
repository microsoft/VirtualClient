// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a JSON converter that can handle the serialization/deserialization of
    /// <see cref="IDictionary{String, IConvertible}"/> objects.
    /// </summary>
    public class ParameterDictionaryJsonConverter : JsonConverter
    {
        private static readonly Type ParameterDictionaryType = typeof(IDictionary<string, IConvertible>);

        /// <summary>
        /// Returns true/false whether the object type is supported for JSON serialization/deserialization.
        /// </summary>
        /// <param name="objectType">The type of object to serialize/deserialize.</param>
        /// <returns>
        /// True if the object is supported, false if not.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == ParameterDictionaryType;
        }

        /// <summary>
        /// Reads the JSON text from the reader and converts it into an <see cref="IDictionary{String, IConvertible}"/>
        /// object instance.
        /// </summary>
        /// <param name="reader">Contains the JSON text defining the <see cref="IDictionary{String, IConvertible}"/> object.</param>
        /// <param name="objectType">The type of object (in practice this will only be an <see cref="IDictionary{String, IConvertible}"/> type).</param>
        /// <param name="existingValue">Unused.</param>
        /// <param name="serializer">Unused.</param>
        /// <returns>
        /// A deserialized <see cref="IDictionary{String, IConvertible}"/> object converted from JSON text.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentException("The reader parameter is required.", nameof(reader));
            }

            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject providerJsonObject = JObject.Load(reader);
                ParameterDictionaryJsonConverter.ReadDictionaryEntries(providerJsonObject, dictionary);
            }

            return dictionary;
        }

        /// <summary>
        /// Writes a <see cref="IDictionary{String, IConvertible}"/> object to JSON text.
        /// </summary>
        /// <param name="writer">Handles the writing of the JSON text.</param>
        /// <param name="value">The <see cref="IDictionary{String, IConvertible}"/> object to serialize to JSON text.</param>
        /// <param name="serializer">The JSON serializer handling the serialization to JSON text.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.ThrowIfNull(nameof(writer));
            serializer.ThrowIfNull(nameof(serializer));

            IDictionary<string, IConvertible> dictionary = value as IDictionary<string, IConvertible>;
            if (value != null)
            {
                ParameterDictionaryJsonConverter.WriteDictionaryEntries(writer, dictionary, serializer);
            }
        }

        private static void ReadDictionaryEntries(JToken providerJsonObject, IDictionary<string, IConvertible> dictionary)
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
                            IConvertible settingValue = propertyValue.Value as IConvertible;

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

        private static void WriteDictionaryEntries(JsonWriter writer, IDictionary<string, IConvertible> dictionary, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            if (dictionary.Count > 0)
            {
                // There is a flaw/bug in some of the contract resolvers or naming strategies (e.g. CamelCaseNamingStrategy)
                // that causes the logic to not apply the casing preferences. We are forcing the resolution of the appropriate
                // JsonContract here to account for the discrepancy.
                JsonDictionaryContract dictionaryContract = serializer.ContractResolver.ResolveContract(ParameterDictionaryType) as JsonDictionaryContract;

                foreach (KeyValuePair<string, IConvertible> entry in dictionary)
                {
                    writer.WritePropertyName(dictionaryContract != null ? dictionaryContract.DictionaryKeyResolver.Invoke(entry.Key) : entry.Key);
                    serializer.Serialize(writer, entry.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}