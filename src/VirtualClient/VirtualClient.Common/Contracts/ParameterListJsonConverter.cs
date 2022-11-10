// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a JSON converter that can handle the serialization/deserialization of
    /// <see cref="IEnumerable{IConvertible}"/> objects.
    /// </summary>
    public class ParameterListJsonConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IEnumerable<IConvertible>);
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            reader.ThrowIfNull(nameof(reader));

            List<IConvertible> list = new List<IConvertible>();
            JArray providerJson = JArray.Load(reader);
            foreach (JToken child in providerJson.Children())
            {
                JValue jValue = child as JValue;
                IConvertible value = jValue.Value as IConvertible;
                list.Add(value);
            }

            return list;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object values, JsonSerializer serializer)
        {
            writer.ThrowIfNull(nameof(writer));
            values.ThrowIfNull(nameof(values));
            serializer.ThrowIfNull(nameof(serializer));

            writer.WriteStartArray();
            foreach (IConvertible value in (IEnumerable<IConvertible>)values)
            {
                serializer.Serialize(writer, value);
            }

            writer.WriteEndArray();
        }
    }
}