// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// A logger that uploads telemetry messages/events to a proxy API endpoint.
    /// </summary>
    internal class ProxyLogger : ILogger
    {
        private static AssemblyName sdkAssembly = Assembly.GetAssembly(typeof(EventContext)).GetName();

        private static Dictionary<LogType, string> eventTypeMappings = new Dictionary<LogType, string>
        {
            { LogType.Undefined, "Traces" },
            { LogType.Error, "Errors" },
            { LogType.Trace, "Traces" },
            { LogType.Metric, "Metrics" },
            { LogType.SystemEvent, "Events" }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLogger"/> class.
        /// </summary>
        /// <param name="channel">The background channel used to upload telemetry through a proxy endpoint.</param>
        /// <param name="source">Defines an explicit source to use for telemetry uploads.</param>
        public ProxyLogger(ProxyTelemetryChannel channel, string source = null)
        {
            channel.ThrowIfNull(nameof(channel));

            this.Channel = channel;
            this.Source = !string.IsNullOrWhiteSpace(source)
                ? source
                : "VirtualClient";
        }

        /// <summary>
        /// The background channel used to upload telemetry through a proxy endpoint.
        /// </summary>
        protected ProxyTelemetryChannel Channel { get; }

        /// <summary>
        /// Defines an explicit source to use for telemetry uploads.
        /// </summary>
        protected string Source { get; }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <summary>
        /// Returns true always.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Uploads the telemetry message/event details to the proxy API endpoint.
        /// </summary>
        /// <typeparam name="TState">The data type of the event state/context object (e.g. <see cref="EventContext"/>).</typeparam>
        /// <param name="logLevel">The severity level for the message/event.</param>
        /// <param name="eventId">Identifying information for the message/event.</param>
        /// <param name="state">The event state/context object.</param>
        /// <param name="exception">An error to upload.</param>
        /// <param name="formatter">Not used.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                if (!ProxyLogger.eventTypeMappings.TryGetValue((LogType)eventId.Id, out string eventType))
                {
                    eventType = ProxyLogger.eventTypeMappings[LogType.Undefined];
                }

                ProxyTelemetryMessage message = new ProxyTelemetryMessage
                {
                    Source = this.Source,
                    EventType = eventType,
                    Message = eventId.Name,
                    ItemType = "trace",
                    SeverityLevel = logLevel,
                    AppHost = Environment.MachineName,
                    AppName = "VirtualClient",
                    SdkVersion = ProxyLogger.sdkAssembly.Version.ToString(),
                };

                EventContext context = state as EventContext;

                if (context != null)
                {
                    message.OperationId = context.ActivityId.ToString();
                    message.OperationParentId = context.ParentActivityId.ToString();
                    message.CustomDimensions = new Dictionary<string, object>(context.Properties, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    message.OperationId = Guid.Empty.ToString();
                    message.OperationParentId = Guid.Empty.ToString();
                }

                this.Channel.Add(message);
            }
        }
    }
}