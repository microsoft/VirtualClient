// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
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
        private static readonly Regex Base64Expression = new Regex("[\x30-\x39\x41-\x5A\x61-\x7A\x2B\x2F]+", RegexOptions.Compiled);
        private static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);

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
        /// Adds the details of the process including standard output, standard error
        /// and exit code to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="name">The property name to use for the process telemetry.</param>
        /// <param name="results">Results produced by the process execution to log.</param>
        public static EventContext AddProcessContext(this EventContext telemetryContext, IProcessProxy process, string name = null, string results = null)
        {
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                int? finalExitCode = null;
                string finalStandardOutput = null;
                string finalStandardError = null;

                try
                {
                    finalExitCode = process.ExitCode;
                }
                catch
                {
                }

                try
                {
                    finalStandardOutput = process.StandardOutput?.ToString();
                }
                catch
                {
                }

                try
                {
                    finalStandardError = process.StandardError?.ToString();
                }
                catch
                {
                }

                string fullCommand = $"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}".Trim();
                if (!string.IsNullOrWhiteSpace(fullCommand))
                {
                    fullCommand = SensitiveData.ObscureSecrets(fullCommand);
                }

                if (string.IsNullOrWhiteSpace(results))
                {
                    telemetryContext.AddContext(name ?? "process", new
                    {
                        id = process.Id,
                        command = fullCommand ?? string.Empty,
                        workingDir = process.StartInfo?.WorkingDirectory ?? string.Empty,
                        exitCode = finalExitCode,
                        standardOutput = finalStandardOutput ?? string.Empty,
                        standardError = finalStandardError ?? string.Empty
                    });
                }
                else
                {
                    telemetryContext.AddContext(name ?? "process", new
                    {
                        id = process.Id,
                        command = fullCommand ?? string.Empty,
                        workingDir = process.StartInfo?.WorkingDirectory ?? string.Empty,
                        exitCode = finalExitCode,
                        results = results ?? string.Empty,
                    });
                }
            }
            catch
            {
                // Best effort.
            }

            return telemetryContext;
        }

        /// <summary>
        /// Extension adds HTTP action response information to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context object.</param>
        /// <param name="response">The HTTP action response.</param>
        /// <param name="propertyName">Optional property allows the caller to define the name of the telemetry context property.</param>
        public static EventContext AddResponseContext(this EventContext telemetryContext, HttpResponseMessage response, string propertyName = "response")
        {
            response.ThrowIfNull(nameof(response));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            string responseContent = null;
            if (response.Content != null)
            {
                try
                {
                    responseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // Best effort only.
                }
            }

            telemetryContext.AddContext(propertyName, new
            {
                statusCode = (int)response.StatusCode,
                method = $"{response?.RequestMessage?.Method}",
                requestUri = $"{response?.RequestMessage?.RequestUri}",
                content = responseContent
            });

            return telemetryContext;
        }

        /// <summary>
        /// Returns a camel-cased version of the property name (e.g. SomeValue -> someValue).
        /// </summary>
        /// <param name="propertyName">The property name to camel-case.</param>
        public static string CamelCased(this string propertyName)
        {
            // We are not trying to get overly fancy here. We just make sure the first character
            // is lower-cased and leave it at that.
            return $"{propertyName.Substring(0, 1).ToLowerInvariant()}{propertyName.Substring(1)}";
        }

        /// <summary>
        /// Returns a pascal-cased version of the property name (e.g. someValue -> SomeValue).
        /// </summary>
        /// <param name="propertyName">The property name to pascal-case.</param>
        public static string PascalCased(this string propertyName)
        {
            // We are not trying to get overly fancy here. We just make sure the first character
            // is lower-cased and leave it at that.
            return $"{propertyName.Substring(0, 1).ToUpperInvariant()}{propertyName.Substring(1)}";
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
        /// Logs the command and arguments to the trace output.
        /// </summary>
        /// <param name="component">The component executing the process.</param>
        /// <param name="process">The process whose details will be logged.</param>
        public static void LogProcessTrace(this VirtualClientComponent component, IProcessProxy process)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));

            try
            {
                if (!string.IsNullOrWhiteSpace(process.StartInfo?.FileName))
                {
                    component.Logger?.LogTraceMessage($"Executing: {process.StartInfo.FileName} {process.StartInfo.Arguments}".Trim());
                }
            }
            catch
            {
                // Best effort only.
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="toolset">The name of the toolset running in the process.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">True to log the results to a log file on the file system. Default = false.</param>
        public static async Task LogProcessDetailsAsync(
            this VirtualClientComponent component,
            IProcessProxy process,
            EventContext telemetryContext,
            string toolset = null,
            string results = null,
            bool logToTelemetry = true,
            bool logToFile = false)
        {
            component.ThrowIfNull(nameof(component));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            ILogger logger = null;

            if (logToTelemetry)
            {
                try
                {
                    if (component.Dependencies.TryGetService<ILogger>(out logger))
                    {
                        // Examples:
                        // --------------
                        // GeekbenchExecutor.ProcessDetails
                        // GeekbenchExecutor.Geekbench.ProcessDetails
                        string effectiveName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                            !string.IsNullOrWhiteSpace(toolset) ? $"{component.TypeName}.{toolset}" : component.TypeName,
                            string.Empty);

                        logger.LogProcessDetails(process, effectiveName, telemetryContext, results);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }

            if (logToFile)
            {
                try
                {
                    if (component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem)
                        && component.Dependencies.TryGetService<PlatformSpecifics>(out PlatformSpecifics specifics))
                    {
                        string effectiveToolName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                            (!string.IsNullOrWhiteSpace(toolset) ? toolset : component.TypeName).ToLowerInvariant().RemoveWhitespace(),
                            string.Empty);

                        string effectiveCommand = $"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}".Trim();

                        string logPath = specifics.GetLogsPath(effectiveToolName.ToLowerInvariant().RemoveWhitespace());

                        if (!fileSystem.Directory.Exists(logPath))
                        {
                            fileSystem.Directory.CreateDirectory(logPath).Create();
                        }

                        // Examples:
                        // --------------
                        // /logs/fio/2023-02-01T122330Z-fio.log
                        // /logs/fio/2023-02-01T122745Z-fio.log
                        string logFilePath = specifics.Combine(logPath, $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssZ")}-{effectiveToolName}.log");

                        // Examples:
                        // --------------
                        // Command           : /home/user/nuget/virtualclient/packages/fio/linux-x64/fio --name=randwrite_4k --size=128G --numjobs=8 --rw=randwrite --bs=4k
                        // Working Directory : /home/user/nuget/virtualclient/packages/fio/linux-x64
                        // Exit Code         : 0
                        //
                        // ##Output##
                        // {
                        //    "fio version" : "fio-3.26-19-ge7e53",
                        //      "timestamp" : 1646873555,
                        //      "timestamp_ms" : 1646873555274,
                        //      "time" : "Thu Mar 10 00:52:35 2022",
                        //      "jobs" : [
                        //        {
                        //           ...
                        //        }
                        //      ]
                        // }
                        // 
                        // Standard Error:
                        StringBuilder outputBuilder = new StringBuilder();
                        outputBuilder.AppendLine($"Command           : {SensitiveData.ObscureSecrets(effectiveCommand)}");
                        outputBuilder.AppendLine($"Working Directory : {process.StartInfo?.WorkingDirectory}");
                        outputBuilder.AppendLine($"Exit Code         : {process.ExitCode}");
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##Output##"); // This is a simple delimiter that will NOT conflict with regular expressions possibly used in custom parsing.
                        outputBuilder.AppendLine(process.StandardOutput?.ToString());

                        if (process.ExitCode != 0)
                        {
                            outputBuilder.AppendLine();
                            outputBuilder.Append(process.StandardError?.ToString());
                        }

                        if (results != null)
                        {
                            outputBuilder.AppendLine();
                            outputBuilder.AppendLine("##Results##");
                            outputBuilder.AppendLine(results);
                        }

                        await fileSystem.File.WriteAllTextAsync(logFilePath, outputBuilder.ToString())
                            .ConfigureAwait(false);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    component.Logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
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

        /// <summary>
        /// Captures the details of the process including standard output, standard error
        /// and exit code.
        /// </summary>
        /// <param name="logger">The telemetry logger.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        internal static void LogProcessDetails<T>(this ILogger logger, IProcessProxy process, EventContext telemetryContext)
            where T : class
        {
            logger.ThrowIfNull(nameof(logger));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                logger.LogMessage(
                    $"{typeof(T).Name}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(process));
            }
            catch
            {
                // Best effort.
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error
        /// and exit code.
        /// </summary>
        /// <param name="logger">The telemetry logger.</param>
        /// <param name="toolset">The name of the command/toolset that produced the results. The suffix 'ProcessDetails' will be appended.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="results">Results from the process execution (i.e. outside of standard output).</param>
        internal static void LogProcessDetails(this ILogger logger, IProcessProxy process, string toolset, EventContext telemetryContext, string results = null)
        {
            logger.ThrowIfNull(nameof(logger));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                if (string.IsNullOrWhiteSpace(results))
                {
                    logger.LogMessage(
                        $"{toolset}.ProcessDetails",
                        LogLevel.Information,
                        telemetryContext.Clone().AddProcessContext(process));
                }
                else
                {
                    logger.LogMessage(
                        $"{toolset}.ProcessResults",
                        LogLevel.Information,
                        telemetryContext.Clone().AddProcessContext(process, results: results));
                }
            }
            catch
            {
                // Best effort.
            }
        }
    }
}
