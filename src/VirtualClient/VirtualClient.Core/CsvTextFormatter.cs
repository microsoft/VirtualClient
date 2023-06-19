// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Serilog.Events;
    using Serilog.Formatting;

    /// <summary>
    /// Formats log/trace messages as Csv text.
    /// </summary>
    internal class CsvTextFormatter : ITextFormatter
    {
        public CsvTextFormatter(IEnumerable<string> includeFields = null)
            : base()
        {
            this.IncludeProperties = new List<string>();
            if (includeFields?.Any() == true)
            {
                this.IncludeProperties.AddRange(includeFields);
            }
        }        

        /// <summary>
        /// A list of properties to include in the csv log
        /// </summary>
        public List<string> IncludeProperties { get; }        

        /// <summary>
        /// Formats the log event information as a CSV-structured object.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent != null)
            {
                if (logEvent.Properties?.Any() == true)
                {
                    int propertyIndex = 0;
                    foreach (string property in this.IncludeProperties)
                    {
                        if (propertyIndex > 0)
                        {
                            output.Write(",");
                        }

                        if (string.Equals(property, "timeStamp", StringComparison.OrdinalIgnoreCase))
                        {
                            output.Write($"{logEvent.Timestamp.ToString("o")}");
                        }
                        else
                        {
                            output.Write($"{this.TryGetPropertyValue(logEvent, property)}");
                        }

                        propertyIndex++;
                    }
                }
                else
                {
                    output.Write($"{logEvent.Timestamp.ToString("o")},");
                    output.Write($"{logEvent.MessageTemplate.Text}");
                }

                output.Write("\n");
            }
        }
        
        private string TryGetPropertyValue(LogEvent logEvent, string propertyName)
        {
            string propertyValue = string.Empty;

            try
            {
                if (logEvent.Properties.ContainsKey(propertyName))
                {
                    JToken value = JObject.FromObject(logEvent.Properties[propertyName])?.First?.First;
                    if (value != null)
                    {
                        propertyValue = value.ToString(Formatting.None).Replace(",", "  ");
                    }
                }
            }
            catch
            {
                // DO NOTHING IF PROPERTY NOT FOUND. Return string.Empty
            }

            return propertyValue == null ? string.Empty : propertyValue;
        }
    }
}
