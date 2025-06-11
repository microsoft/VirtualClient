// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a JSON converter that can handle the serialization/deserialization of
    /// <see cref="List{T}"/> objects where T is <see cref="IDictionary{TKey, TValue}"/> with string keys and <see cref="IConvertible"/> values.
    /// </summary>
    public class ParameterDictionaryListJsonConverter : JsonConverter
    {
        private static readonly Type ParameterDictionaryListType = typeof(List<IDictionary<string, IConvertible>>);
        private static readonly ParameterDictionaryJsonConverter DictionaryConverter = new ParameterDictionaryJsonConverter();

        /// <summary>
        /// Returns true/false whether the object type is supported for JSON serialization/deserialization.
        /// </summary>
        /// <param name="objectType">The type of object to serialize/deserialize.</param>
        /// <returns>
        /// True if the object is supported, false if not.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == ParameterDictionaryListType;
        }

        /// <summary>
        /// Reads the JSON text from the reader and converts it into a <see cref="List{T}"/> of <see cref="IDictionary{TKey, TValue}"/>
        /// object instance.
        /// </summary>
        /// <param name="reader">Contains the JSON text defining the list of dictionaries object.</param>
        /// <param name="objectType">The type of object (in practice this will only be a list of dictionaries type).</param>
        /// <param name="existingValue">Unused.</param>
        /// <param name="serializer">Unused.</param>
        /// <returns>
        /// A deserialized list of dictionaries object converted from JSON text.
        /// </returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader == null)
            {
                throw new ArgumentException("The reader parameter is required.", nameof(reader));
            }

            List<IDictionary<string, IConvertible>> list = new List<IDictionary<string, IConvertible>>();
            if (reader.TokenType == JsonToken.StartArray)
            {
                JArray array = JArray.Load(reader);
                foreach (JToken item in array)
                {
                    if (item.Type == JTokenType.Object)
                    {
                        IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
                        ReadDictionaryEntries(item, dictionary);
                        list.Add(dictionary);
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Writes a list of dictionaries object to JSON text.
        /// </summary>
        /// <param name="writer">Handles the writing of the JSON text.</param>
        /// <param name="value">The list of dictionaries object to serialize to JSON text.</param>
        /// <param name="serializer">The JSON serializer handling the serialization to JSON text.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.ThrowIfNull(nameof(writer));
            serializer.ThrowIfNull(nameof(serializer));

            List<IDictionary<string, IConvertible>> list = value as List<IDictionary<string, IConvertible>>;
            if (list != null)
            {
                writer.WriteStartArray();
                foreach (var dictionary in list)
                {
                    WriteDictionaryEntries(writer, dictionary, serializer);
                }

                writer.WriteEndArray();
            }
        }

        private static void ReadDictionaryEntries(JToken jsonObject, IDictionary<string, IConvertible> dictionary)
        {
            IEnumerable<JToken> children = jsonObject.Children();
            if (children.Any())
            {
                foreach (JToken child in children)
                {
                    if (child.Type == JTokenType.Property)
                    {
                        if (child.First != null)
                        {
                            JValue propertyValue = child.First as JValue;
                            IConvertible settingValue = propertyValue?.Value as IConvertible;

                            // JSON properties that have periods (.) in them will have a path representation
                            // like this:  ['this.is.a.path'].  We have to account for that when adding the key
                            // to the dictionary. The key we want to add is 'this.is.a.path'
                            string key = child.Path;
                            int lastDotIndex = key.LastIndexOf('.');
                            if (lastDotIndex >= 0)
                            {
                                key = key.Substring(lastDotIndex + 1);
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
                foreach (KeyValuePair<string, IConvertible> entry in dictionary)
                {
                    writer.WritePropertyName(entry.Key);
                    serializer.Serialize(writer, entry.Value);
                }
            }

            writer.WriteEndObject();
        }
    }
}