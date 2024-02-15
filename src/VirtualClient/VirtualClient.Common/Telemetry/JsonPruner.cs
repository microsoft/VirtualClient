// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Handles pruning JSON strings.
    /// </summary>
    public static class JsonPruner
    {
        /// <summary>
        /// Prunes the JSON content/document to a length matching the maximum length while preserving
        /// the content as valid JSON.
        /// </summary>
        /// <param name="content">The JSON content/document to prune/truncate.</param>
        /// <param name="maxCharLength">The length to which the JSON content/document should be reduced (in characters).</param>
        /// <returns>
        /// A valid JSON document that that has been reduced/truncated down to the maximum length.
        /// </returns>
        public static string Prune(string content, int maxCharLength)
        {
            content.ThrowIfNull(nameof(content));

            int originalContentLength = content.Length;
            string prunedContent = content;

            if (originalContentLength > maxCharLength)
            {
                // Either a JObject or JArray.
                object parsedJsonObject = null;
                try
                {
                    // There's no 'TryParse' here. Our object is either an array or an object, and *we don't know which*
                    parsedJsonObject = JObject.Parse(prunedContent);
                }
                catch (Exception)
                {
                    parsedJsonObject = JArray.Parse(prunedContent);
                }

                int totalCharsTruncated = 0;
                int charsToTruncatePerProperty = 10;

                if (parsedJsonObject is JObject)
                {
                    IEnumerable<JValue> stringProperties = JsonPruner.GetStringProperties((JObject)parsedJsonObject)
                        ?.OrderByDescending(prop => prop.Value?.ToString().Length);

                    if (stringProperties?.Any() == true)
                    {
                        totalCharsTruncated = JsonPruner.TruncateProperties(stringProperties, originalContentLength, maxCharLength, charsToTruncatePerProperty);
                    }
                }
                else
                {
                    // JArray: Given that our object is a JArray, we truncate by removing array elements, starting from the last element.
                    // However, instead of stopping at element 0 (the first element), we stop at element 1. If we stopped at zero, we would have
                    // truncated all of the array elements, leaving no JSON data left to be sent up as telemetry. In that case, we instead throw the
                    // JsonSerializationException.
                    IEnumerable<JValue> stringProperties = JsonPruner.GetStringProperties((JArray)parsedJsonObject)
                        ?.OrderByDescending(prop => prop.Value?.ToString().Length);

                    if (stringProperties?.Any() == true)
                    {
                        totalCharsTruncated = JsonPruner.TruncateProperties(stringProperties, originalContentLength, maxCharLength, charsToTruncatePerProperty);
                    }
                    else
                    {
                        JArray jsonArray = (parsedJsonObject as JArray);

                        for (int i = jsonArray.Count - 1; i > 1 && (originalContentLength - totalCharsTruncated > maxCharLength); i--)
                        {
                            totalCharsTruncated += jsonArray[i].ToString().Length;
                            jsonArray.RemoveAt(i);
                        }
                    }
                }

                if (originalContentLength - totalCharsTruncated > maxCharLength)
                {
                    throw new JsonSerializationException($"The content could not be pruned to be within a maximum length of {maxCharLength} characters.");
                }

                prunedContent = JsonConvert.SerializeObject(parsedJsonObject);
            }

            return prunedContent;
        }

        private static int TruncateProperties(IEnumerable<JValue> properties, int originalContentLength, int maxCharLength, int charsToTruncatePerProperty)
        {
            bool continueTruncating = true;
            int numPropertiesTruncated = 0;
            int totalCharsTruncated = 0;

            // 1) We first truncate property values with more than 512 more characters.  This is an educated assumption
            //    that properties with that many characters have more expendable characters than properties with
            //    less than 512 characters with regards to the ability to decipher context from the remaining string content.
            IEnumerable<KeyValuePair<int, List<JValue>>> truncationRanges = new List<KeyValuePair<int, List<JValue>>>
            {
                new KeyValuePair<int, List<JValue>>(25, properties.Where(prop => (prop.Value as string).Length >= 512).ToList()),
                new KeyValuePair<int, List<JValue>>(25, properties.Where(prop => (prop.Value as string).Length >= 256 && (prop.Value as string).Length < 512).ToList()),
                new KeyValuePair<int, List<JValue>>(50, properties.Where(prop => (prop.Value as string).Length >= 64 && (prop.Value as string).Length < 256).ToList()),
                new KeyValuePair<int, List<JValue>>(15, properties.Where(prop => (prop.Value as string).Length < 64).ToList())
            };

            foreach (var range in truncationRanges)
            {
                if (originalContentLength - totalCharsTruncated <= maxCharLength)
                {
                    break;
                }

                IEnumerator<JValue> matchingProperties = range.Value.GetEnumerator();
                while (continueTruncating)
                {
                    numPropertiesTruncated = 0;
                    matchingProperties.Reset();

                    while (matchingProperties.MoveNext())
                    {
                        JValue property = matchingProperties.Current;
                        if (property?.Value != null && (property.Value as string).Length > (range.Key + charsToTruncatePerProperty))
                        {
                            JsonPruner.TruncateProperty(property, charsToTruncatePerProperty);
                            totalCharsTruncated += charsToTruncatePerProperty;
                            numPropertiesTruncated++;
                        }

                        if (originalContentLength - totalCharsTruncated <= maxCharLength)
                        {
                            continueTruncating = false;
                            break;
                        }
                    }

                    if (numPropertiesTruncated == 0)
                    {
                        // We've gone as far as we can go with truncating the properties.
                        break;
                    }
                }
            }

            return totalCharsTruncated;
        }

        private static void TruncateProperty(JValue property, int numCharsToTruncate)
        {
            if (property != null && property.Value != null)
            {
                string propertyValue = property.Value.ToString();
                if (propertyValue.Length > numCharsToTruncate)
                {
                    string truncatedPropertyValue = propertyValue.Substring(0, propertyValue.Length - numCharsToTruncate);
                    property.Value = truncatedPropertyValue;
                }
            }
        }

        private static IEnumerable<JValue> GetStringProperties(JObject jsonObject)
        {
            List<JValue> stringProperties = new List<JValue>();
            foreach (KeyValuePair<string, JToken> property in jsonObject)
            {
                JsonPruner.RecurseChildrenToGetStringProperties(property.Value, stringProperties);
            }

            return stringProperties;
        }

        private static IEnumerable<JValue> GetStringProperties(JArray jsonArray)
        {
            List<JValue> stringProperties = new List<JValue>();
            foreach (JToken property in jsonArray)
            {
                JsonPruner.RecurseChildrenToGetStringProperties(property, stringProperties);
            }

            return stringProperties;
        }

        private static void RecurseChildrenToGetStringProperties(JToken property, List<JValue> stringProperties)
        {
            if (!property.HasValues)
            {
                if (property.Type == JTokenType.String)
                {
                    stringProperties.Add(property as JValue);
                }
            }
            else
            {
                foreach (JToken child in property.Children())
                {
                    JsonPruner.RecurseChildrenToGetStringProperties(child, stringProperties);
                }
            }
        }
    }
}