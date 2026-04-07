// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Contains assertions related to the serialization of objects.
    /// </summary>
    public static class SerializationAssert
    {
        /// <summary>
        /// Verifies that a type is JSON serializable without any data loss.
        /// Throws an exception if the deserialized object is not binary-equivalent
        /// to the serialized object.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="data">The object to serialize</param>
        /// <param name="serializerSettings">Serializer settings to apply to the serialization and deserialization of the data/object.</param>
        public static void IsJsonSerializable<T>(T data, JsonSerializerSettings serializerSettings = null)
        {
            try
            {
                bool isSerializable = false;

                string serializedData = serializerSettings == null
                    ? JsonConvert.SerializeObject(data)
                    : JsonConvert.SerializeObject(data, serializerSettings);

                if (!string.IsNullOrEmpty(serializedData))
                {
                    T deserializedData = serializerSettings == null
                        ? JsonConvert.DeserializeObject<T>(serializedData)
                        : JsonConvert.DeserializeObject<T>(serializedData, serializerSettings);

                    if (deserializedData != null)
                    {
                        // Ensure that we did not lose data in the serialize/deserialize
                        // process using binary comparison.
                        string serializedData2 = serializerSettings == null
                            ? JsonConvert.SerializeObject(data)
                            : JsonConvert.SerializeObject(data, serializerSettings);

                        SerializationAssert.JsonEquals(serializedData, serializedData2);

                        isSerializable = true;
                    }
                }

                if (!isSerializable)
                {
                    throw new SerializationAssertFailedException(
                        $"Object of type {typeof(T).Name} is not JSON serializable or does not serialize/deserialize without data loss.");
                }
            }
            catch (SerializationAssertFailedException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new SerializationAssertFailedException(
                    $"Object of type {typeof(T).Name} is not JSON serializable or does not serialize/deserialize without data loss.  {exc.Message}");
            }
        }

        /// <summary>
        /// Verifies that a type is YAML serializable without any data loss.
        /// Throws an exception if the deserialized object is not binary-equivalent
        /// to the serialized object.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="data">The object to serialize</param>
        public static void IsYamlSerializable<T>(T data)
        {
            try
            {
                bool isSerializable = false;

                var yamlSerializer = new YamlDotNet.Serialization.SerializerBuilder().Build();
                string serializedData = yamlSerializer.Serialize(data);

                if (!string.IsNullOrEmpty(serializedData))
                {
                    T deserializedData = new YamlDotNet.Serialization.DeserializerBuilder().Build().Deserialize<T>(serializedData);

                    if (deserializedData != null)
                    {
                        // Ensure that we did not lose data in the serialize/deserialize
                        // process using binary comparison.
                        string serializedData2 = yamlSerializer.Serialize(deserializedData);
                        Assert.AreEqual(serializedData, serializedData2);
                        isSerializable = true;
                    }
                }

                if (!isSerializable)
                {
                    throw new SerializationAssertFailedException(
                        $"Object of type {typeof(T).Name} is not JSON serializable or does not serialize/deserialize without data loss.");
                }
            }
            catch (SerializationAssertFailedException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new SerializationAssertFailedException(
                    $"Object of type {typeof(T).Name} is not JSON serializable or does not serialize/deserialize without data loss.  {exc.Message}");
            }
        }

        /// <summary>
        /// Asserts that the two pieces of JSON text are exactly equal.
        /// </summary>
        /// <param name="jsonText1">JSON text to compare against the second piece of text.</param>
        /// <param name="jsonText2">JSON text to compare against the first piece of text.</param>
        /// <param name="message">The error message to use if the JSON text does not match.</param>
        /// <param name="caseSensitivity">The case-sensitivity of the text comparison (default = OrdinalIgnoreCase)</param>
        public static void JsonEquals(string jsonText1, string jsonText2, string message = null, StringComparison caseSensitivity = StringComparison.OrdinalIgnoreCase)
        {
            SerializationAssert.ValidateTextIsJson(jsonText1);
            SerializationAssert.ValidateTextIsJson(jsonText2);

            // Remove whitespace and standardize quote characters to the single quote ' char.
            string normalizedJsonText1 = Regex.Replace(jsonText1.RemoveWhitespace(), "\"", "'");
            string normalizedJsonText2 = Regex.Replace(jsonText2.RemoveWhitespace(), "\"", "'");

            if (!string.Equals(normalizedJsonText1, normalizedJsonText2, caseSensitivity))
            {
                throw new SerializationAssertFailedException(message
                    ?? $"JSON text/string values are not equal.  JSON text 1<{normalizedJsonText1}>.  JSON text 2 <{normalizedJsonText2}>.");
            }
        }

        private static void ValidateTextIsJson(string text)
        {
            try
            {
                JToken.Parse(text);
            }
            catch
            {
                throw new SerializationAssertFailedException(
                    $"The text supplied was expected to be valid JSON but does not appear to be.  Invalid JSON:  {text}");
            }
        }
    }
}
