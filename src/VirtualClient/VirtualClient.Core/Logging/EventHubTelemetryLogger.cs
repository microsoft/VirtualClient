// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using global::Azure.Messaging.EventHubs;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation for writing telemetry events
    /// to an Event Hub API instance.
    /// </summary>
    /// Event Hub Documentation: https://docs.microsoft.com/en-us/azure/event-hubs/
    public class EventHubTelemetryLogger : ILogger, IFlushableChannel
    {
        /// <summary>
        /// The default interval at which buffered events will be transmitted.
        /// </summary>
        public static readonly TimeSpan DefaultTransmissionInterval = TimeSpan.FromSeconds(30);

        private static AssemblyName loggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
        private static AssemblyName executingAssembly = Assembly.GetEntryAssembly().GetName();
        private JsonSerializerSettings jsonSerializationSettings;
        private EventHubTelemetryChannel underlyingTelemetryChannel;
        private LogLevel minumumLogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubTelemetryLogger"/> class.
        /// </summary>
        /// <param name="channel">The telemetry channel that log data is sent to.</param>
        /// <param name="level">The minimum logging severity level.</param>
        public EventHubTelemetryLogger(EventHubTelemetryChannel channel, LogLevel level)
        {
            channel.ThrowIfNull(nameof(channel));
            this.underlyingTelemetryChannel = channel;
            this.minumumLogLevel = level;

            this.jsonSerializationSettings = new JsonSerializerSettings
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
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            this.jsonSerializationSettings.Converters.Add(new ContextPropertiesJsonConverter());
        }

        /// <summary>
        /// Gets or sets true/false whether channel event transmission diagnostics is
        /// enabled.
        /// </summary>
        public bool DiagnosticsEnabled
        {
            get
            {
                return this.underlyingTelemetryChannel.DiagnosticsEnabled;
            }
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <summary>
        /// Flushes buffered content from the logger instance.
        /// </summary>
        /// <param name="timeout">A timeout for the flush operation. Default = <see cref="EventHubTelemetryLogger.DefaultTransmissionInterval"/></param>
        /// <returns>
        /// A task that can be used to flush buffered content from the logger
        /// instance.
        /// </returns>
        public void Flush(TimeSpan? timeout = null)
        {
            this.underlyingTelemetryChannel.Flush(timeout ?? EventHubTelemetryLogger.DefaultTransmissionInterval);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= this.minumumLogLevel;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                if (!this.IsEnabled(logLevel))
                {
                    return;
                }

                EventData eventData = this.CreateEvent(logLevel, eventId, state, exception, formatter);
                this.AddEventDataToChannel(eventData);
            }
            catch (Exception exc)
            {
                this.LogError(exc);
            }
        }

        /// <summary>
        /// Adds Event Data to the underlying channel.
        /// </summary>
        protected virtual void AddEventDataToChannel(EventData eventData)
        {
            this.underlyingTelemetryChannel.Add(eventData);
        }

        /// <summary>
        /// Creates an <see cref="EventData"/> object with the telemetry information to emit to 
        /// the target Event Hub.
        /// </summary>
        /// <param name="eventMessage">The message to set for the event data object.</param>
        /// <param name="logLevel">The severity level of the logged event.</param>
        /// <param name="eventTimestamp">A timestamp for the event.</param>
        /// <param name="eventContext">Provides additional context information to include in the event data object.</param>
        /// <param name="bufferInfo">Provides information on the current buffered messages count/state to include in the event data object.</param>
        /// <returns></returns>
        protected virtual EventData CreateEventObject(string eventMessage, LogLevel logLevel, DateTime eventTimestamp, EventContext eventContext, object bufferInfo = null)
        {
            // Allow for specific property overrides.
            eventContext.Properties.TryGetValue(MetadataContract.AppHost, out object appHost);
            eventContext.Properties.TryGetValue(MetadataContract.AppName, out object appName);
            eventContext.Properties.TryGetValue(MetadataContract.AppVersion, out object appVersion);

            DateTime timestamp = eventTimestamp;
            if (eventContext.Properties.TryGetValue(MetadataContract.Timestamp, out object datetime))
            {
                DateTime.TryParse(datetime?.ToString(), out timestamp);
            }

            if (timestamp.Kind == DateTimeKind.Local)
            {
                timestamp = timestamp.ToUniversalTime();
            }

            var eventObject = new
            {
                appName = appName ?? EventHubTelemetryLogger.executingAssembly.Name,
                appHost = appHost ?? Environment.MachineName,
                itemType = logLevel.ToString().ToLowerInvariant(),
                severityLevel = (int)logLevel,
                message = eventMessage,
                timestamp = timestamp.ToString("o"),
                operation_Id = eventContext?.ActivityId.ToString() ?? string.Empty,
                operation_ParentId = eventContext?.ParentActivityId.ToString() ?? string.Empty,
                sdkVersion = appVersion ?? EventHubTelemetryLogger.loggingAssembly.Version.ToString(),
                customDimensions = eventContext != null ? EventHubTelemetryLogger.GetContextProperties(eventContext, bufferInfo) : null,
            };

            return new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventObject, this.jsonSerializationSettings)));
        }

        private static IEnumerable<KeyValuePair<string, object>> GetContextProperties(EventContext context, object bufferInfo = null)
        {
            Dictionary<string, object> contextProperties = new Dictionary<string, object>
            {
                { "transactionId", context.TransactionId },
                { "durationMs", context.DurationMs }
            };

            if (bufferInfo != null)
            {
                contextProperties.Add(nameof(bufferInfo), bufferInfo);
            }

            if (context.Properties?.Any() == true)
            {
                contextProperties.AddRange(context.Properties.Select(prop => new KeyValuePair<string, object>(prop.Key, prop.Value))
                    .OrderBy(entry => entry.Key));
            }

            return contextProperties;
        }

        private EventData CreateEvent<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
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
                    eventMessage = $"{exception.ToString(withCallStack: false, withErrorTypes: true)} {state.ToString()}";
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

            object bufferInfo = null;
            if (this.DiagnosticsEnabled)
            {
                EventHubTelemetryChannel.ChannelDiagnostics diagnostics = this.underlyingTelemetryChannel.Diagnostics;

                bufferInfo = new
                {
                    bufferedEvents = this.underlyingTelemetryChannel.BufferCount,
                    eventsExpected = diagnostics?.EventsExpected(),
                    eventsTransmitted = diagnostics?.EventsTransmitted(),
                    eventTransmissionFailures = diagnostics?.EventsTransmissionFailed(),
                    eventsDropped = diagnostics?.EventsDropped()
                };
            }

            EventData eventData = this.CreateEventObject(eventMessage, logLevel, DateTime.UtcNow, eventContext, bufferInfo);
            if (eventData.Body.Length > EventHubTelemetryChannel.MaxEventDataBytes)
            {
                EventContext scaledDownContext = eventContext.Clone(withProperties: false)
                    .AddContext("exceededSizeLimits", bool.TrueString);

                eventData = this.CreateEventObject(eventMessage, logLevel, DateTime.UtcNow, scaledDownContext, bufferInfo);
            }

            return eventData;
        }

        private void LogError(Exception exception)
        {
            try
            {
                EventContext errorContext = new EventContext(Guid.NewGuid())
                    .AddError(exception);

                EventData eventData = this.CreateEvent(LogLevel.Error, new EventId(911, $"{nameof(EventHubTelemetryLogger)}.LoggingError"), errorContext, null, null);
                this.AddEventDataToChannel(eventData);
            }
            catch
            {
                // If we failed to create/log the event AND we fail to create/log the error event, we do not let
                // the error surface. This is a best-effort process and we do not want to cause issues for the
                // upstream callers due to telemetry logging issues.
            }
        }
    }
}