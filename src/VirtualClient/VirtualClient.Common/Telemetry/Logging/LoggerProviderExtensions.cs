// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ILoggerProvider"/> instances that
    /// write telemetry.
    /// </summary>
    public static class LoggerProviderExtensions
    {
        /// <summary>
        /// Extension wraps the logger provider in a provider that applies a routing/filter function
        /// to <see cref="ILogger"/> instances.
        /// </summary>
        /// <param name="provider">The provider to apply the routing/filter function.</param>
        /// <param name="filterFunction">The routing/filter function.</param>
        /// <returns>
        /// A <see cref="RoutableLoggerProvider"/> instance that creates <see cref="ILogger"/> instances
        /// bound by a routing/filter function.
        /// </returns>
        public static ILoggerProvider WithFilter(this ILoggerProvider provider, Func<EventId, object, bool> filterFunction)
        {
            return new RoutableLoggerProvider(provider, filterFunction);
        }

        /// <summary>
        /// Extension wraps the logger provider in a provider that applies a routing/filter function
        /// to <see cref="ILogger"/> instances.
        /// </summary>
        /// <param name="provider">The provider to apply the routing/filter function.</param>
        /// <param name="filterFunction">The routing/filter function.</param>
        /// <returns>
        /// A <see cref="RoutableLoggerProvider"/> instance that creates <see cref="ILogger"/> instances
        /// bound by a routing/filter function.
        /// </returns>
        public static ILoggerProvider WithFilter(this ILoggerProvider provider, Func<EventId, LogLevel, object, bool> filterFunction)
        {
            return new RoutableLoggerProvider(provider, filterFunction);
        }

        /// <summary>
        /// Extension configures the logger provider to create loggers that route telemetry
        /// events to <see cref="ILogger"/> instances that specialize in logging telemetry.
        /// </summary>
        /// <param name="provider">The providers in which to apply telemetry routing.</param>
        /// <returns>
        /// A set of <see cref="ILoggerProvider"/> instances that will apply a telemetry routing/function
        /// to <see cref="ILogger"/> instances they create.
        /// </returns>
        public static ILoggerProvider WithTelemetryRouting(this ILoggerProvider provider)
        {
            provider.ThrowIfNull(nameof(provider));

            ILoggerProvider routableProvider = null;
            LoggerSpecializationAttribute specializationAttribute = provider.GetType()
                .GetCustomAttributes(typeof(LoggerSpecializationAttribute), true)?
                .FirstOrDefault(attr => attr is LoggerSpecializationAttribute) as LoggerSpecializationAttribute;

            if (specializationAttribute != null && specializationAttribute.Name == SpecializationConstant.Telemetry)
            {
                routableProvider = provider.WithFilter((eventId, state) => state is EventContext);
            }
            else
            {
                routableProvider = provider.WithFilter((eventId, state) => !(state is EventContext));
            }

            return routableProvider;
        }

        /// <summary>
        /// Extension configures the set of logger providers to create loggers that route telemetry
        /// events to <see cref="ILogger"/> instances that specialize in logging telemetry.
        /// </summary>
        /// <param name="providers">The set of providers in which to apply telemetry routing.</param>
        /// <returns>
        /// A set of <see cref="ILoggerProvider"/> instances that will apply a telemetry routing/function
        /// to <see cref="ILogger"/> instances they create.
        /// </returns>
        public static IEnumerable<ILoggerProvider> WithTelemetryRouting(this IEnumerable<ILoggerProvider> providers)
        {
            providers.ThrowIfNull(nameof(providers));

            List<ILoggerProvider> routableProviders = new List<ILoggerProvider>();
            foreach (ILoggerProvider provider in providers)
            {
                routableProviders.Add(provider.WithTelemetryRouting());
            }

            return routableProviders;
        }
    }
}