// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Serilog.Events;
    using Serilog.Formatting;

    /// <summary>
    /// Formats log/trace messages as JSON text.
    /// </summary>
    internal class JsonTextFormatter : ITextFormatter
    {
        public JsonTextFormatter(IEnumerable<string> excludeProperties = null)
            : base()
        {
            this.ExcludeProperties = new List<string>();
            if (excludeProperties?.Any() == true)
            {
                this.ExcludeProperties.AddRange(excludeProperties);
            }
        }

        /// <summary>
        /// A list of properties to exclude from output.
        /// </summary>
        public List<string> ExcludeProperties { get; }

        /// <summary>
        /// Formats the log event information as a JSON-structured object.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent != null)
            {
                const string tab = "    ";

                output.WriteLine("{");
                output.WriteLine($"{tab}\"timestamp\": \"{logEvent.Timestamp.ToString("o")}\",");
                output.WriteLine($"{tab}\"level\": \"{logEvent.Level}\",");

                if (logEvent.Properties?.Any() == true)
                {
                    output.WriteLine($"{tab}\"message\": \"{logEvent.MessageTemplate.Text}\",");

                    int propertyIndex = 0;
                    foreach (var property in logEvent.Properties)
                    {
                        if (!this.ExcludeProperties.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            try
                            {
                                if (propertyIndex > 0)
                                {
                                    output.WriteLine(",");
                                }

                                output.Write($"{tab}\"{property.Key}\": ");
                                JToken value = JObject.FromObject(property.Value)?.First?.First;
                                if (value != null)
                                {
                                    output.Write(value.ToString());
                                }
                                else
                                {
                                    output.Write("null");
                                }
                            }
                            catch
                            {
                                // Best effort.
                            }
                            finally
                            {
                                propertyIndex++;
                            }
                        }
                    }
                }
                else
                {
                    output.WriteLine($"{tab}\"message\": \"{logEvent.MessageTemplate.Text}\"");
                }

                output.WriteLine(string.Empty);
                output.WriteLine("}");
                output.WriteLine("---");
            }
        }
    }
}
