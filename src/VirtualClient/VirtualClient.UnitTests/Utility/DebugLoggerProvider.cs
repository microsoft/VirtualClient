// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests.Utility
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Text;
    using VirtualClient.Common.Telemetry;

    public sealed class DebugLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Creates a custom console logger
        /// </summary>
        /// <param name="categoryName"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new CustomDebugLogger(categoryName);
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// A custom console logger
    /// </summary>
    public class CustomDebugLogger : ILogger
    {
        private readonly string categoryName;

        /// <summary>
        /// Creates a custom console logger
        /// </summary>
        /// <param name="categoryName"></param>
        public CustomDebugLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        /// <summary>
        /// Logs one message
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Func<TState, Exception, string> defaultFormatter = (s, e) =>
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                stringBuilder.Append(DateTime.Now);
                stringBuilder.Append("] ");
                stringBuilder.Append("[");
                stringBuilder.Append(logLevel.ToString());
                stringBuilder.Append("] ");
                stringBuilder.AppendLine(eventId.Name);

                if (e != null)
                {
                    stringBuilder.AppendLine(e.ToString());
                }
                else if (s != null)
                {
                    if (s is EventContext)
                    {
                        EventContext ev = s as EventContext;
                        foreach (var prop in ev.Properties)
                        {
                            stringBuilder.AppendLine(string.Format("\t\t{0}={1}", prop.Key, prop.Value));
                        }
                    }
                }

                return stringBuilder.ToString();
            };

            Func<TState, Exception, string> formatterToUse = null;
            if (formatter == null)
            {
                formatterToUse = defaultFormatter;
            }
            else
            {
                formatterToUse = formatter;
            }

            if (!this.IsEnabled(logLevel))
            {
                return;
            }

            string lineToLog = formatterToUse(state, exception);
            System.Diagnostics.Debug.WriteLine(lineToLog);
        }

        /// <summary>
        /// Checks if this logger is enabled (returns true)
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Not implemented (returns null)
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}