// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Some logging hellper methods specific to virtual client
    /// </summary>
    public static class VirtualClientLoggingExtensions
    {
        private static readonly Regex Base64Expression = new Regex("[\x30-\x39\x41-\x5A\x61-\x7A\x2B\x2F]+");

        /// <summary>
        /// Adds the profile parameters to the telemetry event context.
        /// </summary>
        public static EventContext AddParameters<TValue>(this EventContext context, IDictionary<string, TValue> parameters, string name = "parameters")
            where TValue : class
        {
            context.ThrowIfNull(nameof(context));
            parameters.ThrowIfNull(nameof(parameters));

            context.Properties[name] = parameters.ObscureSecrets();

            return context;
        }

        /// <summary>
        /// Returns a camel-cased version of the property name.
        /// </summary>
        /// <param name="propertyName">The property name to camel-case.</param>
        public static string CamelCased(this string propertyName)
        {
            // We are not trying to get overly fancy here. We just make sure the first character
            // is lower-cased and leave it at that.
            return $"{propertyName.Substring(0, 1).ToLowerInvariant()}{propertyName.Substring(1)}";
        }

        /// <summary>
        /// Extension logs the information and context.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        public static void LogMessage(this ILogger logger, string message, EventContext eventContext)
        {
            logger.ThrowIfNull(nameof(logger));
            VirtualClientLoggingExtensions.LogMessage(logger, message, LogLevel.Information, LogType.Trace, eventContext);
        }

        /// <summary>
        /// Extension logs the information and context.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="level">The severity level of the message/event (e.g. Information, Error).</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        public static void LogMessage(this ILogger logger, string message, LogLevel level, EventContext eventContext)
        {
            logger.ThrowIfNull(nameof(logger));
            VirtualClientLoggingExtensions.LogMessage(logger, message, level, LogType.Trace, eventContext);
        }

        /// <summary>
        /// Extension logs the information and context.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="level">The severity level of the message/event (e.g. Information, Error).</param>
        /// <param name="logType">The type of data represented by the message/event (e.g. Trace, TestMetric, SystemEvent).</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        public static void LogMessage(this ILogger logger, string message, LogLevel level, LogType logType, EventContext eventContext)
        {
            logger.ThrowIfNull(nameof(logger));

            try
            {
                if (eventContext != null)
                {
                    // Note:
                    // The log type is used to route messages to the appropriate logger underneath. In practice, there
                    // are numerous loggers undernath an ILogger instance. For example, the Virtual Client logs to multiple
                    // different Event Hubs depending upon the log type.
                    logger.LogTelemetry(new EventId((int)logType, message), level, eventContext);
                }
                else
                {
                    logger.Log(level, new EventId((int)logType, message), message);
                }
            }
            catch
            {
                // Logging errors should not cause the application to crash.
            }
        }

        /// <summary>
        /// Extension logs standard Start/Stop/Error events for the method logic.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="body">The logic/method body to execute in between the message logging.</param>
        /// <param name="displayErrors">True if the full information for any errors that occur should be displayed on the console.</param>
        public static void LogMessage(this ILogger logger, string message, EventContext eventContext, Action body, bool displayErrors = false)
        {
            logger.ThrowIfNull(nameof(logger));
            eventContext.ThrowIfNull(nameof(eventContext));

            Func<int> bodyWrapper = () =>
            {
                body.Invoke();
                return 0;
            };

            VirtualClientLoggingExtensions.LogMessage(logger, message, eventContext, bodyWrapper, displayErrors);
        }

        /// <summary>
        /// Extension logs standard Start/Stop/Error events for the method logic.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="body">The logic/method body to execute in between the message logging.</param>
        /// <param name="displayErrors">True if the full information for any errors that occur should be displayed on the console.</param>
        public static TResult LogMessage<TResult>(this ILogger logger, string message, EventContext eventContext, Func<TResult> body, bool displayErrors = false)
        {
            logger.ThrowIfNull(nameof(logger));
            eventContext.ThrowIfNull(nameof(eventContext));

            body.ThrowIfInvalid(
                nameof(body),
                (b) => !typeof(Task).IsAssignableFrom(typeof(TResult)),
                $"{nameof(ILogger)}.{nameof(LogMessage)} called with body/delegate that returns a {nameof(Task)}. Use {nameof(ILogger)}.{nameof(LogMessageAsync)} instead.");

            Stopwatch executionTime = Stopwatch.StartNew();
            TResult result = default(TResult);

            try
            {
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Start", LogLevel.Information, eventContext);
                result = body.Invoke();
            }
            catch (OperationCanceledException)
            {
                // Expected for cases where a cancellation token is cancelled.
            }
            catch (Exception exc)
            {
                if (displayErrors)
                {
                    Console.WriteLine(exc.ToDisplayFriendlyString(withCallStack: true));
                }

                EventContext errorContext = eventContext.Clone();
                errorContext.AddError(exc, withCallStack: true, maxCallStackLength: 6000);
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Error", LogLevel.Error, errorContext);
                throw;
            }
            finally
            {
                executionTime.Stop();
                eventContext.DurationMs = executionTime.ElapsedMilliseconds;
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Stop", LogLevel.Information, eventContext);
            }

            return result;
        }

        /// <summary>
        /// Extension logs standard Start/Stop/Error events for the method logic.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="body">The logic/method body to execute in between the message logging.</param>
        /// <param name="displayErrors">True if the full information for any errors that occur should be displayed on the console.</param>
        public static Task LogMessageAsync(this ILogger logger, string message, EventContext eventContext, Func<Task> body, bool displayErrors = false)
        {
            logger.ThrowIfNull(nameof(logger));
            message.ThrowIfNullOrWhiteSpace(nameof(message));
            eventContext.ThrowIfNull(nameof(eventContext));

            Func<Task<int>> wrapper = async () =>
            {
                await body().ConfigureAwait(false);
                return 0;
            };

            return VirtualClientLoggingExtensions.LogMessageAsync(logger, message, eventContext, wrapper, displayErrors);
        }

        /// <summary>
        /// Extension logs standard Start/Stop/Error events for the method logic.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="body">The logic/method body to execute in between the message logging.</param>
        /// <param name="displayErrors">True if the full information for any errors that occur should be displayed on the console.</param>
        public static async Task<TResult> LogMessageAsync<TResult>(this ILogger logger, string message, EventContext eventContext, Func<Task<TResult>> body, bool displayErrors = false)
        {
            logger.ThrowIfNull(nameof(logger));
            eventContext.ThrowIfNull(nameof(eventContext));

            Stopwatch executionTime = Stopwatch.StartNew();
            TResult result = default(TResult);

            try
            {
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Start", LogLevel.Information, eventContext);
                result = await body().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected for cases where a cancellation token is cancelled.
            }
            catch (Exception exc)
            {
                if (displayErrors)
                {
                    // Console.WriteLine(exc.ToDisplayFriendlyString(withCallStack: true));
                }

                EventContext errorContext = eventContext.Clone();
                errorContext.AddError(exc, withCallStack: true, maxCallStackLength: 6000);
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Error", LogLevel.Error, errorContext);
                throw;
            }
            finally
            {
                executionTime.Stop();
                eventContext.DurationMs = executionTime.ElapsedMilliseconds;
                VirtualClientLoggingExtensions.LogMessage(logger, $"{message}Stop", LogLevel.Information, eventContext);
            }

            return result;
        }

        /// <summary>
        /// Extension logs error data to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="error">The error to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="level">The severity level of the message/event (e.g. Information, Error).</param>
        public static void LogErrorMessage(this ILogger logger, Exception error, EventContext eventContext, LogLevel level = LogLevel.Error)
        {
            logger.ThrowIfNull(nameof(logger));
            eventContext.ThrowIfNull(nameof(eventContext));

            if (error != null)
            {
                EventContext errorContext = eventContext.Clone();
                errorContext.AddError(error, withCallStack: true, maxCallStackLength: 6000);
                VirtualClientLoggingExtensions.LogMessage(logger, error.Message, level, LogType.Error, errorContext);
            }
        }

        /// <summary>
        /// Extension logs performance counter data to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="toolName">The name of the tool that captured the counters.</param>
        /// <param name="counters">The performance counters to log.</param>
        /// <param name="startTime">The time at which the performance counter capture process began.</param>
        /// <param name="endTime">The time at which the performance counter capture process ended.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="toolVersion">The version of the tool/toolset.</param>
        public static void LogPerformanceCounters(this ILogger logger, string toolName, IEnumerable<Metric> counters, DateTime startTime, DateTime endTime, EventContext eventContext, string toolVersion = null)
        {
            logger.ThrowIfNull(nameof(logger));
            eventContext.ThrowIfNull(nameof(eventContext));

            if (counters?.Any() == true)
            {
                const string scenarioName = "PerformanceCounter";
                foreach (Metric counter in counters)
                {
                    if (counter != Metric.None)
                    {
                        // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
                        // output. To enable a seamless transition, we are supporting the old and the new schema
                        // until we have all systems using the latest version of the Virtual Client.
                        EventContext counterContext = eventContext.Clone();
                        counterContext.Properties["scenarioName"] = scenarioName;
                        counterContext.Properties["scenarioStartTime"] = startTime;
                        counterContext.Properties["scenarioEndTime"] = endTime;
                        counterContext.Properties["metricName"] = counter.Name;
                        counterContext.Properties["metricValue"] = counter.Value;
                        counterContext.Properties["metricUnit"] = counter.Unit ?? string.Empty;
                        counterContext.Properties["metricDescription"] = counter.Description ?? string.Empty;
                        counterContext.Properties["metricRelativity"] = counter.Relativity;
                        counterContext.Properties["toolName"] = toolName;
                        counterContext.Properties["toolVersion"] = toolVersion;
                        counterContext.Properties["tags"] = counter.Tags != null ? $"{string.Join(",", counter.Tags)}" : string.Empty;
                        counterContext.Properties["metricMetadata"] = counter.Metadata as object;

                        VirtualClientLoggingExtensions.LogMessage(logger, scenarioName, LogLevel.Information, LogType.Metrics, counterContext);
                    }
                }
            }
        }

        /// <summary>
        /// Extension logs system/OS event data to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The performance counter message/event name.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="events">The system events to log.</param>
        public static void LogSystemEvents(this ILogger logger, string message, IEnumerable<KeyValuePair<string, object>> events, EventContext eventContext)
        {
            logger.ThrowIfNull(nameof(logger));
            message.ThrowIfNullOrWhiteSpace(nameof(message));

            if (events?.Any() == true)
            {
                foreach (var systemEvent in events)
                {
                    // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
                    // output. To enable a seamless transition, we are supporting the old and the new schema
                    // until we have all systems using the latest version of the Virtual Client.
                    EventContext systemEventContext = eventContext?.Clone();
                    systemEventContext.Properties["eventType"] = systemEvent.Key;
                    systemEventContext.Properties["eventInfo"] = systemEvent.Value;

                    VirtualClientLoggingExtensions.LogMessage(logger, message, LogLevel.Information, LogType.SystemEvent, systemEventContext);
                }
            }
        }

        /// <summary>
        /// Logs the test metrics/results to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="toolName">The name of the workload tool that produced the test metrics/results (e.g. GeekBench, FIO).</param>
        /// <param name="scenarioName">The name of the test (e.g. fio_randwrite_4GB_4k_d1_th1_direct).</param>
        /// <param name="scenarioStartTime">The time at which the test began.</param>
        /// <param name="scenarioEndTime">The time at which the test ended.</param>
        /// <param name="metrics">List of the test metrics to log.</param>
        /// <param name="metricCategorization">The resource that was tested (e.g. a specific disk drive).</param>
        /// <param name="scenarioArguments">The command line parameters provided to the workload tool.</param>
        /// <param name="tags">Tags associated with the test.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="toolResults">The raw results produced by the workload/monitor etc. from which the metrics were parsed.</param>
        /// <param name="toolVersion">The version of the tool/toolset.</param>
        public static void LogMetrics(
            this ILogger logger,
            string toolName,
            string scenarioName,
            DateTime scenarioStartTime,
            DateTime scenarioEndTime,
            IList<Metric> metrics,
            string metricCategorization,
            string scenarioArguments,
            IEnumerable<string> tags,
            EventContext eventContext,
            string toolResults = null,
            string toolVersion = null)
        {
            logger.ThrowIfNull(nameof(logger));

            foreach (Metric metric in metrics)
            {
                VirtualClientLoggingExtensions.LogMetrics(
                    logger,
                    toolName,
                    scenarioName,
                    metric.StartTime == DateTime.MinValue ? scenarioStartTime : metric.StartTime,
                    metric.EndTime == DateTime.MinValue ? scenarioEndTime : metric.EndTime,
                    metric.Name,
                    metric.Value,
                    metric.Unit,
                    metricCategorization,
                    scenarioArguments,
                    tags,
                    eventContext,
                    metric.Relativity,
                    metric.Description,
                    toolResults,
                    toolVersion,
                    metric.Metadata);
            }
        }

        /// <summary>
        /// Logs the test metrics/results to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="toolName">The name of the workload tool that produced the test metrics/results (e.g. GeekBench, FIO).</param>
        /// <param name="scenarioName">The name of the test (e.g. fio_randwrite_4GB_4k_d1_th1_direct).</param>
        /// <param name="scenarioStartTime">The time at which the test began.</param>
        /// <param name="scenarioEndTime">The time at which the test ended.</param>
        /// <param name="metricName">The name of the metric associated with the test (e.g. iops/sec, requests/sec).</param>
        /// <param name="metricValue">The test metric result/value.</param>
        /// <param name="metricCategorization">The resource that was tested (e.g. a specific disk drive).</param>
        /// <param name="metricUnits">The units of measurement for the test metric result/value.</param>
        /// <param name="scenarioArguments">The command line parameters provided to the workload tool.</param>
        /// <param name="tags">Tags associated with the test.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="relativity">The relationship of the metric value to an outcome (e.g. higher/lower is better).</param>
        /// <param name="description">A description of the metric.</param>
        /// <param name="toolResults">The raw results produced by the workload/monitor etc. from which the metrics were parsed.</param>
        /// <param name="toolVersion">The version of the tool/toolset.</param>
        /// <param name="metricMetadata">Telemetry context related to metric.</param>
        public static void LogMetrics(
            this ILogger logger,
            string toolName,
            string scenarioName,
            DateTime scenarioStartTime,
            DateTime scenarioEndTime,
            string metricName,
            double metricValue,
            string metricUnits,
            string metricCategorization,
            string scenarioArguments,
            IEnumerable<string> tags,
            EventContext eventContext,
            MetricRelativity relativity = MetricRelativity.Undefined,
            string description = null,
            string toolResults = null,
            string toolVersion = null,
            IEnumerable<KeyValuePair<string, IConvertible>> metricMetadata = null)
        {
            logger.ThrowIfNull(nameof(logger));
            scenarioName.ThrowIfNullOrWhiteSpace(nameof(scenarioName));
            metricName.ThrowIfNullOrWhiteSpace(nameof(metricName));
            toolName.ThrowIfNullOrWhiteSpace(nameof(toolName));
            eventContext.ThrowIfNull(nameof(eventContext));
            scenarioStartTime.ThrowIfInvalid(nameof(scenarioStartTime), (time) => time != DateTime.MinValue);
            scenarioEndTime.ThrowIfInvalid(nameof(scenarioEndTime), (time) => time != DateTime.MinValue);

            // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
            // output. To enable a seamless transition, we are supporting the old and the new schema
            // until we have all systems using the latest version of the Virtual Client.
            var properties = new Dictionary<string, object>
            {
                { "scenarioName", scenarioName },
                { "scenarioStartTime", scenarioStartTime },
                { "scenarioEndTime", scenarioEndTime },
                { "scenarioArguments", scenarioArguments ?? string.Empty },
                { "metricName", metricName },
                { "metricValue", metricValue },
                { "metricUnit", metricUnits ?? string.Empty },
                { "metricCategorization", metricCategorization ?? string.Empty },
                { "metricDescription", description ?? string.Empty },
                { "metricRelativity", relativity.ToString() },
                { "toolName", toolName },
                { "toolVersion", toolVersion },
                { "toolResults", toolResults },
                { "tags", tags != null ? string.Join(',', tags) : string.Empty },
                { "metricMetadata", metricMetadata as object ?? string.Empty }
            };

            EventContext metricsContext = eventContext.Clone();
            metricsContext.Properties.AddRange(properties, withReplace: true);

            VirtualClientLoggingExtensions.LogMessage(logger, $"{toolName}.ScenarioResult", LogLevel.Information, LogType.Metrics, metricsContext);
        }

        /// <summary>
        /// Extension logs the trace information and context.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The message/event name to log.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        public static void LogTraceMessage(this ILogger logger, string message = null, EventContext eventContext = null)
        {
            logger.ThrowIfNull(nameof(logger));
            VirtualClientLoggingExtensions.LogMessage(
                logger,
                !string.IsNullOrWhiteSpace(message) ? message : "~",
                LogLevel.Trace,
                LogType.Trace,
                eventContext ?? EventContext.Persisted());
        }

        /// <summary>
        /// Obscures any secrets in the parameter set.
        /// </summary>
        /// <param name="parameters">The parameters that may contain secrets to obscure.</param>
        public static IDictionary<string, TValue> ObscureSecrets<TValue>(this IDictionary<string, TValue> parameters)
            where TValue : class
        {
            parameters.ThrowIfNull(nameof(parameters));

            IDictionary<string, TValue> obscuredParameters = new Dictionary<string, TValue>();
            // StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            foreach (var entry in parameters)
            {
                // We pass in secrets and keys as parameters (both on the command line and in profiles).
                // This logic helps ensure that we do not expose those secrets in plain text within our
                // telemetry.
                TValue value = entry.Value;
                if (value is string)
                {
                    try
                    {
                        string keyValue = $"{entry.Key}={entry.Value}";
                        string obscuredKeyValue = SensitiveData.ObscureSecrets(keyValue);
                        value = obscuredKeyValue?.Substring(entry.Key.Length + 1) as TValue;
                    }
                    catch
                    {
                        // Best effort.
                    }
                }

                obscuredParameters[entry.Key?.CamelCased()] = value as TValue;
            }

            return obscuredParameters;
        }
    }
}
