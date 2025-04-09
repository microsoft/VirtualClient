// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can be used
    /// to write summary file.
    /// </summary>
    public sealed class SummaryFileLoggerProvider : ILoggerProvider
    {
        private string filePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryFileLoggerProvider"/> class.
        /// <param name="filePath">The path to the CSV file to which the metrics should be written.</param>
        /// </summary>
        public SummaryFileLoggerProvider(string filePath)
        {
            this.filePath = filePath;
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
            SummaryFileLogger logger = new SummaryFileLogger(this.filePath);
            VirtualClientRuntime.CleanupTasks.Add(new Action_(() =>
            {
                logger.Flush();
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
