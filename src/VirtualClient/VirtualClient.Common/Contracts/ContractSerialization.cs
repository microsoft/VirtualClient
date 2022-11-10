// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Extension methods for handling serialization/deserialization operations in a consistent
    /// and expected way for data contract items/objects.
    /// </summary>
    public static class ContractSerialization
    {
        /// <summary>
        /// Default character set encoding for VirtualClient.Common contracts/entities.
        /// </summary>
        public static Encoding DefaultEncoding { get; } = Encoding.UTF8;

        /// <summary>
        /// Serializer settings to use when serializing/deserializing objects to/from
        /// JSON.
        /// </summary>
        public static JsonSerializerSettings DefaultJsonSerializationSettings { get; } = new JsonSerializerSettings
        {
            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Error,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None,

            // By default, serialize enum values to their string representation.
            Converters = new JsonConverter[] { new StringEnumConverter() }
        };

        /// <summary>
        /// Deserializes the object from its JSON-formatted representation.
        /// </summary>
        /// <param name="jsonData">The JSON-formatted representation of the object to deserialize.</param>
        /// <param name="settings">
        /// Optional parameter defines the JSON serializer settings to apply. The default serialization
        /// settings will be applied otherwise.
        /// </param>
        /// <returns>
        /// A runtime object type.
        /// </returns>
        public static TData FromJson<TData>(this string jsonData, JsonSerializerSettings settings = null)
        {
            return JsonConvert.DeserializeObject<TData>(jsonData, settings ?? ContractSerialization.DefaultJsonSerializationSettings);
        }

        /// <summary>
        /// Deserializes the object from its JSON-formatted representation.
        /// </summary>
        /// <param name="jsonData">The JSON-formatted representation of the object to deserialize.</param>
        /// <param name="objectType">The type of the object the string representation should be deserialized to.</param>
        /// <param name="settings">
        /// Optional parameter defines the JSON serializer settings to apply. The default serialization
        /// settings will be applied otherwise.
        /// </param>
        /// <returns>
        /// A runtime object type.
        /// </returns>
        public static object FromJson(this string jsonData, Type objectType, JsonSerializerSettings settings = null)
        {
            return JsonConvert.DeserializeObject(jsonData, objectType, settings ?? ContractSerialization.DefaultJsonSerializationSettings);
        }

        /// <summary>
        /// Serializes the object to a JSON-formatted representation.
        /// </summary>
        /// <param name="data">The data object to cnnvert into JSON format.</param>
        /// <param name="settings">
        /// Optional parameter defines the JSON serializer settings to apply. The default serialization
        /// settings will be applied otherwise.
        /// </param>
        /// <returns>
        /// A JSON-formatted string representation of the object.
        /// </returns>
        public static string ToJson(this object data, JsonSerializerSettings settings = null)
        {
            return JsonConvert.SerializeObject(data, settings ?? ContractSerialization.DefaultJsonSerializationSettings);
        }

        /// <summary>
        /// Converts the <see cref="Stream"/> object into a data object/contract representation using default
        /// encoding.
        /// </summary>
        /// <typeparam name="TData">The data type of the object to convert into from the stream.</typeparam>
        /// <param name="itemStream">The stream containing the data object bytes to convert.</param>
        /// <returns>
        /// A data object/contract containing the JSON-deserialized contents of the stream.
        /// </returns>
        public static TData FromStream<TData>(this Stream itemStream)
        {
            if (itemStream == null)
            {
                throw new ArgumentException("The item stream parameter is required.", nameof(itemStream));
            }

            TData item = default(TData);
            itemStream.Position = 0;
            using (StreamReader reader = new StreamReader(itemStream))
            {
                item = reader.ReadToEnd().FromJson<TData>();
            }

            return item;
        }

        /// <summary>
        /// Converts the data/item object into a <see cref="Stream"/> representation using default
        /// encoding.
        /// </summary>
        /// <typeparam name="TData">The data type of the object to convert into a stream.</typeparam>
        /// <param name="item">The data/item object to convert.</param>
        /// <returns>
        /// A <see cref="Stream"/> object containing the JSON-serialized contents of the item.
        /// </returns>
        public static Stream ToStream<TData>(this TData item)
        {
            if (item == null)
            {
                throw new ArgumentException("The item parameter is required.", nameof(item));
            }

            MemoryStream itemStream = new MemoryStream();
            using (StreamWriter streamWriter = new StreamWriter(itemStream, encoding: ContractSerialization.DefaultEncoding, bufferSize: 1024, leaveOpen: true))
            {
                using (JsonWriter writer = new JsonTextWriter(streamWriter))
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.None;
                    JsonSerializer.Create(ContractSerialization.DefaultJsonSerializationSettings).Serialize(writer, item);
                    writer.Flush();
                    streamWriter.Flush();
                }
            }

            itemStream.Position = 0;
            return itemStream;
        }
    }
}