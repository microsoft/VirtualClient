// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A mock/test logger that can be used to verify logging mechanics in test scenarios.
    /// The logger tracks all events that are logged and is a list of those events itself.
    /// </summary>
    public class InMemoryLogger : List<Tuple<LogLevel, EventId, object, Exception>>, ILogger
    {
        /// <summary>
        /// Delegate allows a block of code to be defined an executed on all calls
        /// to the Log method.
        /// </summary>
        public Action<LogLevel, EventId, object, Exception> OnLog { get; set; }

        /// <summary>
        /// Delegate allows a block of code to be defined an executed on all calls
        /// to the Log method when an exception is included.
        /// </summary>
        public Action<LogLevel, EventId, object, Exception> OnLogError { get; set; }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns true if an event was logged whose event name/message matches.
        /// </summary>
        public IEnumerable<Tuple<LogLevel, EventId, object, Exception>> MessagesLogged(string message, LogLevel? level = null)
        {
            return this.Where(entry => entry.Item2.Name == message && (level == null || entry.Item1 == level));
        }

        /// <summary>
        /// Returns true if an event was logged whose event name/message matches.
        /// </summary>
        public IEnumerable<Tuple<LogLevel, EventId, object, Exception>> MessagesLogged(Regex message, LogLevel? level = null)
        {
            return this.Where(entry => message.IsMatch(entry.Item2.Name) && (level == null || entry.Item1 == level));
        }

        /// <summary>
        /// Returns true always.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Invokes the mock logging logic.
        /// </summary>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.Add(new Tuple<LogLevel, EventId, object, Exception>(logLevel, eventId, state, exception));
            this.OnLog?.Invoke(logLevel, eventId, state, exception);
        }
    }
}
