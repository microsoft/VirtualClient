// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Extensions;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation that writes messages to the console. The
    /// .NET Core implementation caches messages vs. writing them to output immediately.
    /// </summary>
    /// <remarks>
    /// We are using a custom implementation of ConsoleLogger (instead of inheriting) because of issues described in
    /// this github thread: https://github.com/aspnet/EntityFramework.Docs/pull/1164. Recommend revisiting this code once
    /// the project has been moved to .NET Core 3.0 (.NET Standard equivalent)
    /// </remarks>
    public class ConsoleLogger : ILogger
    {
        private static readonly ConsoleColor DefaultFontColor = Console.ForegroundColor;
        private static readonly object SyncRoot = new object();

        /// <summary>
        ///  Initializes a new instance of the <see cref="ConsoleLogger"/> class.
        /// </summary>
        public ConsoleLogger(string categoryName, LogLevel minimumLogLevel = LogLevel.Information, bool disableColors = false)
        {
            this.MinimumLogLevel = minimumLogLevel;
            this.CategoryName = categoryName;
            this.IncludeTimestamps = true;
            this.DisableColors = disableColors;
        }

        /// <summary>
        /// The default console logger.
        /// </summary>
        public static ConsoleLogger Default { get; set; } = new ConsoleLogger("VirtualClient", LogLevel.Debug);

        /// <summary>
        /// True to include log severity levels in output.
        /// </summary>
        public bool IncludeLogLevel { get; set; }

        /// <summary>
        /// True if timestamps should be included in log output.
        /// </summary>
        public bool IncludeTimestamps { get; set; }

        /// <summary>
        /// Gets or sets true/false whether differential colors (dependent upon log level)
        /// are disabled.
        /// </summary>
        public bool DisableColors { get; set; }

        /// <summary>
        /// Gets or sets the name of the category for the logger.
        /// </summary>
        public string CategoryName { get; set; }

        /// <summary>
        /// Gets or sets the minimum log level/severity.
        /// </summary>
        public LogLevel MinimumLogLevel { get; set; }

        /// <summary>
        /// Required implementation for ILogger
        /// </summary>
        /// <typeparam name="TState">The data type for the event/message object.</typeparam>
        /// <param name="logLevel">The log level/severity of the event/message.</param>
        /// <param name="eventId">The event ID of the event/message.</param>
        /// <param name="state">The event/message object or text.</param>
        /// <param name="exception">An exception associated with the event/message.</param>
        /// <param name="formatter">Given an exception is provided, a delegate to format the message output for that exception.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                if (!this.IsEnabled(logLevel))
                {
                    return;
                }

                string message = null;
                bool includeCallstack = this.MinimumLogLevel <= LogLevel.Trace;

                if (logLevel >= LogLevel.Warning)
                {
                    if (exception != null)
                    {
                        message = eventId.Name;
                        ConsoleLogger.WriteMessage(logLevel, this.CategoryName, eventId.Id, message, exception, this.DisableColors, this.IncludeTimestamps, includeCallstack);
                    }
                    else if (ConsoleLogger.TryParseEventContextFromState(state, out EventContext telemetryContext))
                    {
                        telemetryContext.Properties.TryGetValue("error", out object error);
                        
                        if (error != null)
                        {
                            JArray errors = JArray.FromObject(error);
                            IEnumerable<string> errorMessages = errors.Select(token => token.SelectToken("errorMessage")?.Value<string>());

                            if (errorMessages?.Any() == true)
                            {
                                if (includeCallstack)
                                {
                                    if (telemetryContext.Properties.TryGetValue("errorCallstack", out object errorCallstack))
                                    {
                                        string effectiveCallstack = errorCallstack?.ToString();
                                        if (!string.IsNullOrWhiteSpace(effectiveCallstack))
                                        {
                                            message =
                                                $"{string.Join(' ', errorMessages)}" +
                                                $"{Environment.NewLine}{Environment.NewLine}{effectiveCallstack}{Environment.NewLine}";
                                        }
                                    }
                                }
                                else
                                {
                                    message = $"{string.Join(' ', errorMessages)}";
                                }
                            }
                        }
                        else
                        {
                            message = eventId.Name;
                        }
                    }
                    else if (ConsoleLogger.TryParseExceptionFromState(state, out Exception exc))
                    {
                        message = exc.ToDisplayFriendlyString(withCallStack: includeCallstack);
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        ConsoleLogger.WriteMessage(logLevel, this.CategoryName, eventId.Id, message, null, this.DisableColors, this.IncludeTimestamps);
                    }
                }
                else if (this.MinimumLogLevel <= LogLevel.Debug && ConsoleLogger.TryParseProcessDetailsFromState(state, out ProcessDetails processDetails))
                {
                    // TODO:
                    // To avoid the output of process standard output + error twice, we are making the assumption that
                    // a component.LogProcessDetailsAsync() method call is followed by a process.ThrowIfErrored() method call.
                    // This is often the case so the assumption holds true largely at the moment. However, this is NOT a good design
                    // choice and we will need to figure out a better design in the future.
                    if (processDetails.ExitCode == 0)
                    {
                        // Standard Output
                        if (!string.IsNullOrWhiteSpace(processDetails.StandardOutput))
                        {
                            ConsoleLogger.WriteMessage(LogLevel.Trace, this.CategoryName, eventId.Id, string.Empty, null, this.DisableColors, false);
                            ConsoleLogger.WriteMessage(LogLevel.Trace, this.CategoryName, eventId.Id, processDetails.StandardOutput, null, this.DisableColors, false);
                        }

                        // Standard Error
                        if (!string.IsNullOrWhiteSpace(processDetails.StandardError))
                        {
                            ConsoleLogger.WriteMessage(LogLevel.Trace, this.CategoryName, eventId.Id, string.Empty, null, this.DisableColors, false);
                            ConsoleLogger.WriteMessage(LogLevel.Trace, this.CategoryName, eventId.Id, processDetails.StandardError, null, this.DisableColors, false);
                        }
                    }
                }
                else
                {
                    message = eventId.Name;

                    if (message == null)
                    {
                        message = state?.ToString();
                    }

                    if (!string.IsNullOrEmpty(message))
                    {
                        ConsoleLogger.WriteMessage(logLevel, this.CategoryName, eventId.Id, message, null, this.DisableColors, this.IncludeTimestamps);
                    }
                }
            }
            catch
            {
                // No console logging errors should cause the application itself to crash.
            }
        }

        /// <summary>
        /// Checks if a log level is enabled. Inferred through the minimum log level.
        /// </summary>
        /// <param name="logLevel">log level being queried</param>
        /// <returns>if log level is above minimum log level currently set for this ILogger</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= this.MinimumLogLevel;
        }

        /// <summary>
        /// Begins a logical operation scope. Not used, but required for ILogger interface.
        /// </summary>
        /// <typeparam name="TState">Recommend key,value pair types like Dictionary</typeparam>
        /// <param name="state">Scope state.</param>
        /// <returns>Disposable scope.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return state as IDisposable;
        }

        /// <summary>
        /// Returns the message header (ex: info: Console (event ID = 1234))
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logName">The name of the log category</param>
        /// <param name="logLevel">The log level/severity.</param>
        /// <param name="eventId">An event ID associated with the logging message/event.</param>
        /// <param name="exception">exception to be written</param>
        /// <param name="includeLogLevel">True to include the log level in output.</param>
        /// <param name="includeTimestamp">True to include timestamps in output.</param>
        /// <param name="includeCallstack">Include error callstacks.</param>
        /// <returns>
        /// The message header for the log entry.
        /// </returns>
        internal static string GetMessage(string message, LogLevel logLevel, int eventId, string logName, Exception exception, bool includeLogLevel = false, bool includeTimestamp = false, bool includeCallstack = false)
        {
            string messageHeaderLogLevel = ConsoleLogger.GetMessageHeaderLogLevel(logLevel);
            string messageContent = message;
            string exceptionMessage = string.Empty;

            if (exception != null)
            {
                exceptionMessage = exception.ToDisplayFriendlyString(withCallStack: includeCallstack); 
            }

            if (includeLogLevel && includeTimestamp)
            {
                messageContent = $"[{messageHeaderLogLevel}: {DateTime.Now}] {message}";
            }
            else if (includeLogLevel)
            {
                messageContent = $"[{messageHeaderLogLevel}] {message}";
            }
            else if (includeTimestamp)
            {
                messageContent = $"[{DateTime.Now}] {message}";
            }

            // Example:
            // Any message/event that happened.
            // [info] Any message/event that happened.
            // [info: 09/27/2018 06:09:33 PM] Any message/event that happened.
            if (!string.IsNullOrWhiteSpace(exceptionMessage))
            {
                messageContent = $"{messageContent}{Environment.NewLine}{exceptionMessage}";
            }

            return messageContent;
        }

        /// <summary>
        /// Returns the console foreground/text color for the log level provided.
        /// </summary>
        /// <param name="logLevel">The log level/severity.</param>
        /// <returns>
        /// The console foreground/text color for the log level provided.
        /// </returns>
        internal static ConsoleColor GetTextColor(LogLevel logLevel)
        {
            ConsoleColor textColor = ConsoleLogger.DefaultFontColor;

            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    textColor = ConsoleColor.Red;
                    break;

                case LogLevel.Warning:
                    textColor = ConsoleColor.Yellow;
                    break;
            }

            return textColor;
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Desired format for log level text is lower-cased in console output.")]
        private static string GetMessageHeaderLogLevel(LogLevel logLevel)
        {
            string messageHeaderLogLevel = logLevel.ToString().ToLowerInvariant();
            if (logLevel == LogLevel.Information)
            {
                messageHeaderLogLevel = "info";
            }

            return messageHeaderLogLevel;
        }

        private static bool TryParseEventContextFromState<TState>(TState state, out EventContext telemetryContext)
        {
            telemetryContext = state as EventContext;
            return telemetryContext != null;
        }

        private static bool TryParseExceptionFromState<TState>(TState state, out Exception exc)
        {
            exc = state as Exception;
            return exc != null;
        }

        private static bool TryParseProcessDetailsFromState<TState>(TState state, out ProcessDetails details)
        {
            details = null;
            EventContext telemetryContext = state as EventContext;
            if (telemetryContext != null)
            {
                if (telemetryContext.Properties.TryGetValue("process", out object processInfo))
                {
                    dynamic processObject = processInfo as dynamic;
                    if (processObject != null)
                    {
                        details = new ProcessDetails
                        {
                            Id = processObject.id,
                            ExitCode = processObject.exitCode,
                            CommandLine = processObject.command,
                            StandardOutput = processObject.standardOutput,
                            StandardError = processObject.standardError,
                            WorkingDirectory = processObject.workingDir,
                        };
                    }
                }
            }

            return details != null;
        }

        private static void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception, bool disableColors = false, bool includeTimestamp = false, bool includeCallstack = false)
        {
            lock (ConsoleLogger.SyncRoot)
            {
                // Example output format of OOB ConsoleLogger:
                //
                // info: Microsoft.AspNetCore.Hosting.Internal.WebHost[1]
                //       Request starting HTTP / 1.1 GET http://localhost:4500/api/todo/0
                // info: Microsoft.AspNetCore.Mvc.Internal.ControllerActionInvoker[1]
                //       Executing action method TodoApi.Controllers.TodoController.GetById(TodoApi) with arguments(0) -ModelState is Valid
                // info: TodoApi.Controllers.TodoController[1002]
                //       Getting item 0

                try
                {
                    if (!disableColors)
                    {
                        System.Console.ForegroundColor = ConsoleLogger.GetTextColor(logLevel);
                    }

                    string outputMessage = ConsoleLogger.GetMessage(
                        message: message,
                        logLevel: logLevel,
                        eventId: eventId,
                        logName: logName,
                        exception: exception,
                        includeTimestamp: includeTimestamp,
                        includeCallstack: includeCallstack);

                    switch (logLevel)
                    {
                        case LogLevel.Warning:
                        case LogLevel.Error:
                        case LogLevel.Critical:
                            Console.Error.WriteLine(outputMessage);
                            break;

                        default:
                            Console.WriteLine(outputMessage);
                            break;
                    }
                }
                finally
                {
                    if (!disableColors)
                    {
                        System.Console.ForegroundColor = ConsoleLogger.DefaultFontColor;
                    }
                }
            }
        }
    }
}
