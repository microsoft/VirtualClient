// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions methods for collection classes used as part of the standard
    /// contract definitions.
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Extension merges a set of new entries with the existing set of dictionary entries based upon
        /// the individual entry keys.
        /// </summary>
        /// <param name="dictionary">The dictionary into which the new entries will be merged/added.</param>
        /// <param name="items">The set of new entries that will be merged with the existing dictionary entries.</param>
        /// <param name="withReplace">True to replace items that already exist, false to reject adding items that already exist.</param>
        public static void AddRange<T>(this IDictionary<string, T> dictionary, IEnumerable<KeyValuePair<string, T>> items, bool withReplace = false)
        {
            if (dictionary == null)
            {
                throw new ArgumentException("The dictionary parameter must be defined.", nameof(dictionary));
            }

            if (items == null)
            {
                throw new ArgumentException("The items parameter must be defined.", nameof(items));
            }

            foreach (KeyValuePair<string, T> entry in items)
            {
                if (withReplace)
                {
                    dictionary[entry.Key] = entry.Value;
                }
                else
                {
                    dictionary.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Extension merges a set of new entries with the existing set of dictionary entries based upon
        /// the individual entry keys.
        /// </summary>
        /// <param name="dictionary">The dictionary into which the new entries will be merged/added.</param>
        /// <param name="items">The set of new entries that will be merged with the existing dictionary entries.</param>
        public static void AddRange<T>(this HashSet<T> dictionary, IEnumerable<T> items)
        {
            if (dictionary == null)
            {
                throw new ArgumentException("The dictionary parameter must be defined.", nameof(dictionary));
            }

            if (items == null)
            {
                throw new ArgumentException("The items parameter must be defined.", nameof(items));
            }

            foreach (T entry in items)
            {
                dictionary.Add(entry);
            }
        }

        /// <summary>
        /// Extensions returns true/false whether the two dictionary sets are semantically equal.
        /// </summary>
        /// <param name="dictionary1">The source dictionary set.</param>
        /// <param name="dictionary2">The comparison dictionary set.</param>
        /// <returns>
        /// True if the dictionary sets are semantically equal, false if not.
        /// </returns>
        public static bool Equals<T>(this IDictionary<string, T> dictionary1, IDictionary<string, T> dictionary2)
            where T : IConvertible
        {
            bool areEqual = false;
            if (dictionary1 == null)
            {
                throw new ArgumentException("The dictionary parameter must be defined.", nameof(dictionary1));
            }

            if (dictionary2 == null)
            {
                throw new ArgumentException("The dictionary parameter must be defined.", nameof(dictionary2));
            }

            if (dictionary1.Count == dictionary2.Count)
            {
                areEqual = true;
                foreach (KeyValuePair<string, T> entry in dictionary1)
                {
                    if (!dictionary2.ContainsKey(entry.Key))
                    {
                        areEqual = false;
                        break;
                    }

                    // Not all primitive data types implement Equals in the same way.  By converting
                    // to a string here we place the equality comparison on an even playing field.
                    string value1 = dictionary1[entry.Key].ToString();
                    string value2 = dictionary2[entry.Key].ToString();
                    if (!string.Equals(value1, value2, StringComparison.OrdinalIgnoreCase))
                    {
                        areEqual = false;
                        break;
                    }
                }
            }

            return areEqual;
        }

        /// <summary>
        /// Parses the dictionary entry value into the <see cref="IConvertible"/> type supplied.
        /// </summary>
        /// <param name="dictionary">Dictionary containing the key with a value to parse.</param>
        /// <param name="key">The key in the dictionary.</param>
        /// <param name="defaultValue">A default value to return if the entry does not exist.</param>
        public static T GetValue<T>(this IDictionary<string, IConvertible> dictionary, string key, IConvertible defaultValue = null)
            where T : IConvertible
        {
            dictionary.ThrowIfNull(nameof(dictionary));
            key.ThrowIfNullOrWhiteSpace(nameof(key));

            if (defaultValue == null && !dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException(
                    $"The value cannot be parsed from the dictionary. An entry with key '{key}' does not exist in the dictionary.");
            }

            T convertedValue = default(T);
            IConvertible value = dictionary.ContainsKey(key) && dictionary[key] != null
                ? dictionary[key]
                : defaultValue;

            try
            {
                convertedValue = (T)Convert.ChangeType(value, typeof(T));
            }
            catch (FormatException exc)
            {
                throw new FormatException(
                     $"Invalid data type conversion. The value of the dictionary entry with key '{key}' cannot be parsed as a '{typeof(T).Name}' data type.",
                     exc);
            }

            return convertedValue;
        }

        /// <summary>
        /// Parses the dictionary entry value into the enumeration/enum type supplied.
        /// </summary>
        /// <typeparam name="T">The type of the enum to be parsed.</typeparam>
        /// <param name="dictionary">Dictionary containing the key with a value to parse.</param>
        /// <param name="key">The key in the dictionary.</param>
        /// <param name="defaultValue">A default value to return if the entry does not exist.</param>
        /// <returns>The value parsed.</returns>
        public static T GetEnumValue<T>(this IDictionary<string, IConvertible> dictionary, string key, T? defaultValue = null)
            where T : struct
        {
            dictionary.ThrowIfNull(nameof(dictionary));
            key.ThrowIfNullOrWhiteSpace(nameof(key));

            if (defaultValue == null && !dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException(
                    $"The value cannot be parsed from the dictionary. An entry with key '{key}' does not exist in the dictionary.");
            }

            T value;
            if (!dictionary.ContainsKey(key))
            {
                value = defaultValue.Value;
            }
            else if (!Enum.TryParse<T>(dictionary[key]?.ToString(), out value))
            {
                throw new FormatException(
                    $"Invalid enum type conversion.  The value of key '{key}': value '{dictionary[key]?.ToString()}' is expected to be formatted as a '{typeof(T).Name}' enumeration data type.");
            }

            return value;
        }

        /// <summary>
        /// Parses the dictionary entry value into a <see cref="TimeSpan"/> value.
        /// </summary>
        /// <param name="dictionary">Dictionary containing the key with a value to parse.</param>
        /// <param name="key">The key in the dictionary.</param>
        /// <param name="defaultValue">A default value to return if the entry does not exist.</param>
        /// <returns>The value parsed.</returns>
        public static TimeSpan GetTimeSpanValue(this IDictionary<string, IConvertible> dictionary, string key, TimeSpan? defaultValue = null)
        {
            dictionary.ThrowIfNull(nameof(dictionary));
            key.ThrowIfNullOrWhiteSpace(nameof(key));

            if (defaultValue == null && !dictionary.ContainsKey(key))
            {
                throw new KeyNotFoundException(
                    $"The value cannot be parsed from the dictionary. An entry with key '{key}' does not exist in the dictionary.");
            }

            TimeSpan value = TimeSpan.Zero;
            if (!dictionary.ContainsKey(key))
            {
                value = defaultValue.Value;
            }
            else
            {
                if (!TimeSpan.TryParse(dictionary[key]?.ToString(), out value))
                {
                    throw new FormatException(
                        $"Invalid timespan type conversion.  The value of key '{key}': value '{dictionary[key]?.ToString()}' is expected to be formatted as a '{typeof(TimeSpan).Name}' data type.");
                }
            }

            return value;
        }

        /// <summary>
        /// Shuffle IEnumerable.
        /// </summary>
        /// <param name="originSet">Origin IEnumerable</param>
        /// <returns>Shuffled IEnumerable object.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> originSet)
        {
            Random seed = new Random();
            return originSet == null ? null : originSet.ToList().OrderBy(x => seed.Next()).ToList();
        }
    }
}