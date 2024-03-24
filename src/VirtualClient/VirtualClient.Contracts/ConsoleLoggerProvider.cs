// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A very thin settings passthrough provider as a stop gap to handle the missing obsoleted functionality .NET Core 2.2 and .NET Core 3.0 implementation.
    /// See https://github.com/aspnet/EntityFramework.Docs/pull/1164 for full discussion of issue.
    /// </summary>
    public sealed class ConsoleLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleLoggerProvider"/>.
        /// </summary>
        public ConsoleLoggerProvider(LogLevel minimumLogLevel = LogLevel.Information, bool disableColors = false)
        {
            this.IncludeTimestamps = true;
            this.DisableColors = disableColors;
            this.MinimumLogLevel = minimumLogLevel;
        }

        /// <summary>
        /// Gets or sets true/false whether to include scopes with the logging output.
        /// </summary>
        public bool IncludeScope { get; set; }

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
        /// Creates a console logger that automatically flushes each message logged to
        /// console output.
        /// </summary>
        /// <param name="categoryName">The name of the logging category.</param>
        /// <returns>
        /// A console logger that will automatically flush individual logged messages to console
        /// output.
        /// </returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger(categoryName, this.MinimumLogLevel, this.DisableColors)
            {
                IncludeTimestamps = this.IncludeTimestamps
            };
        }

        /// <summary>
        /// Required by <see cref="ILoggerProvider"/> interface. Does nothing.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
