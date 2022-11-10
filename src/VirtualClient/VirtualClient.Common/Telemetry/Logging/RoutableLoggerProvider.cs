// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Specialized logger provider enables the creations of <see cref="ILogger"/> instances
    /// whose logging can be controlled by a filter applied to the context of individual messages.
    /// </summary>
    internal sealed class RoutableLoggerProvider : ILoggerProvider
    {
        private ILoggerProvider baseLoggerProvider;
        private Func<EventId, LogLevel, object, bool> logFilterFunction;

        /// <summary>
        /// Constructor for RoutableLoggerProvider.
        /// </summary>
        /// <param name="loggerProvider">
        /// The base logging provider that will enable routing on <see cref="ILogger"/> instances it creates.
        /// </param>
        /// <param name="filterFunction">
        /// The function to determine true or false whether <see cref="ILogger"/> instances
        /// created by the provider should log individual messages based upon the context of the message
        /// (e.g. (eventId, state) => state is <see cref="EventContext"/>} ).
        /// </param>
        public RoutableLoggerProvider(ILoggerProvider loggerProvider, Func<EventId, object, bool> filterFunction)
        {
            loggerProvider.ThrowIfNull(nameof(loggerProvider));
            filterFunction.ThrowIfNull(nameof(filterFunction));

            this.baseLoggerProvider = loggerProvider;
            this.logFilterFunction = filterFunction != null
                ? new Func<EventId, LogLevel, object, bool>((eventId, logLevel, state) => filterFunction.Invoke(eventId, state))
                : new Func<EventId, LogLevel, object, bool>((eventId, logLevel, state) => true);
        }

        /// <summary>
        /// Constructor for RoutableLoggerProvider.
        /// </summary>
        /// <param name="loggerProvider">
        /// The base logging provider that will enable routing on <see cref="ILogger"/> instances it creates.
        /// </param>
        /// <param name="filterFunction">
        /// The function to determine true or false whether <see cref="ILogger"/> instances
        /// created by the provider should log individual messages based upon the context of the message
        /// (e.g. (eventId, state) => state is <see cref="EventContext"/>} ).
        /// </param>
        public RoutableLoggerProvider(ILoggerProvider loggerProvider, Func<EventId, LogLevel, object, bool> filterFunction)
        {
            loggerProvider.ThrowIfNull(nameof(loggerProvider));
            filterFunction.ThrowIfNull(nameof(filterFunction));

            this.baseLoggerProvider = loggerProvider;
            this.logFilterFunction = filterFunction;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance that logs events/messages based upon the
        /// results of applying a filter function to the context of individual events/messages.
        /// </summary>
        /// <param name="categoryName">The logger/event category.</param>
        /// <returns>
        /// An <see cref="ILogger"/> instance that applies a filter function to the context
        /// of events/messages to determine whether to log them.
        /// </returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new RoutableLogger(
                this.baseLoggerProvider.CreateLogger(categoryName),
                this.logFilterFunction);
        }

        /// <summary>
        /// Disposes the underlying resources in the provider.
        /// </summary>
        public void Dispose()
        {
            this.baseLoggerProvider?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}