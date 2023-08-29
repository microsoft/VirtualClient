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
    /// to write metrics data to a CSV file.
    /// </summary>
    public sealed class OcpFileLoggerProvider : ILoggerProvider
    {
        private string filePath;
        private long maxFileSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcpFileLoggerProvider"/> class.
        /// <param name="csvFilePath">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="maximumFileSizeBytes">The maximum size of each CSV file (in bytes) before a new file (rollover) will be created.</param>
        /// </summary>
        public OcpFileLoggerProvider(string csvFilePath, long maximumFileSizeBytes)
        {
            csvFilePath.ThrowIfNullOrWhiteSpace(nameof(csvFilePath));
            maximumFileSizeBytes.ThrowIfInvalid(nameof(maximumFileSizeBytes), (size) => size > 0);

            this.filePath = csvFilePath;
            this.maxFileSize = maximumFileSizeBytes;
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
            OcpFileLogger logger = new OcpFileLogger(this.filePath, this.maxFileSize);
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
