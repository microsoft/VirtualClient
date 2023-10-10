// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Extension methods for <see cref="EventContext"/>
    /// </summary>
    public static class EventContextLoggingExtensions
    {
        private static readonly AssemblyName LoggingAssembly = Assembly.GetAssembly(typeof(EventContextLoggingExtensions)).GetName();
        private static readonly AssemblyName ExecutingAssembly = Assembly.GetEntryAssembly().GetName();

        private static List<EventContextField> contextFields = new List<EventContextField>
        {
            new EventContextField("Timestamp", (ctx) => DateTime.UtcNow.ToString("o")),
            new EventContextField("ExperimentId", "experimentId"),
            new EventContextField("ClientId", "agentId"),
            new EventContextField("Profile", "executionProfile"),
            new EventContextField("ProfileName", "executionProfileName"),
            new EventContextField("ToolName", "toolName"),
            new EventContextField("ScenarioName", "scenarioName"),
            new EventContextField("ScenarioStartTime", "scenarioStartTime"),
            new EventContextField("ScenarioEndTime", "scenarioEndTime"),
            new EventContextField("MetricCategorization", "metricCategorization"),
            new EventContextField("MetricName", "metricName"),
            new EventContextField("MetricValue", "metricValue"),
            new EventContextField("MetricUnit", "metricUnit"),
            new EventContextField("MetricDescription", "metricDescription"),
            new EventContextField("MetricRelativity", "metricRelativity"),
            new EventContextField("ExecutionSystem", "executionSystem"),
            new EventContextField("OperatingSystemPlatform", "operatingSystemPlatform"),
            new EventContextField("OperationId", (ctx) => ctx.ActivityId.ToString()),
            new EventContextField("OperationParentId", (ctx) => ctx.ParentActivityId.ToString()),
            new EventContextField("AppHost", propertyValue: Environment.MachineName),
            new EventContextField("AppName", propertyValue: EventContextLoggingExtensions.ExecutingAssembly.Name),
            new EventContextField("AppVersion", propertyValue: EventContextLoggingExtensions.ExecutingAssembly.Version.ToString()),
            new EventContextField("AppTelemetryVersion", propertyValue: EventContextLoggingExtensions.LoggingAssembly.Version.ToString()),
            new EventContextField("Tags", "tags")
        };

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string GetCsvHeaders()
        {
            return string.Join(",", contextFields.Select(field => $"\"{field.ColumnName}\""));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string ToCsvRow(this EventContext context)
        {
            return string.Join(",", contextFields.Select(field => field.GetFieldValue(context)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static string GetFieldValue(this EventContext context, string fieldName)
        {
            return contextFields.Where(field => field.ColumnName == fieldName).First().GetFieldValue(context);
        }

        internal class EventContextField
        {
            // We are very purposefully using member variables here vs. properties to keep
            // the evaluation of the EventContext objects as efficient as possible
            // (i.e. avoiding unnecessary method callstacks).
            private string propertyName;
            private string propertyValue;
            private Func<EventContext, string> propertyQuery;

            public EventContextField(string columnName, Func<EventContext, string> query)
            {
                this.ColumnName = columnName;
                this.propertyQuery = query;
            }

            public EventContextField(string columnName, string propertyName = null, string propertyValue = null)
            {
                this.ColumnName = columnName;
                this.propertyName = propertyName;
                this.propertyValue = propertyValue;
            }

            public string ColumnName { get; }

            public string GetFieldValue(EventContext context)
            {
                string value = null;
                if (context?.Properties != null)
                {
                    if (this.propertyValue != null)
                    {
                        value = this.propertyValue;
                    }
                    else if (this.propertyName != null)
                    {
                        if (context.Properties.TryGetValue(this.propertyName, out object propertyValue) && propertyValue != null)
                        {
                            if (propertyValue is DateTime)
                            {
                                value = ((DateTime)propertyValue).ToString("o");
                            }
                            else
                            {
                                value = propertyValue.ToString();
                            }
                        }
                    }
                    else if (this.propertyQuery != null)
                    {
                        value = this.propertyQuery.Invoke(context);
                    }
                }

                return value;
            }
        }
    }
}
