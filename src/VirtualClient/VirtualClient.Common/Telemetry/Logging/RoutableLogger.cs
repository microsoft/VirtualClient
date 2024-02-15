// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation that determines whether to log messages
    /// based upon a filter function applied to the context of the message.
    /// </summary>
    internal class RoutableLogger : ILogger, IFlushableChannel
    {
        private ILogger routingEnabledLogger;
        private Func<EventId, LogLevel, object, bool> loggerRoutingFunction;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutableLogger"/> class.
        /// </summary>
        /// <param name="baseLogger">The base logger to which the filter function will be applied.</param>
        /// <param name="filterFunction">
        /// The filter function that defines whether the base logger will log a message or not.  The filter
        /// function expects an <see cref="EventId"/> and the TState parameter of the message.
        /// </param>
        public RoutableLogger(ILogger baseLogger, Func<EventId, LogLevel, object, bool> filterFunction)
        {
            this.routingEnabledLogger = baseLogger;
            this.loggerRoutingFunction = filterFunction ?? new Func<EventId, LogLevel, object, bool>((eventId, logLevel, state) => true);
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return this.routingEnabledLogger.BeginScope(state);
        }

        /// <summary>
        /// Flushes buffered content from the logger instance.
        /// </summary>
        /// <param name="timeout">A timeout for the flush operation. Default = 30 seconds.</param>
        /// <returns>
        /// A task that can be used to flush buffered content from the logger
        /// instance.
        /// </returns>
        public void Flush(TimeSpan? timeout = null)
        {
            (this.routingEnabledLogger as IFlushableChannel)?.Flush(timeout);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return this.routingEnabledLogger.IsEnabled(logLevel);
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (this.loggerRoutingFunction.Invoke(eventId, logLevel, state))
            {
                this.routingEnabledLogger.Log(logLevel, eventId, state, exception, formatter);
            }
        }
    }
}