// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
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
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation for writing events to local file
    /// </summary>
    public class SerilogFileLogger : ILogger
    {
        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogFileLogger"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger
        /// used by the <see cref="ILogger"/> instance.
        /// </param>
        public SerilogFileLogger(LoggerConfiguration configuration)
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
            string eventMessage = null;
            EventContext eventContext = state as EventContext;

            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                // Use the explicitly defined event name.
                eventMessage = eventId.Name;
            }
            else if (eventContext == null)
            {
                if (state != null && exception != null && formatter != null)
                {
                    // A formatted error message is expected.
                    eventMessage = formatter.Invoke(state, exception);
                }
                else if (exception != null && state != null)
                {
                    // State context and an exception were provided.
                    eventMessage = $"{exception.ToString(withCallStack: false, withErrorTypes: true)} {state}";
                }
                else if (exception != null)
                {
                    // An exception was provided by itself.
                    eventMessage = exception.ToString(withCallStack: false, withErrorTypes: true);
                }
                else
                {
                    // State context was provided by itself.
                    eventMessage = state.ToString();
                }
            }

            MessageTemplate template = new MessageTemplateParser().Parse(eventMessage);
            List<LogEventProperty> properties = this.GetEventProperties(eventContext);
            LogEvent logEvent = new LogEvent(DateTime.Now, LogEventLevel.Information, exception, template, properties);
            this.logger.Write(logEvent);
        }

        private List<LogEventProperty> GetEventProperties(EventContext context)
        {
            List<LogEventProperty> properties = new List<LogEventProperty>();
            if (context != null)
            {
                properties.Add(new LogEventProperty("transactionId", new ScalarValue(context.TransactionId)));
                properties.Add(new LogEventProperty("durationMs", new ScalarValue(context.DurationMs)));

                foreach (KeyValuePair<string, object> property in context.Properties.OrderBy(p => p.Key))
                {
                    string serializedProperty = JsonContextSerialization.Serialize(property.Value);
                    properties.Add(new LogEventProperty(property.Key, new ScalarValue(serializedProperty)));
                }
            }

            return properties;
        }
    }
}
