// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Contracts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can be used
    /// to write summary log files.
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
        /// Creates an <see cref="ILogger"/> instance that can be used write events to a 
        /// summary log.
        /// </summary>
        /// <param name="categoryName">The logger events category.</param>
        public ILogger CreateLogger(string categoryName)
        {
            string effectiveFilePath = this.filePath;

            if (string.IsNullOrWhiteSpace(effectiveFilePath))
            {
                PlatformSpecifics platformSpecifics = VirtualClientRuntime.PlatformSpecifics
                   ?? new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

                string experimentId = VirtualClientRuntime.ExperimentId;
                string logsPath = platformSpecifics.GetLogsPath();
                string summaryFileName = "summary.txt";

                if (!string.IsNullOrWhiteSpace(experimentId))
                {
                    summaryFileName = $"{experimentId}-summary.txt";
                }

                effectiveFilePath = platformSpecifics.Combine(logsPath, summaryFileName);
            }

            SummaryFileLogger logger = new SummaryFileLogger(effectiveFilePath);
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
