// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.OpenComputeProject;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing metrics data to a CSV file.
    /// </summary>
    public class OcpFileLogger : ILogger, IDisposable
    {
#pragma warning disable CS0169 // The field 'OcpFileLogger.initialized' is never used
        private static readonly Encoding ContentEncoding = Encoding.UTF8;
        private ConcurrentBuffer buffer;
        private string logDirectory;
        private string fileDirectory;
        private string fileExtension;
        private List<string> filePaths;
        private IAsyncPolicy fileAccessRetryPolicy;
        private IFileSystem fileSystem;
        private bool initialized;
        private long maxFileSizeBytes;
        private SemaphoreSlim semaphore;
        private bool disposed;
#pragma warning restore CS0169 // The field 'OcpFileLogger.initialized' is never used

        /// <summary>
        /// Initializes a new instance of the <see cref="OcpFileLogger"/> class.
        /// </summary>
        /// <param name="logDirectory">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="maximumFileSizeBytes">The maximum size of each CSV file (in bytes) before a new file (rollover) will be created.</param>
        /// <param name="retryPolicy"></param>
        public OcpFileLogger(string logDirectory, long maximumFileSizeBytes, IAsyncPolicy retryPolicy = null)
        {
            logDirectory.ThrowIfNullOrWhiteSpace(nameof(logDirectory));

            this.logDirectory = logDirectory;
            this.fileDirectory = Path.GetDirectoryName(logDirectory);
            this.fileExtension = Path.GetExtension(logDirectory);
            this.filePaths = new List<string>();
            this.fileSystem = new FileSystem();
            this.buffer = new ConcurrentBuffer();
            this.fileAccessRetryPolicy = retryPolicy ?? Policy.Handle<IOException>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries));
            this.maxFileSizeBytes = maximumFileSizeBytes;
            this.semaphore = new SemaphoreSlim(1, 1);
        }

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            EventContext eventContext = state as EventContext;

            if (eventContext != null)
            {
                if (eventId.Id == (int)LogType.Metrics)
                {
                    try
                    {
                        try
                        {
                            this.semaphore.Wait();
                            string message = OcpFileLogger.WriteMeasurement(eventContext);
                            this.buffer.Append(message);
                        }
                        finally
                        {
                            this.semaphore.Release();
                        }

                    }
                    catch
                    {
                        // Best effort. We do not want to crash the application on failures to access
                        // the CSV file.
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.semaphore.Dispose();
                    this.disposed = true;
                }
            }
        }

        private static string WriteMeasurement(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(Environment.NewLine);

            Measurement measurement = new Measurement()
            {
                Name = context.GetFieldValue("MetricName"),
                Unit = context.GetFieldValue("MetricUnit"),
                Value = context.GetFieldValue("MetricValue"),
            };

            return messageBuilder.ToString();
        }
    }
}
