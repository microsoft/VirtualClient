// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Extensions;

    /// <summary>
    /// Some logging hellper methods specific to virtual client
    /// </summary>
    public static class VirtualClientLoggingExtensions
    {
        private static readonly Regex Base64Expression = new Regex("[\x30-\x39\x41-\x5A\x61-\x7A\x2B\x2F]+", RegexOptions.Compiled);
        private static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);

        private static readonly IAsyncPolicy FileSystemAccessRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));

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
        /// <param name="maxChars">
        /// The maximum number of characters that will be logged in the telemetry event from standard output + error. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static EventContext AddProcessContext(this EventContext telemetryContext, IProcessProxy process, string name = null, int maxChars = 125000)
        {
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            maxChars.ThrowIfInvalid(
                nameof(maxChars),
                (count) => count >= 0,
                $"Invalid max character count. The value provided must be greater than or equal to zero.");

            try
            {
                int? finalExitCode = null;
                string finalStandardOutput = null;
                string finalStandardError = null;
                int totalOutputChars = 0;

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
                    totalOutputChars += finalStandardOutput?.Length ?? 0;
                }
                catch
                {
                }

                try
                {
                    finalStandardError = process.StandardError?.ToString();
                    totalOutputChars += finalStandardError?.Length ?? 0;
                }
                catch
                {
                }

                string fullCommand = $"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}".Trim();
                if (!string.IsNullOrWhiteSpace(fullCommand))
                {
                    fullCommand = SensitiveData.ObscureSecrets(fullCommand);
                }

                // Note that 'totalOutputChars' represents the total # of characters in both the
                // standard output and error.
                if (finalStandardOutput != null && totalOutputChars > maxChars)
                {
                    // e.g.
                    // Given Max Chars = 125,000, length of standard output = 130,000 and length of standard error = 500
                    // Standard Output Substring Length = 130,000 - (130,500 - 125,000) = 130,000 - 5,500 = 124,500
                    // 
                    // And thus, the standard output will be 124,500 chars in length. The standard error will be 500 chars in length.
                    // The total will be 125,000 chars, right at the max.
                    int substringLength = finalStandardOutput.Length - (totalOutputChars - maxChars);
                    if (substringLength > 0)
                    {
                        // Careful that we do not attempt to get an invalid substring (e.g. 0 to -5).
                        finalStandardOutput = finalStandardOutput.Substring(0, finalStandardOutput.Length - (totalOutputChars - maxChars));
                    }
                    else
                    {
                        finalStandardOutput = string.Empty;
                    }

                    // Refresh the total character count
                    totalOutputChars = (finalStandardOutput?.Length ?? 0) + (finalStandardError?.Length ?? 0);
                }

                if (finalStandardError != null && totalOutputChars > maxChars)
                {
                    int substringLength = finalStandardError.Length - (totalOutputChars - maxChars);
                    if (substringLength > 0)
                    {
                        // Careful that we do not attempt to get an invalid substring (e.g. 0 to -5).
                        finalStandardError = finalStandardError.Substring(0, finalStandardError.Length - (totalOutputChars - maxChars));
                    }
                    else
                    {
                        finalStandardError = string.Empty;
                    }
                }

                telemetryContext.Properties[name ?? "process"] = new
                {
                    id = process.Id,
                    command = fullCommand ?? string.Empty,
                    workingDir = process.StartInfo?.WorkingDirectory ?? string.Empty,
                    exitCode = finalExitCode,
                    standardOutput = finalStandardOutput ?? string.Empty,
                    standardError = finalStandardError ?? string.Empty
                };
            }
            catch
            {
                // Best effort.
            }

            return telemetryContext;
        }

        /// <summary>
        /// Adds the details of the ssh command including standard output, standard error
        /// and exit code to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="sshCommand">The ssh command whose details will be captured.</param>
        /// <param name="name">The property name to use for the process telemetry.</param>
        /// <param name="maxChars">
        /// The maximum number of characters that will be logged in the telemetry event from standard output + error. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static EventContext AddSshCommandContext(this EventContext telemetryContext, ISshCommandProxy sshCommand, string name = null, int maxChars = 125000)
        {
            sshCommand.ThrowIfNull(nameof(sshCommand));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            maxChars.ThrowIfInvalid(
                nameof(maxChars),
                (count) => count >= 0,
                $"Invalid max character count. The value provided must be greater than or equal to zero.");

            try
            {
                int? finalExitCode = null;
                string finalStandardOutput = null;
                string finalStandardError = null;
                int totalOutputChars = 0;

                try
                {
                    finalExitCode = sshCommand.ExitStatus;
                }
                catch
                {
                }

                try
                {
                    finalStandardOutput = sshCommand.Result?.ToString();
                    totalOutputChars += finalStandardOutput?.Length ?? 0;
                }
                catch
                {
                }

                try
                {
                    finalStandardError = sshCommand.Error?.ToString();
                    totalOutputChars += finalStandardError?.Length ?? 0;
                }
                catch
                {
                }

                string fullCommand = $"{sshCommand.CommandText}".Trim();
                if (!string.IsNullOrWhiteSpace(fullCommand))
                {
                    fullCommand = SensitiveData.ObscureSecrets(fullCommand);
                }

                // Note that 'totalOutputChars' represents the total # of characters in both the
                // standard output and error.
                if (finalStandardOutput != null && totalOutputChars > maxChars)
                {
                    // e.g.
                    // Given Max Chars = 125,000, length of standard output = 130,000 and length of standard error = 500
                    // Standard Output Substring Length = 130,000 - (130,500 - 125,000) = 130,000 - 5,500 = 124,500
                    // 
                    // And thus, the standard output will be 124,500 chars in length. The standard error will be 500 chars in length.
                    // The total will be 125,000 chars, right at the max.
                    int substringLength = finalStandardOutput.Length - (totalOutputChars - maxChars);
                    if (substringLength > 0)
                    {
                        // Careful that we do not attempt to get an invalid substring (e.g. 0 to -5).
                        finalStandardOutput = finalStandardOutput.Substring(0, finalStandardOutput.Length - (totalOutputChars - maxChars));
                    }
                    else
                    {
                        finalStandardOutput = string.Empty;
                    }

                    // Refresh the total character count
                    totalOutputChars = (finalStandardOutput?.Length ?? 0) + (finalStandardError?.Length ?? 0);
                }

                if (finalStandardError != null && totalOutputChars > maxChars)
                {
                    int substringLength = finalStandardError.Length - (totalOutputChars - maxChars);
                    if (substringLength > 0)
                    {
                        // Careful that we do not attempt to get an invalid substring (e.g. 0 to -5).
                        finalStandardError = finalStandardError.Substring(0, finalStandardError.Length - (totalOutputChars - maxChars));
                    }
                    else
                    {
                        finalStandardError = string.Empty;
                    }
                }

                telemetryContext.Properties[name ?? "sshCommand"] = new
                {
                    command = fullCommand ?? string.Empty,
                    exitCode = finalExitCode,
                    standardOutput = finalStandardOutput ?? string.Empty,
                    standardError = finalStandardError ?? string.Empty
                };
            }
            catch
            {
                // Best effort.
            }

            return telemetryContext;
        }

        /// <summary>
        /// Adds the generated results of the process to telemetry
        /// and exit code to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="name">The property name to use for the process telemetry.</param>
        /// <param name="maxChars">
        /// The maximum number of characters that will be logged in the telemetry event from standard output + error. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static EventContext AddProcessResults(this EventContext telemetryContext, IProcessProxy process, string name = null, int maxChars = 125000)
        {
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            maxChars.ThrowIfInvalid(
                nameof(maxChars),
                (count) => count >= 0,
                $"Invalid max character count. The value provided must be greater than or equal to zero.");

            try
            {
                int? finalExitCode = null;
                string finalResults = null;

                try
                {
                    finalExitCode = process.ExitCode;
                }
                catch
                {
                }

                try
                {
                    finalResults = process?.LogResults?.GeneratedResults;
                }
                catch
                {
                }

                if (finalResults != null && finalResults.Length > maxChars)
                {
                    finalResults = finalResults.Substring(0, maxChars);
                }

                string fullCommand = $"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}".Trim();
                if (!string.IsNullOrWhiteSpace(fullCommand))
                {
                    fullCommand = SensitiveData.ObscureSecrets(fullCommand);
                }

                telemetryContext.Properties[name ?? "processResults"] = new
                {
                    id = process.Id,
                    command = fullCommand ?? string.Empty,
                    workingDir = process.StartInfo?.WorkingDirectory ?? string.Empty,
                    exitCode = finalExitCode,
                    results = finalResults ?? string.Empty,
                };
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
        /// Executes whenever an operation within the context of the component succeeds. This is used for example 
        /// to write custom telemetry and metrics associated with individual operations within the component.
        /// </summary>
        public static void LogFailedMetric(
            this VirtualClientComponent component,
            string toolName = null,
            string toolVersion = null,
            string scenarioName = null,
            string scenarioArguments = null,
            string metricCategorization = null,
            DateTime? scenarioStartTime = null,
            DateTime? scenarioEndTime = null,
            EventContext telemetryContext = null)
        {
            component.ThrowIfNull(nameof(component));
            component.LogSuccessOrFailMetric(false, toolName, toolVersion, scenarioName, scenarioArguments, metricCategorization, scenarioStartTime, scenarioEndTime, telemetryContext);
        }

        /// <summary>
        /// Logs a "Failed" metric to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="toolName">The name of the tool that produced the test metrics/results (e.g. GeekBench, FIO).</param>
        /// <param name="scenarioName">The name of the test (e.g. fio_randwrite_4GB_4k_d1_th1_direct).</param>
        /// <param name="scenarioStartTime">The time at which the test began.</param>
        /// <param name="scenarioEndTime">The time at which the test ended.</param>
        /// <param name="tags">Tags associated with the test.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="scenarioArguments">The command line parameters provided to the tool.</param>
        /// <param name="metricCategorization">The resource that was tested (e.g. a specific disk drive).</param>
        /// <param name="toolVersion">The version of the tool/toolset.</param>
        public static void LogFailedMetric(
            this ILogger logger,
            string toolName,
            string scenarioName,
            DateTime scenarioStartTime,
            DateTime scenarioEndTime,
            EventContext eventContext,
            string scenarioArguments = null,
            string metricCategorization = null,
            string toolVersion = null,
            IEnumerable<string> tags = null)
        {
            VirtualClientLoggingExtensions.LogMetrics(
                logger,
                toolName,
                scenarioName,
                scenarioStartTime,
                scenarioEndTime,
                "Failed",
                1,
                null,
                metricCategorization,
                scenarioArguments,
                tags,
                eventContext,
                MetricRelativity.LowerIsBetter,
                "Indicates the component or toolset execution failed for the scenario defined.",
                toolVersion: toolVersion);
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
        /// <param name="supportOriginalSchema">True to include properties in the metrics output that support the original Virtual Client metrics schema. Default = false.</param>
        public static void LogMetrics(
            this ILogger logger,
            string toolName,
            string scenarioName,
            DateTime scenarioStartTime,
            DateTime scenarioEndTime,
            IEnumerable<Metric> metrics,
            string metricCategorization,
            string scenarioArguments,
            IEnumerable<string> tags,
            EventContext eventContext,
            string toolResults = null,
            string toolVersion = null,
            bool supportOriginalSchema = false)
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
                    metric.Metadata,
                    supportOriginalSchema);
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
        /// <param name="supportOriginalSchema">True to include properties in the metrics output that support the original Virtual Client metrics schema. Default = false.</param>
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
            IEnumerable<KeyValuePair<string, IConvertible>> metricMetadata = null,
            bool supportOriginalSchema = false)
        {
            logger.ThrowIfNull(nameof(logger));
            scenarioName.ThrowIfNullOrWhiteSpace(nameof(scenarioName));
            metricName.ThrowIfNullOrWhiteSpace(nameof(metricName));
            toolName.ThrowIfNullOrWhiteSpace(nameof(toolName));
            eventContext.ThrowIfNull(nameof(eventContext));
            scenarioStartTime.ThrowIfInvalid(nameof(scenarioStartTime), (time) => time != DateTime.MinValue);
            scenarioEndTime.ThrowIfInvalid(nameof(scenarioEndTime), (time) => time != DateTime.MinValue);

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
                { "toolVersion", toolVersion ?? string.Empty },
                { "toolResults", toolResults ?? string.Empty },
                { "tags", tags != null ? string.Join(',', tags) : string.Empty },
                { "metricMetadata", metricMetadata as object ?? string.Empty }
            };

            // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
            // output. To enable a seamless transition, we are supporting the old and the new schema
            // until we have all systems using the latest version of the Virtual Client.
            if (supportOriginalSchema)
            {
                properties["testName"] = scenarioName;
                properties["testResult"] = metricValue;
                properties["units"] = metricUnits ?? string.Empty;
                properties["testedInstance"] = metricCategorization ?? string.Empty;
                properties["testArguments"] = scenarioArguments ?? string.Empty;
                properties["testStartTime"] = scenarioStartTime;
                properties["testEndTime"] = scenarioEndTime;
            }

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
        /// <param name="supportOriginalSchema">True to include properties in the metrics output that support the original Virtual Client metrics schema. Default = false.</param>
        public static void LogPerformanceCounters(this ILogger logger, string toolName, IEnumerable<Metric> counters, DateTime startTime, DateTime endTime, EventContext eventContext, string toolVersion = null, bool supportOriginalSchema = false)
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

                        // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
                        // output. To enable a seamless transition, we are supporting the old and the new schema
                        // until we have all systems using the latest version of the Virtual Client.
                        if (supportOriginalSchema)
                        {
                            counterContext.Properties["counterName"] = counter.Name;
                            counterContext.Properties["counterValue"] = counter.Value;
                            counterContext.Properties["testName"] = scenarioName;
                            counterContext.Properties["testStartTime"] = startTime;
                            counterContext.Properties["testEndTime"] = endTime;
                            counterContext.Properties["units"] = counter.Unit ?? string.Empty;
                        }

                        VirtualClientLoggingExtensions.LogMessage(logger, scenarioName, LogLevel.Information, LogType.Metrics, counterContext);
                    }
                }
            }
        }

        /// <summary>
        /// Captures the details of the sshcommand including standard output, standard error and exit codes to 
        /// telemetry on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="sshCommand">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">True to log the results to a log file on the file system. Default = false.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static async Task LogSshCommandDetailsAsync(
            this VirtualClientComponent component, ISshCommandProxy sshCommand, EventContext telemetryContext, bool logToTelemetry = true, bool logToFile = false, int logToTelemetryMaxChars = 125000)
        {
            component.ThrowIfNull(nameof(component));
            sshCommand.ThrowIfNull(nameof(sshCommand));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            ILogger logger = null;

            if (logToTelemetry)
            {
                try
                {
                    if (component.Dependencies.TryGetService<ILogger>(out logger))
                    {
                        logger.LogSshCommandDetails(sshCommand, component.TypeName, telemetryContext, logToTelemetryMaxChars);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }

            if (VirtualClientComponent.LogToFile && logToFile)
            {
                try
                {
                    await component.LogResultsToFileAsync(sshCommand.LogResults, telemetryContext);
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error and exit codes to 
        /// telemetry and log files on the system.
        /// </summary>
        /// <param name="component">The component requesting the logging.</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetry">True to log the results to telemetry. Default = true.</param>
        /// <param name="logToFile">True to log the results to a log file on the file system. Default = false.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        public static async Task LogProcessDetailsAsync(
            this VirtualClientComponent component, IProcessProxy process, EventContext telemetryContext, bool logToTelemetry = true, bool logToFile = false, int logToTelemetryMaxChars = 125000)
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
                        logger.LogProcessDetails(process, component.TypeName, telemetryContext, logToTelemetryMaxChars);
                    }
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }

            if (VirtualClientComponent.LogToFile && logToFile)
            {
                try
                {
                    await component.LogResultsToFileAsync(process.LogResults, telemetryContext);
                }
                catch (Exception exc)
                {
                    // Best effort but we should never crash VC if the logging fails. Metric capture
                    // is more important to the operations of VC. We do want to log the failure.
                    logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
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
        /// Logs a "Succeeded" metric to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="toolName">The name of the tool that produced the test metrics/results (e.g. GeekBench, FIO).</param>
        /// <param name="scenarioName">The name of the test (e.g. fio_randwrite_4GB_4k_d1_th1_direct).</param>
        /// <param name="scenarioStartTime">The time at which the test began.</param>
        /// <param name="scenarioEndTime">The time at which the test ended.</param>
        /// <param name="tags">Tags associated with the test.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="scenarioArguments">The command line parameters provided to the tool.</param>
        /// <param name="metricCategorization">The resource that was tested (e.g. a specific disk drive).</param>
        /// <param name="toolVersion">The version of the tool/toolset.</param>
        public static void LogSuccessMetric(
            this ILogger logger,
            string toolName,
            string scenarioName,
            DateTime scenarioStartTime,
            DateTime scenarioEndTime,
            EventContext eventContext,
            string scenarioArguments = null,
            string metricCategorization = null,
            string toolVersion = null,
            IEnumerable<string> tags = null)
        {
            logger.LogMetrics(
                toolName,
                scenarioName,
                scenarioStartTime,
                scenarioEndTime,
                "Succeeded",
                1,
                null,
                metricCategorization,
                scenarioArguments,
                tags,
                eventContext,
                MetricRelativity.HigherIsBetter,
                "Indicates the component or toolset execution succeeded for the scenario defined.",
                toolVersion: toolVersion);
        }

        /// <summary>
        /// Executes whenever an operation within the context of the component succeeds. This is used for example 
        /// to write custom telemetry and metrics associated with individual operations within the component.
        /// </summary>
        public static void LogSuccessMetric(
            this VirtualClientComponent component,
            string toolName = null,
            string toolVersion = null,
            string scenarioName = null,
            string scenarioArguments = null,
            string metricCategorization = null,
            DateTime? scenarioStartTime = null,
            DateTime? scenarioEndTime = null,
            EventContext telemetryContext = null)
        {
            component.ThrowIfNull(nameof(component));
            component.LogSuccessOrFailMetric(true, toolName, toolVersion, scenarioName, scenarioArguments, metricCategorization, scenarioStartTime, scenarioEndTime, telemetryContext);
        }

        /// <summary>
        /// Extension logs system/OS event data to the target telemetry data store(s).
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="message">The performance counter message/event name.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the event.</param>
        /// <param name="events">The system events to log.</param>
        /// <param name="supportOriginalSchema">True to include properties in the metrics output that support the original Virtual Client metrics schema. Default = false.</param>
        public static void LogSystemEvents(this ILogger logger, string message, IEnumerable<KeyValuePair<string, object>> events, EventContext eventContext, bool supportOriginalSchema = false)
        {
            logger.ThrowIfNull(nameof(logger));
            message.ThrowIfNullOrWhiteSpace(nameof(message));

            if (events?.Any() == true)
            {
                foreach (var systemEvent in events)
                {
                    EventContext systemEventContext = eventContext?.Clone();
                    systemEventContext.Properties["eventType"] = systemEvent.Key;
                    systemEventContext.Properties["eventInfo"] = systemEvent.Value;

                    // 1/18/2022: Note that we are in the process of modifying the schema of the VC telemetry
                    // output. To enable a seamless transition, we are supporting the old and the new schema
                    // until we have all systems using the latest version of the Virtual Client.
                    if (supportOriginalSchema)
                    {
                        systemEventContext.Properties["name"] = systemEvent.Key;
                        systemEventContext.Properties["value"] = systemEvent.Value;
                    }

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
        /// <param name="componentType">The type of component (e.g. GeekbenchExecutor).</param>
        /// <param name="sshCommand">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        internal static void LogSshCommandDetails(this ILogger logger, ISshCommandProxy sshCommand, string componentType, EventContext telemetryContext, int logToTelemetryMaxChars = 125000)
        {
            logger.ThrowIfNull(nameof(logger));
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            sshCommand.ThrowIfNull(nameof(sshCommand));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                // Examples:
                // --------------
                // GeekbenchExecutor.ProcessDetails
                // GeekbenchExecutor.Geekbench.ProcessDetails
                // GeekbenchExecutor.ProcessResults
                // GeekbenchExecutor.Geekbench.ProcessResults
                string eventNamePrefix = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                    !string.IsNullOrWhiteSpace(sshCommand.LogResults.ToolSet) ? $"{componentType}.{sshCommand.LogResults.ToolSet}" : componentType,
                    string.Empty);

                logger.LogMessage(
                    $"{eventNamePrefix}.SshCommandDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddSshCommandContext(sshCommand, maxChars: logToTelemetryMaxChars));
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
        /// <param name="componentType">The type of component (e.g. GeekbenchExecutor).</param>
        /// <param name="process">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="logToTelemetryMaxChars">
        /// The maximum number of characters that will be logged in the telemetry event. There are often limitations on the size 
        /// of telemetry events. The goal here is to capture as much of the information about the process in the telemetry event
        /// without risking data loss during upload because the message exceeds thresholds. Default = 125,000 chars. In relativity
        /// there are about 3000 characters in an average single-spaced page of text.
        /// </param>
        internal static void LogProcessDetails(this ILogger logger, IProcessProxy process, string componentType, EventContext telemetryContext, int logToTelemetryMaxChars = 125000)
        {
            logger.ThrowIfNull(nameof(logger));
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            process.ThrowIfNull(nameof(process));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                // Examples:
                // --------------
                // GeekbenchExecutor.ProcessDetails
                // GeekbenchExecutor.Geekbench.ProcessDetails
                // GeekbenchExecutor.ProcessResults
                // GeekbenchExecutor.Geekbench.ProcessResults
                string eventNamePrefix = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                    !string.IsNullOrWhiteSpace(process.LogResults.ToolSet) ? $"{componentType}.{process.LogResults.ToolSet}" : componentType,
                    string.Empty);

                logger.LogMessage(
                    $"{eventNamePrefix}.ProcessDetails",
                    LogLevel.Information,
                    telemetryContext.Clone().AddProcessContext(process, maxChars: logToTelemetryMaxChars));

                if (!string.IsNullOrWhiteSpace(process.LogResults.GeneratedResults))
                {
                    logger.LogMessage(
                        $"{eventNamePrefix}.ProcessResults",
                        LogLevel.Information,
                        telemetryContext.Clone().
                        AddProcessResults(process, maxChars: logToTelemetryMaxChars));
                }
            }
            catch
            {
                // Best effort.
            }
        }

        /// <summary>
        /// Captures the details of the process including standard output, standard error, exit code and results in log files
        /// on the system.
        /// </summary>
        /// <param name="component">The component that ran the process.</param>
        /// <param name="logResults">The process whose details will be captured.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        internal static async Task LogResultsToFileAsync(this VirtualClientComponent component, LogResults logResults, EventContext telemetryContext)
        {
            component.ThrowIfNull(nameof(component));
            logResults.ThrowIfNull(nameof(logResults));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            try
            {
                if (component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem)
                    && component.Dependencies.TryGetService<PlatformSpecifics>(out PlatformSpecifics specifics))
                {
                    string effectiveToolName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                        (!string.IsNullOrWhiteSpace(logResults.ToolSet) ? logResults.ToolSet : component.TypeName).ToLowerInvariant().RemoveWhitespace(),
                        string.Empty);

                    string effectiveCommand = $"{logResults.CommandLine}".Trim();
                    string logPath = specifics.GetLogsPath(effectiveToolName.ToLowerInvariant().RemoveWhitespace());

                    if (!fileSystem.Directory.Exists(logPath))
                    {
                        fileSystem.Directory.CreateDirectory(logPath).Create();
                    }

                    // Examples:
                    // --------------
                    // /logs/fio/2023-02-01T122330Z-fio.log
                    // /logs/fio/2023-02-01T122745Z-fio.log
                    //
                    // /logs/fio/2023-02-01T122330Z-randomwrite_4k_blocksize.log
                    // /logs/fio/2023-02-01T122745Z-randomwrite_8k_blocksize.log
                    string effectiveLogFileName = VirtualClientLoggingExtensions.PathReservedCharacterExpression.Replace(
                        $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmssffffZ")}-{(!string.IsNullOrWhiteSpace(component.Scenario) ? component.Scenario : effectiveToolName)}.log",
                        string.Empty).ToLowerInvariant().RemoveWhitespace();

                    string logFilePath = specifics.Combine(logPath, effectiveLogFileName);

                    // Examples:
                    // --------------
                    // Command           : /home/user/nuget/virtualclient/packages/fio/linux-x64/fio --name=randwrite_4k --size=128G --numjobs=8 --rw=randwrite --bs=4k
                    // Working Directory : /home/user/nuget/virtualclient/packages/fio/linux-x64
                    // Exit Code         : 0
                    //
                    // ##StandardOutput##
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
                    // ##GeneratedResults##
                    // Any results from the output of the process

                    StringBuilder outputBuilder = new StringBuilder();
                    outputBuilder.AppendLine($"Command           : {SensitiveData.ObscureSecrets(logResults?.CommandLine)}");
                    outputBuilder.AppendLine($"Working Directory : {logResults?.WorkingDirectory}");
                    outputBuilder.AppendLine($"Exit Code         : {logResults?.ExitCode}");
                    outputBuilder.AppendLine();
                    outputBuilder.AppendLine("##StandardOutput##"); // This is a simple delimiter that will NOT conflict with regular expressions possibly used in custom parsing.
                    outputBuilder.AppendLine(logResults.StandardOutput);

                    if (!string.IsNullOrEmpty(logResults.StandardError))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##StandardError##");
                        outputBuilder.AppendLine(logResults.StandardError);
                    }

                    if (!string.IsNullOrWhiteSpace(logResults.GeneratedResults))
                    {
                        outputBuilder.AppendLine();
                        outputBuilder.AppendLine("##GeneratedResults##");
                        outputBuilder.AppendLine(logResults.GeneratedResults);
                    }

                    await VirtualClientLoggingExtensions.FileSystemAccessRetryPolicy.ExecuteAsync(async () =>
                    {
                        await fileSystem.File.WriteAllTextAsync(logFilePath, outputBuilder.ToString());
                    });
                }
            }
            catch (Exception exc)
            {
                // Best effort but we should never crash VC if the logging fails. Metric capture
                // is more important to the operations of VC. We do want to log the failure.
                component.Logger?.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }
        }

        private static void LogSuccessOrFailMetric(
           this VirtualClientComponent component,
           bool success,
           string toolName = null,
           string toolVersion = null,
           string scenarioName = null,
           string scenarioArguments = null,
           string metricCategorization = null,
           DateTime? scenarioStartTime = null,
           DateTime? scenarioEndTime = null,
           EventContext telemetryContext = null)
        {
            component.ThrowIfNull(nameof(component));

            // Example Metrics for Executor (Scenario parameter not defined, defaults used).
            // | Tool Name          | Scenario         | Metric Name | Metric Value |
            // |--------------------|------------------|-------------|--------------|
            // | GeekbenchExecutor  | Outcome          | Succeeded   | 1            |
            // | GeekbenchExecutor  | Outcome          | Failed      | 0            |
            // | GeekbenchExecutor  | Outcome          | Succeeded   | 0            |
            // | GeekbenchExecutor  | Outcome          | Failed      | 1            |

            // Example Metrics for Executor (Scenario parameter defined)
            // | Tool Name          | Scenario         | Metric Name | Metric Value |
            // |--------------------|------------------|-------------|--------------|
            // | GeekbenchExecutor  | ScoreSystem      | Succeeded   | 1            |
            // | GeekbenchExecutor  | ScoreSystem      | Failed      | 0            |
            // | GeekbenchExecutor  | ScoreSystem      | Succeeded   | 0            |
            // | GeekbenchExecutor  | ScoreSystem      | Failed      | 1            |

            // Example Metrics for Toolset
            // | Tool Name          | Scenario         | Metric Name | Metric Value |
            // |--------------------|------------------|-------------|--------------|
            // | Geekbench5         | ScoreSystem      | Succeeded   | 1            |
            // | Geekbench5         | ScoreSystem      | Failed      | 0            |
            // | Geekbench5         | ScoreSystem      | Succeeded   | 0            |
            // | Geekbench5         | ScoreSystem      | Failed      | 1            |

            string effectiveScenarioName = scenarioName;
            string effectiveToolName = toolName;

            if (string.IsNullOrWhiteSpace(scenarioName) && !string.IsNullOrWhiteSpace(component.Scenario))
            {
                effectiveScenarioName = component.Scenario;
            }

            effectiveScenarioName = !string.IsNullOrWhiteSpace(effectiveScenarioName) ? effectiveScenarioName : "Outcome";
            effectiveToolName = !string.IsNullOrEmpty(effectiveToolName) ? effectiveToolName : component.TypeName;

            if (success)
            {
                component.Logger.LogSuccessMetric(
                    effectiveToolName,
                    effectiveScenarioName,
                    scenarioStartTime ?? DateTime.UtcNow,
                    scenarioEndTime ?? DateTime.UtcNow,
                    telemetryContext ?? EventContext.Persisted(),
                    scenarioArguments,
                    metricCategorization,
                    toolVersion);
            }
            else
            {
                component.Logger.LogFailedMetric(
                    effectiveToolName,
                    effectiveScenarioName,
                    scenarioStartTime ?? DateTime.UtcNow,
                    scenarioEndTime ?? DateTime.UtcNow,
                    telemetryContext ?? EventContext.Persisted(),
                    scenarioArguments,
                    metricCategorization,
                    toolVersion);
            }
        }
    }
}