// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using global::Serilog;
    using global::Serilog.Core;
    using global::Serilog.Events;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Telemetry;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing metrics data to a CSV file.
    /// </summary>
    public class MetricsCsvFileLogger : ILogger
    {
        internal static readonly IEnumerable<MetricsCsvField> CsvFields;

        private static AssemblyName loggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
        private static AssemblyName executingAssembly = Assembly.GetEntryAssembly().GetName();
        private Logger logger;

        static MetricsCsvFileLogger()
        {
            MetricsCsvFileLogger.loggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
            MetricsCsvFileLogger.executingAssembly = Assembly.GetEntryAssembly().GetName();

            MetricsCsvFileLogger.CsvFields = new List<MetricsCsvField>
            {
                new MetricsCsvField("Timestamp", (ctx) => DateTime.UtcNow.ToString("o")),
                new MetricsCsvField("ExperimentId", "experimentId"),
                new MetricsCsvField("ClientId", "agentId"),
                new MetricsCsvField("Profile", "executionProfile"),
                new MetricsCsvField("ProfileName", "executionProfileName"),
                new MetricsCsvField("ToolName", "toolName"),
                new MetricsCsvField("ScenarioName", "scenarioName"),
                new MetricsCsvField("ScenarioStartTime", "scenarioStartTime"),
                new MetricsCsvField("ScenarioEndTime", "scenarioEndTime"),
                new MetricsCsvField("MetricCategorization", "metricCategorization"),
                new MetricsCsvField("MetricName", "metricName"),
                new MetricsCsvField("MetricValue", "metricValue"),
                new MetricsCsvField("MetricUnit", "metricUnit"),
                new MetricsCsvField("MetricDescription", "metricDescription"),
                new MetricsCsvField("MetricRelativity", "metricRelativity"),
                new MetricsCsvField("ExecutionSystem", "executionSystem"),
                new MetricsCsvField("OperatingSystemPlatform", "operatingSystemPlatform"),
                new MetricsCsvField("OperationId", (ctx) => ctx.ActivityId.ToString()),
                new MetricsCsvField("OperationParentId", (ctx) => ctx.ParentActivityId.ToString()),
                new MetricsCsvField("AppHost", propertyValue: Environment.MachineName),
                new MetricsCsvField("AppName", propertyValue: MetricsCsvFileLogger.executingAssembly.Name),
                new MetricsCsvField("AppVersion", propertyValue: MetricsCsvFileLogger.executingAssembly.Version.ToString()),
                new MetricsCsvField("AppTelemetryVersion", propertyValue: MetricsCsvFileLogger.loggingAssembly.Version.ToString()),
                new MetricsCsvField("Tags", "tags")
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLogger"/> class.
        /// </summary>
        /// <param name="csvFilePath">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="maximumFileSizeBytes">The maximum size of each CSV file (in bytes) before a new file (rollover) will be created.</param>
        /// <param name="flushInterval">The interval at which buffered content will be written to file.</param>
        public MetricsCsvFileLogger(string csvFilePath, long maximumFileSizeBytes, TimeSpan flushInterval)
        {
            LoggerConfiguration logConfiguration = new LoggerConfiguration().WriteTo.File(
                    csvFilePath,
                    outputTemplate: "{Message}",
                    fileSizeLimitBytes: maximumFileSizeBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: 10,
                    flushToDiskInterval: flushInterval,
                    hooks: new MetricsCsvFileLifecycleHooks());

            this.logger = logConfiguration.CreateLogger();
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            EventContext eventContext = state as EventContext;

            if (eventContext != null)
            {
                if (eventId.Id == (int)LogType.Metrics)
                {
                    string message = MetricsCsvFileLogger.CreateMessage(eventContext);
                    this.logger.Write(LogEventLevel.Information, message);
                }
            }
        }

        internal static string CreateMessage(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(Environment.NewLine);
            messageBuilder.AppendJoin(',', MetricsCsvFileLogger.CsvFields.Select(field => $"\"{field.GetFieldValue(context)}\""));

            return messageBuilder.ToString();
        }
    }

    internal class MetricsCsvField
    {
        // We are very purposefully using member variables here vs. properties to keep
        // the evaluation of the EventContext objects as efficient as possible
        // (i.e. avoiding unnecessary method callstacks).
        private string propertyName;
        private string propertyValue;
        private Func<EventContext, string> propertyQuery;

        public MetricsCsvField(string columnName, Func<EventContext, string> query)
        {
            this.ColumnName = columnName;
            this.propertyQuery = query;
        }

        public MetricsCsvField(string columnName, string propertyName = null, string propertyValue = null)
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
