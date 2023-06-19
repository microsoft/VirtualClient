// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::Serilog;
    using global::Serilog.Core;
    using global::Serilog.Events;
    using global::Serilog.Parsing;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing data to local comma-separated
    /// value (CSV) file.
    /// </summary>
    public class MetricsCsvFileLogger : ILogger
    {
        private static readonly IDictionary<LogLevel, LogEventLevel> LevelMappings = new Dictionary<LogLevel, LogEventLevel>
        {
            { LogLevel.Trace, LogEventLevel.Verbose },
            { LogLevel.Debug, LogEventLevel.Verbose },
            { LogLevel.Information, LogEventLevel.Information },
            { LogLevel.Warning, LogEventLevel.Warning },
            { LogLevel.Error, LogEventLevel.Error },
            { LogLevel.Critical, LogEventLevel.Fatal }
        };

        private static readonly IDictionary<string, string> CsvHeaderMappings = new Dictionary<string, string>
        {
            { "Timestamp", null },
            { "ExperimentId", "experimentId" },
            { "ClientId", "agentId" },
            { "Profile", "executionProfile" },
            { "ProfileName", "executionProfileName" },
            { "ToolName", "toolName" },
            { "ScenarioName", "scenarioName" },
            { "ScenarioStartTime", "scenarioStartTime" },
            { "ScenarioEndTime", "scenarioEndTime" },
            { "MetricCategorization", "metricCategorization" },
            { "MetricName", "metricName" },
            { "MetricValue", "metricValue" },
            { "MetricUnit", "metricUnit" },
            { "MetricDescription", "metricDescription" },
            { "MetricRelativity", "metricRelativity" },
            { "ExecutionSystem", "executionSystem" },
            { "OperatingSystemPlatform", "platformArchitecture" },
            { "OperationId", null },
            { "OperationParentId", null },
            { "AppName", null },
            { "AppHost", null },
            { "AppVersion", "appVersion" },
            { "AppTelemetryVersion", "appVersion" },
            { "tags", " " },
            { "metadata", " " }
        };

        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLogger"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger used by the <see cref="ILogger"/> instance.
        /// </param>
        public MetricsCsvFileLogger(LoggerConfiguration configuration)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.logger = configuration.CreateLogger();
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
                if (MetricsCsvFileLogger.LevelMappings.TryGetValue(logLevel, out LogEventLevel level) && eventId.Id == (int)LogType.Metrics)
                {
                    string eventMessage = null;

                    if (!string.IsNullOrWhiteSpace(eventId.Name))
                    {
                        // Use the explicitly defined event name.
                        eventMessage = eventId.Name;
                    }

                    MessageTemplate template = new MessageTemplateParser().Parse(eventMessage);
                    List<LogEventProperty> properties = MetricsCsvFileLogger.GetEventProperties(eventContext);
                    LogEvent logEvent = new LogEvent(DateTime.Now, level, exception, template, properties);
                    this.logger.Write(logEvent);
                }
            }
        }

        private static List<LogEventProperty> GetEventProperties(EventContext context)
        {
            List<LogEventProperty> properties = new List<LogEventProperty>();
            if (context != null)
            {
                properties.Add(new LogEventProperty("transactionId", new ScalarValue(context.TransactionId)));
                properties.Add(new LogEventProperty("durationMs", new ScalarValue(context.DurationMs)));

                foreach (KeyValuePair<string, object> property in context.Properties.OrderBy(p => p.Key))
                {
                    properties.Add(new LogEventProperty(property.Key, new ScalarValue(property.Value != null ? property.Value : string.Empty)));
                }
            }

            return properties;
        }
    }
}
