// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using global::Serilog;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can
    /// be used to log events/messages to a local csv file.
    /// </summary>
    [LoggerSpecialization(Name = SpecializationConstant.StructuredLogging)]
    public sealed class SerilogCsvFileLoggerProvider : ILoggerProvider
    {
        private LoggerConfiguration configuration;

        private IEnumerable<string> csvLogHeaders;

        private string rollingLogFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogCsvFileLoggerProvider"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger
        /// used by the <see cref="ILogger"/> instance.
        /// </param>
        /// <param name="csvHeaders"> Headers for the CSV Log File</param>
        /// <param name="rollingLogFilePath"> Rolling File Path for CSV Log File</param>
        public SerilogCsvFileLoggerProvider(LoggerConfiguration configuration, IEnumerable<string> csvHeaders, string rollingLogFilePath)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.configuration = configuration;
            this.csvLogHeaders = csvHeaders;
            this.rollingLogFilePath = rollingLogFilePath;
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
            return new SerilogCsvFileLogger(this.configuration, this.csvLogHeaders, this.rollingLogFilePath);
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
