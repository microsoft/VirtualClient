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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation for writing events to local file
    /// </summary>
    public class SerilogFileLogger : ILogger
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

        /// <summary>
        /// Serializer settings to use when serializing/deserializing objects to/from
        /// JSON.
        /// </summary>
        private static readonly JsonSerializerSettings SerializationSettings = new JsonSerializerSettings
        {
            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Error,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None,

            // By default, serialize enum values to their string representation.
            Converters = new JsonConverter[] { new StringEnumConverter() },

            // By default, ALL properties in the JSON structure will be camel-cased including
            // dictionary keys.
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

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
            if (SerilogFileLogger.LevelMappings.TryGetValue(logLevel, out LogEventLevel level))
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
                LogEvent logEvent = new LogEvent(DateTime.Now, level, exception, template, properties);
                this.logger.Write(logEvent);
            }
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
                    string serializedProperty = property.Value.ToJson(SerilogFileLogger.SerializationSettings);
                    properties.Add(new LogEventProperty(property.Key, new ScalarValue(serializedProperty)));
                }
            }

            return properties;
        }
    }
}
