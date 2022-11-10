// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using global::Serilog;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can
    /// be used to log events/messages to a local file.
    /// </summary>
    [LoggerSpecialization(Name = SpecializationConstant.StructuredLogging)]
    public sealed class SerilogFileLoggerProvider : ILoggerProvider
    {
        private LoggerConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogFileLoggerProvider"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger
        /// used by the <see cref="ILogger"/> instance.
        /// </param>
        public SerilogFileLoggerProvider(LoggerConfiguration configuration)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.configuration = configuration;
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
            return new SerilogFileLogger(this.configuration);
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
