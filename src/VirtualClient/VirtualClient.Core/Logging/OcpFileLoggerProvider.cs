// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can be used
    /// to write metrics data to a CSV file.
    /// </summary>
    public sealed class OcpFileLoggerProvider : ILoggerProvider
    {
        private string logDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcpFileLoggerProvider"/> class.
        /// <param name="logDirectory">The path to the CSV file to which the metrics should be written.</param>
        /// </summary>
        public OcpFileLoggerProvider(string logDirectory)
        {
            logDirectory.ThrowIfNullOrWhiteSpace(nameof(logDirectory));

            this.logDirectory = Path.Combine(logDirectory, "ocp");
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance that can be used to log events/messages
        /// to an Application Insights endpoint.
        /// </summary>
        /// <param name="categoryName">The logger events category.</param>
        /// <returns>
        /// An <see cref="ILogger"/> instance that can log events/messages to an Application
        /// Insights endpoint.
        /// </returns>
        public ILogger CreateLogger(string categoryName)
        {
            OcpFileLogger logger = new OcpFileLogger(this.logDirectory);
            VirtualClientRuntime.CleanupTasks.Add(new Action_(() =>
            {
                logger.Dispose();
            }));

            return logger;
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
