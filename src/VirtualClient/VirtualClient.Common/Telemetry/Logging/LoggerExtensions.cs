// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ILogger"/> instances that
    /// write telemetry.
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Extension logs the telemetry event and context.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventName">The name of the telemetry event.</param>
        /// <param name="level">The severity level of the telemetry event.</param>
        /// <param name="context">Context associated with the telemetry event.</param>
        public static void LogTelemetry(this ILogger logger, string eventName, LogLevel level, EventContext context)
        {
            logger.ThrowIfNull(nameof(logger));
            eventName.ThrowIfNullOrWhiteSpace(nameof(eventName));
            context.ThrowIfNull(nameof(context));

            logger.Log(level, new EventId(eventName.GetHashCode(StringComparison.OrdinalIgnoreCase), eventName), context, null, null);
        }

        /// <summary>
        /// Extension logs the telemetry event and context.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventId">The ID of the event.</param>
        /// <param name="level">The severity level of the telemetry event.</param>
        /// <param name="context">Context associated with the telemetry event.</param>
        public static void LogTelemetry(this ILogger logger, EventId eventId, LogLevel level, EventContext context)
        {
            logger.ThrowIfNull(nameof(logger));
            context.ThrowIfNull(nameof(context));

            logger.Log(level, eventId, context, null, null);
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body
        /// (e.g. UpdateItemStart, UpdateItemStop)..
        /// </summary>
        /// <param name="logger">The logger handling the event.</param>
        /// <param name="eventNameBase">
        /// The root/base name of the event. The suffix Start, Stop or Error will be appended to this
        /// (e.g. EventName -> EventNameStart, EventNameStop).
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <param name="samplingOptions">
        /// Options/settings to used when applying a sampling mechanic to the capture of telemetry
        /// </param>
        public static void LogTelemetry(this ILogger logger, string eventNameBase, EventContext context, Action body, SamplingOptions samplingOptions = null)
        {
            logger.ThrowIfNull(nameof(logger));
            eventNameBase.ThrowIfNullOrWhiteSpace(nameof(eventNameBase));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            Func<int> bodyWrapper = () =>
            {
                body.Invoke();
                return 0;
            };

            LoggerExtensions.LogTelemetry(logger, eventNameBase, context, bodyWrapper, samplingOptions);
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body
        /// (e.g. UpdateItemStart, UpdateItemStop)..
        /// </summary>
        /// <param name="logger">The logger handling the event.</param>
        /// <param name="eventId">
        /// The ID of the event. The name or ID defined will be used as the prefix for the Start/Stop events
        /// (e.g. Name=EventName -> EventNameStart, EventNameStop, ID=123 -> 123Start, 123Stop). The name defined takes
        /// priority over the ID as the prefix.
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        public static void LogTelemetry(this ILogger logger, EventId eventId, EventContext context, Action body)
        {
            logger.ThrowIfNull(nameof(logger));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            Func<int> bodyWrapper = () =>
            {
                body.Invoke();
                return 0;
            };

            LoggerExtensions.LogTelemetry(logger, eventId, context, bodyWrapper);
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body (e.g. UpdateItemStart, UpdateItemStop
        /// and returns the result of the method/action body invocation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventNameBase">
        /// The root/base name of the event. The suffix Start, Stop or Error will be appended to this
        /// (e.g. EventName -> EventNameStart, EventNameStop).
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <param name="samplingOptions">
        /// Options/settings to used when applying a sampling mechanic to the capture of telemetry
        /// </param>
        public static TResult LogTelemetry<TResult>(this ILogger logger, string eventNameBase, EventContext context, Func<TResult> body, SamplingOptions samplingOptions = null)
        {
            logger.ThrowIfNull(nameof(logger));
            eventNameBase.ThrowIfNullOrWhiteSpace(nameof(eventNameBase));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            body.ThrowIfInvalid(
                nameof(body),
                (b) => !typeof(Task).IsAssignableFrom(typeof(TResult)),
                $"{nameof(ILogger)}.{nameof(LogTelemetry)} called with body/delegate that returns a {nameof(Task)}. Use {nameof(ILogger)}.{nameof(LogTelemetryAsync)} instead.");

            try
            {
                if (samplingOptions != null && !samplingOptions.Sample)
                {
                    return body();
                }
                else
                {
                    Stopwatch executionTime = Stopwatch.StartNew();

                    try
                    {
                        LoggerExtensions.LogStartEvent(logger, eventNameBase, context);

                        return body();
                    }
                    catch (Exception exc)
                    {
                        LoggerExtensions.LogErrorEvent(logger, eventNameBase, context, exc);
                        throw;
                    }
                    finally
                    {
                        executionTime.Stop();
                        LoggerExtensions.LogStopEvent(logger, eventNameBase, context, executionTime.ElapsedMilliseconds);
                    }
                }
            }
            finally
            {
                if (samplingOptions != null)
                {
                    samplingOptions.EventCount++;
                }
            }
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body (e.g. UpdateItemStart, UpdateItemStop
        /// and returns the result of the method/action body invocation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventId">
        /// The ID of the event. The name or ID defined will be used as the prefix for the Start/Stop events
        /// (e.g. Name=EventName -> EventNameStart, EventNameStop, ID=123 -> 123Start, 123Stop). The name defined takes
        /// priority over the ID as the prefix.
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        public static TResult LogTelemetry<TResult>(this ILogger logger, EventId eventId, EventContext context, Func<TResult> body)
        {
            logger.ThrowIfNull(nameof(logger));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            body.ThrowIfInvalid(
                nameof(body),
                (b) => !typeof(Task).IsAssignableFrom(typeof(TResult)),
                $"{nameof(ILogger)}.{nameof(LogTelemetry)} called with body/delegate that returns a {nameof(Task)}. Use {nameof(ILogger)}.{nameof(LogTelemetryAsync)} instead.");

            return LoggerExtensions.LogTelemetry(logger, eventId.Name ?? eventId.Id.ToString(), context, body);
        }

        /// <summary>
        /// Extension logs the telemetry event and context.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventName">The name of the telemetry event.</param>
        /// <param name="level">The severity level of the telemetry event.</param>
        /// <param name="context">Context associated with the telemetry event.</param>
        public static Task LogTelemetryAsync(this ILogger logger, string eventName, LogLevel level, EventContext context)
        {
            logger.ThrowIfNull(nameof(logger));
            eventName.ThrowIfNullOrWhiteSpace(nameof(eventName));
            context.ThrowIfNull(nameof(context));

            logger.Log(level, new EventId(eventName.GetHashCode(StringComparison.OrdinalIgnoreCase), eventName), context, null, null);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body
        /// (e.g. UpdateItemStart, UpdateItemStop).
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventNameBase">
        /// The root/base name of the event. The suffix Start, Stop or Error will be appended to this
        /// (e.g. EventName -> EventNameStart, EventNameStop).
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <param name="samplingOptions">
        /// Options/settings to used when applying a sampling mechanic to the capture of telemetry
        /// </param>
        /// <returns>
        /// A task that can be used to asynchronously execute the method/action body while publishing telemetry events.
        /// </returns>
        public static Task LogTelemetryAsync(this ILogger logger, string eventNameBase, EventContext context, Func<Task> body, SamplingOptions samplingOptions = null)
        {
            logger.ThrowIfNull(nameof(logger));
            eventNameBase.ThrowIfNullOrWhiteSpace(nameof(eventNameBase));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            Func<Task<int>> wrapper = async () =>
            {
                await body().ConfigureAwait(false);
                return 0;
            };

            return LoggerExtensions.LogTelemetryAsync(logger, eventNameBase, context, wrapper, samplingOptions);
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body
        /// (e.g. UpdateItemStart, UpdateItemStop).
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventId">
        /// The ID of the event. The name or ID defined will be used as the prefix for the Start/Stop events
        /// (e.g. Name=EventName -> EventNameStart, EventNameStop, ID=123 -> 123Start, 123Stop). The name defined takes
        /// priority over the ID as the prefix.
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <returns>
        /// A task that can be used to asynchronously execute the method/action body while publishing telemetry events.
        /// </returns>
        public static Task LogTelemetryAsync(this ILogger logger, EventId eventId, EventContext context, Func<Task> body)
        {
            logger.ThrowIfNull(nameof(logger));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            Func<Task<int>> wrapper = async () =>
            {
                await body().ConfigureAwait(false);
                return 0;
            };

            return LoggerExtensions.LogTelemetryAsync(logger, eventId, context, wrapper);
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body (e.g. UpdateItemStart, UpdateItemStop
        /// and returns the result of the method/action body invocation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventNameBase">
        /// The root/base name of the event. The suffix Start, Stop or Error will be appended to this
        /// (e.g. EventName -> EventNameStart, EventNameStop).
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <param name="samplingOptions">
        /// Options/settings to used when applying a sampling mechanic to the capture of telemetry
        /// </param>
        /// <returns>
        /// A task that can be used to asynchronously execute the method/action body while publishing telemetry events.
        /// </returns>
        public static async Task<TResult> LogTelemetryAsync<TResult>(this ILogger logger, string eventNameBase, EventContext context, Func<Task<TResult>> body, SamplingOptions samplingOptions = null)
        {
            logger.ThrowIfNull(nameof(logger));
            eventNameBase.ThrowIfNullOrWhiteSpace(nameof(eventNameBase));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            body.ThrowIfInvalid(
                nameof(body),
                (b) => !typeof(Task).IsAssignableFrom(typeof(TResult)),
                $"{nameof(ILogger)}.{nameof(LogTelemetry)} called with body/delegate that returns a {nameof(Task)}. Use {nameof(ILogger)}.{nameof(LogTelemetryAsync)} instead.");

            try
            {
                if (samplingOptions != null && !samplingOptions.Sample)
                {
                    return await body().ConfigureAwait(false);
                }
                else
                {
                    Stopwatch executionTime = Stopwatch.StartNew();

                    try
                    {
                        LoggerExtensions.LogStartEvent(logger, eventNameBase, context);
                        return await body().ConfigureAwait(false);
                    }
                    catch (Exception exc)
                    {
                        LoggerExtensions.LogErrorEvent(logger, eventNameBase, context, exc);
                        throw;
                    }
                    finally
                    {
                        executionTime.Stop();
                        LoggerExtensions.LogStopEvent(logger, eventNameBase, context, executionTime.ElapsedMilliseconds);
                    }
                }
            }
            finally
            {
                if (samplingOptions != null)
                {
                    samplingOptions.EventCount++;
                }
            }
        }

        /// <summary>
        /// Extension logs Start/Stop/Error telemetry events for the method/action body (e.g. UpdateItemStart, UpdateItemStop
        /// and returns the result of the method/action body invocation.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="eventId">
        /// The ID of the event. The name or ID defined will be used as the prefix for the Start/Stop events
        /// (e.g. Name=EventName -> EventNameStart, EventNameStop, ID=123 -> 123Start, 123Stop). The name defined takes
        /// priority over the ID as the prefix.
        /// </param>
        /// <param name="context">Provides context identifiers and properties associated with the event.</param>
        /// <param name="body">Defines a body of logic to execute.</param>
        /// <returns>
        /// A task that can be used to asynchronously execute the method/action body while publishing telemetry events.
        /// </returns>
        public static Task<TResult> LogTelemetryAsync<TResult>(this ILogger logger, EventId eventId, EventContext context, Func<Task<TResult>> body)
        {
            logger.ThrowIfNull(nameof(logger));
            context.ThrowIfNull(nameof(context));
            body.ThrowIfNull(nameof(body));

            return LoggerExtensions.LogTelemetryAsync(logger, eventId.Name ?? eventId.Id.ToString(), context, body);
        }

        private static void LogErrorEvent(ILogger logger, string eventNameBase, EventContext context, Exception exc)
        {
            string eventName = $"{eventNameBase}{EventNameSuffix.Error}";
            context.AddError(exc, withCallStack: true);
            logger.Log(LogLevel.Error, new EventId(eventName.GetHashCode(StringComparison.OrdinalIgnoreCase), eventName), context, null, null);
        }

        private static void LogStartEvent(ILogger logger, string eventNameBase, EventContext context)
        {
            string eventName = $"{eventNameBase}{EventNameSuffix.Start}";
            logger.Log(LogLevel.Information, new EventId(eventName.GetHashCode(StringComparison.OrdinalIgnoreCase), eventName), context, null, null);
        }

        private static void LogStopEvent(ILogger logger, string eventNameBase, EventContext context, long durationMs)
        {
            context.DurationMs = durationMs;
            string eventName = $"{eventNameBase}{EventNameSuffix.Stop}";
            logger.Log(LogLevel.Information, new EventId(eventName.GetHashCode(StringComparison.OrdinalIgnoreCase), eventName), context, null, null);
        }
    }
}