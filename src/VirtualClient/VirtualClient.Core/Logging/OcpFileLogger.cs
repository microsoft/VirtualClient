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
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing metrics data to a CSV file.
    /// </summary>
    public class OcpFileLogger : ILogger, IFlushableChannel, IDisposable
    {
        internal static readonly IEnumerable<MetricsCsvField> CsvFields;

        private static readonly AssemblyName LoggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
        private static readonly AssemblyName ExecutingAssembly = Assembly.GetEntryAssembly().GetName();
        private static readonly Encoding ContentEncoding = Encoding.UTF8;

        private ConcurrentBuffer buffer;
        private string filePath;
        private string fileNameNoExtension;
        private string fileDirectory;
        private string fileExtension;
        private List<string> filePaths;
        private IAsyncPolicy fileAccessRetryPolicy;
        private IFileSystem fileSystem;
        private Task flushTask;
        private bool initialized;
        private long maxFileSizeBytes;
        private SemaphoreSlim semaphore;
        private bool disposed;

        static OcpFileLogger()
        {
            OcpFileLogger.LoggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
            OcpFileLogger.ExecutingAssembly = Assembly.GetEntryAssembly().GetName();
            OcpFileLogger.CsvFields = new List<MetricsCsvField>
            {
                new MetricsCsvField("Timestamp", (ctx) => DateTime.UtcNow.ToString("o")),
                new MetricsCsvField("ExperimentId", "experimentId"),
                new MetricsCsvField("ClientId", "agentId"),
                new MetricsCsvField("Profile", "executionProfile"),
                new MetricsCsvField("ProfileName", "executionProfileName"),
                new MetricsCsvField("ToolName", "toolName"),
                new MetricsCsvField("ScenarioName", "scenarioName"),
                new MetricsCsvField("ScenarioStartTime", "scenarioStartTime"),
                new MetricsCsvField("ScenarioEndTime", "scenarioEndTime"),
                new MetricsCsvField("MetricCategorization", "metricCategorization"),
                new MetricsCsvField("MetricName", "metricName"),
                new MetricsCsvField("MetricValue", "metricValue"),
                new MetricsCsvField("MetricUnit", "metricUnit"),
                new MetricsCsvField("MetricDescription", "metricDescription"),
                new MetricsCsvField("MetricRelativity", "metricRelativity"),
                new MetricsCsvField("ExecutionSystem", "executionSystem"),
                new MetricsCsvField("OperatingSystemPlatform", "operatingSystemPlatform"),
                new MetricsCsvField("OperationId", (ctx) => ctx.ActivityId.ToString()),
                new MetricsCsvField("OperationParentId", (ctx) => ctx.ParentActivityId.ToString()),
                new MetricsCsvField("AppHost", propertyValue: Environment.MachineName),
                new MetricsCsvField("AppName", propertyValue: OcpFileLogger.ExecutingAssembly.Name),
                new MetricsCsvField("AppVersion", propertyValue: OcpFileLogger.ExecutingAssembly.Version.ToString()),
                new MetricsCsvField("AppTelemetryVersion", propertyValue: OcpFileLogger.LoggingAssembly.Version.ToString()),
                new MetricsCsvField("Tags", "tags")
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OcpFileLogger"/> class.
        /// </summary>
        /// <param name="csvFilePath">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="maximumFileSizeBytes">The maximum size of each CSV file (in bytes) before a new file (rollover) will be created.</param>
        /// <param name="retryPolicy"></param>
        public OcpFileLogger(string csvFilePath, long maximumFileSizeBytes, IAsyncPolicy retryPolicy = null)
        {
            csvFilePath.ThrowIfNullOrWhiteSpace(nameof(csvFilePath));

            this.filePath = csvFilePath;
            this.fileNameNoExtension = Path.GetFileNameWithoutExtension(csvFilePath);
            this.fileDirectory = Path.GetDirectoryName(csvFilePath);
            this.fileExtension = Path.GetExtension(csvFilePath);
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

        /// <summary>
        /// Flushes the remaining buffer content to the file system.
        /// </summary>
        /// <param name="timeout">Not used.</param>
        public void Flush(TimeSpan? timeout = null)
        {
            this.FlushBufferAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
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
                            string message = OcpFileLogger.CreateMessage(eventContext);
                            this.buffer.Append(message);
                        }
                        finally
                        {
                            this.semaphore.Release();
                        }

                        if (this.flushTask == null)
                        {
                            this.flushTask = this.MonitorBufferAsync();
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

        internal static string CreateMessage(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(Environment.NewLine);
            messageBuilder.AppendJoin(',', OcpFileLogger.CsvFields.Select(field => $"\"{field.GetFieldValue(context)}\""));

            return messageBuilder.ToString();
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

        private Task MonitorBufferAsync()
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (!this.initialized)
                        {
                            this.InitializeFilePaths();
                            this.initialized = true;
                        }

                        await Task.Delay(300);
                        await this.FlushBufferAsync();
                    }
                    catch
                    {
                        // Best effort. We do not want to crash the application on failures to access
                        // the CSV file.
                    }
                }
            });
        }

        private async Task FlushBufferAsync()
        {
            if (this.buffer.Length > 0)
            {
                await this.fileAccessRetryPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        await this.semaphore.WaitAsync();
                        string latestFilePath = this.filePaths.Last();

                        using (FileSystemStream fileStream = this.fileSystem.FileStream.New(latestFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            if (fileStream.Length == 0)
                            {
                                // We need to ensure that the CSV headers are written to the file first. Here, we are using
                                // a simple technique to check if the file (at the path provided) was initialized or not.
                                // The very first time the file is opened, the stream will have a length of zero bytes.
                                string columnHeaders = string.Join(",", OcpFileLogger.CsvFields.Select(field => $"\"{field.ColumnName}\""));
                                fileStream.Write(OcpFileLogger.ContentEncoding.GetBytes(columnHeaders));
                            }

                            byte[] bufferContents = OcpFileLogger.ContentEncoding.GetBytes(this.buffer.ToString());

                            if (fileStream.Length + bufferContents.Length > this.maxFileSizeBytes)
                            {
                                this.filePaths.Add(Path.Combine(this.fileDirectory, $"{this.fileNameNoExtension}_{this.filePaths.Count}{this.fileExtension}"));
                            }

                            fileStream.Position = fileStream.Length;
                            fileStream.Write(bufferContents);
                            await fileStream.FlushAsync();

                            this.buffer.Clear();
                        }
                    }
                    finally
                    {
                        this.semaphore.Release();
                    }
                });
            }
        }

        private void InitializeFilePaths()
        {
            if (!this.fileSystem.Directory.Exists(this.fileDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.fileDirectory);
            }

            IEnumerable<string> matchingFiles = this.fileSystem.Directory.EnumerateFiles(this.fileDirectory, $"{this.fileNameNoExtension}*{this.fileExtension}");
            if (matchingFiles?.Any() != true)
            {
                this.filePaths.Add(this.filePath);
            }
            else
            {
                this.filePaths.AddRange(matchingFiles.OrderBy(file => file));
            }
        }
    }
}
