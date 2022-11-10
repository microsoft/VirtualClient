// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for logging telemetry event information
    /// using an EventSource provider.
    /// </summary>
    [LoggerSpecialization(Name = SpecializationConstant.Telemetry)]
    public class EventSourceLogger : ILogger
    {
        private static readonly Dictionary<LogLevel, EventLevel> SeverityMap = new Dictionary<LogLevel, EventLevel>
        {
            [LogLevel.Critical] = EventLevel.Critical,
            [LogLevel.Debug] = EventLevel.Verbose,
            [LogLevel.Error] = EventLevel.Error,
            [LogLevel.Information] = EventLevel.Informational,
            [LogLevel.None] = EventLevel.Verbose,
            [LogLevel.Trace] = EventLevel.Verbose,
            [LogLevel.Warning] = EventLevel.Warning
        };

        private readonly EventSource telemetryEventSource;

        /// <summary>
        /// Initializes an instance of the <see cref="EventSourceLogger"/> class
        /// </summary>
        /// <param name="eventSource">The EventSource definition for which the ETW event data is associated.</param>
        public EventSourceLogger(EventSource eventSource)
        {
            this.telemetryEventSource = eventSource ?? throw new ArgumentNullException(nameof(eventSource));
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // ETW traces require an event name. If the event name is not provided, the logger will ignore
            // the log request.
            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                this.WriteEvent(logLevel, eventId, state, exception);
            }
        }

        /// <summary>
        /// Constrains the size of the <see cref="TelemetryEventData"/> object to be within the limits of an
        /// ETW event (65,356 bytes).
        /// </summary>
        /// <param name="eventData">The <see cref="TelemetryEventData"/> object to constrain.</param>
        /// <param name="originatingContext">
        /// The originating <see cref="EventContext"/> object from which the <see cref="TelemetryEventData"/> object was created
        /// and that contains the context properties that will be constrained in size.
        /// </param>
        /// <returns>
        /// A <see cref="TelemetryEventData"/> object whose size will be within the limits of a single ETW event.
        /// </returns>
        protected static TelemetryEventData ConstrainSizeWithinEtwLimits(TelemetryEventData eventData, EventContext originatingContext)
        {
            eventData.ThrowIfNull(nameof(eventData));
            originatingContext.ThrowIfNull(nameof(originatingContext));

            TelemetryEventData constrainedEventData = eventData.Clone();
            constrainedEventData.PayloadModifications = ContextModifications.ConstrainPayload;

            if (eventData.EventSize > TelemetryEventData.MaxEtwEventSizeInBytes)
            {
                try
                {
                    // Size of context - size of properties other than context (e.g. TransactionId, User etc....)
                    int maxSizeOfContextInBytes = TelemetryEventData.MaxEtwEventPayloadSizeInBytes - eventData.GetPayloadSizeInBytes(withContext: false);
                    int maxSizeOfContentInChars = Encoding.Unicode.GetByteCount("a") / maxSizeOfContextInBytes;
                    string constrainedContext = JsonContextSerialization.Serialize(originatingContext.Properties, maxSizeOfContentInChars);
                    constrainedEventData.Context = constrainedContext;
                }
                catch (Exception ex)
                {
                    constrainedEventData.PayloadModifications |= ContextModifications.ConstrainPayloadFailed;

                    // Diagnostics should never throw. If this occurs, we replace the payload with an appropriate error message.
                    string payloadConstraintErrorMessage = $"Constraining the event payload size failed!: {ex.ToString(withCallStack: false, withErrorTypes: true)}.";
                    payloadConstraintErrorMessage += Environment.NewLine;
                    payloadConstraintErrorMessage += $"Original Context: {constrainedEventData.Context}";

                    int contextSize = constrainedEventData.Context == null
                        ? 0
                        : Encoding.Unicode.GetByteCount(constrainedEventData.Context);

                    int payloadSizeWithoutContext = constrainedEventData.GetPayloadSizeInBytes() - contextSize;
                    int allowableContextSize = TelemetryEventData.MaxEtwEventPayloadSizeInBytes - payloadSizeWithoutContext;
                    int lengthLimit = Math.Min(payloadConstraintErrorMessage.Length, allowableContextSize / sizeof(char));

                    constrainedEventData.Context = payloadConstraintErrorMessage.Substring(0, lengthLimit);
                }
            }

            return constrainedEventData;
        }

        private void WriteEvent<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception)
        {
            EventContext eventContext = state as EventContext;

            if (eventContext == null)
            {
                eventContext = new EventContext(Guid.NewGuid());
            }

            if (exception != null)
            {
                eventContext.AddContext("error", exception.ToString(withCallStack: true, withErrorTypes: true));
            }

            Guid activityId = eventContext.ActivityId;
            Guid relatedActivityId = eventContext.ParentActivityId;

            EventLevel level;
            if (!EventSourceLogger.SeverityMap.TryGetValue(logLevel, out level))
            {
                level = EventLevel.Verbose;
            }

            EventSourceOptions eventOptions = new EventSourceOptions() { Level = level };
            TelemetryEventData eventData = eventContext.ToEventData();
            if (eventData.EventSize > TelemetryEventData.MaxEtwEventSizeInBytes)
            {
                eventData = EventSourceLogger.ConstrainSizeWithinEtwLimits(eventData, eventContext);
            }

            try
            {
                this.WriteEvent(eventId.Name, ref eventOptions, ref activityId, ref relatedActivityId, ref eventData);
            }
            catch (EventSourceException exc) when (exc.Message.Contains("The payload for a single event is too large.", StringComparison.OrdinalIgnoreCase))
            {
                // We are capturing this exception with this exact text because this is the ONLY way we can distinguish this
                // exception from the other EventSourceExceptions that might be thrown.  All exceptions thrown by the EventSource
                // class are unfortunately EventSourceExceptions.

                // Try to capture the event data without the context/payload so that we don't lose everything.
                TelemetryEventData constrainedEventData = new TelemetryEventData
                {
                    DurationMs = eventData.DurationMs,
                    PayloadModifications = ContextModifications.ConstrainPayload,
                    TransactionId = eventData.TransactionId,
                    User = eventData.User,
                };

                this.WriteEvent(eventId.Name, ref eventOptions, ref activityId, ref relatedActivityId, ref constrainedEventData);
            }
        }

        private void WriteEvent(string eventName, ref EventSourceOptions options, ref Guid activityId, ref Guid relatedActivityId, ref TelemetryEventData eventData)
        {
            EventSource.SetCurrentThreadActivityId(activityId);
            this.telemetryEventSource.Write(eventName, ref options, ref activityId, ref relatedActivityId, ref eventData);
        }
    }
}