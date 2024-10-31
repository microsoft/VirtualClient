// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections;
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
        private LogLevel minumumLogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogFileLogger"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger
        /// used by the <see cref="ILogger"/> instance.
        /// </param>
        /// <param name="level">The minimum logging severity level.</param>
        public SerilogFileLogger(LoggerConfiguration configuration, LogLevel level)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.logger = configuration.CreateLogger();
            this.minumumLogLevel = level;
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= this.minumumLogLevel;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
            {
                return;
            }

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

                List<LogEventProperty> properties = new List<LogEventProperty>
                {
                    new LogEventProperty("id", new ScalarValue(Guid.NewGuid().ToString().ToLowerInvariant())),
                    new LogEventProperty("timestamp", new ScalarValue(DateTime.UtcNow.ToString("o"))),
                    new LogEventProperty("level", new ScalarValue(logLevel.ToString())),
                    new LogEventProperty("message", new ScalarValue(eventMessage)),
                    new LogEventProperty("operationId", new ScalarValue(eventContext.ActivityId)),
                    new LogEventProperty("transactionId", new ScalarValue(eventContext.TransactionId)),
                    new LogEventProperty("durationMs", new ScalarValue(eventContext.DurationMs))
                };

                if (eventContext != null)
                {
                    foreach (var entry in eventContext.Properties)
                    {
                        SerilogFileLogger.AddProperties(properties, entry.Key, entry.Value);
                    }
                }
                
                LogEvent logEvent = new LogEvent(
                    DateTime.Now, 
                    level, 
                    exception, 
                    new MessageTemplateParser().Parse(eventMessage), 
                    properties);

                this.logger.Write(logEvent);
            }
        }

        /// <summary>
        /// Adds the property (or nested properties) to the set of <see cref="LogEventProperty"/> values.
        /// </summary>
        /// <param name="properties">Serilog logging framework properties collection.</param>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">The value of the property (including primitive data types as well as collections).</param>
        protected static void AddProperties(List<LogEventProperty> properties, string propertyName, object propertyValue)
        {
            try
            {
                if (propertyValue is IDictionary)
                {
                    List<LogEventProperty> dictionaryProperties = new List<LogEventProperty>();
                    foreach (DictionaryEntry entry in propertyValue as IDictionary)
                    {
                        SerilogFileLogger.AddProperties(dictionaryProperties, entry.Key.ToString(), entry.Value);
                    }

                    Dictionary<ScalarValue, LogEventPropertyValue> propertyValues = new Dictionary<ScalarValue, LogEventPropertyValue>();
                    if (dictionaryProperties.Any())
                    {
                        foreach (var entry in dictionaryProperties)
                        {
                            propertyValues.Add(new ScalarValue(entry.Name), entry.Value);
                        }
                    }

                    properties.Add(new LogEventProperty(propertyName, new DictionaryValue(propertyValues)));
                }
                else
                {
                    properties.Add(new LogEventProperty(propertyName, new ScalarValue(propertyValue)));
                }
            }
            catch
            {
                // Best Effort
            }
        }
    }
}
