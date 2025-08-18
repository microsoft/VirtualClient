// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using global::Serilog;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can
    /// be used to log events/messages to a local file.
    /// </summary>
    [LoggerAlias("File")]
    [LoggerSpecialization(Name = SpecializationConstant.StructuredLogging)]
    public sealed class SerilogFileLoggerProvider : ILoggerProvider, IDisposable
    {
        private LoggerConfiguration configuration;
        private LogLevel minumumLogLevel;
        private IList<IDisposable> disposables;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerilogFileLoggerProvider"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Configuration settings that will be supplied to the Serilog logger
        /// used by the <see cref="ILogger"/> instance.
        /// </param>
        /// <param name="level">The minimum logging severity level.</param>
        public SerilogFileLoggerProvider(LoggerConfiguration configuration, LogLevel level)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.configuration = configuration;
            this.minumumLogLevel = level;
            this.disposables = new List<IDisposable>();
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
            Serilog.Core.Logger logger = this.configuration.CreateLogger();
            this.disposables.Add(logger);
            return new SerilogFileLogger(logger, this.minumumLogLevel);
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    foreach (IDisposable disposable in this.disposables)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch
                        {
                            // Best effort
                        }
                    }

                    this.disposed = true;
                }
            }
        }
    }
}
