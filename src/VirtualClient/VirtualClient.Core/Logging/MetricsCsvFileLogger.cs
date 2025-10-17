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
    using VirtualClient.Contracts.Metadata;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing metrics data to a CSV file.
    /// </summary>
    public class MetricsCsvFileLogger : ILogger, IFlushableChannel, IDisposable
    {
        internal static readonly IEnumerable<MetricsCsvField> CsvFields;

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

        static MetricsCsvFileLogger()
        {
            MetricsCsvFileLogger.ExecutingAssembly = Assembly.GetEntryAssembly().GetName();
            MetricsCsvFileLogger.CsvFields = new List<MetricsCsvField>
            {
                new MetricsCsvField("Timestamp", (ctx) => ParseTimestamp(ctx, "timestamp")),
                new MetricsCsvField("ExperimentId", "experimentId"),
                new MetricsCsvField("ExecutionSystem", "executionSystem"),
                new MetricsCsvField("ProfileName", "executionProfileName"),
                new MetricsCsvField("ClientId", "clientId"),
                new MetricsCsvField("ToolName", "toolName"),
                new MetricsCsvField("ToolVersion", "toolVersion"),
                new MetricsCsvField("ScenarioName", "scenarioName"),
                new MetricsCsvField("ScenarioStartTime", "scenarioStartTime"),
                new MetricsCsvField("ScenarioEndTime", "scenarioEndTime"),
                new MetricsCsvField("MetricName", "metricName"),
                new MetricsCsvField("MetricValue", "metricValue"),
                new MetricsCsvField("MetricUnit", "metricUnit"),
                new MetricsCsvField("MetricCategorization", "metricCategorization"),
                new MetricsCsvField("MetricDescription", "metricDescription"),
                new MetricsCsvField("MetricRelativity", "metricRelativity"),
                new MetricsCsvField("MetricVerbosity", "metricVerbosity"),
                new MetricsCsvField("AppHost", propertyValue: Environment.MachineName),
                new MetricsCsvField("AppName", propertyValue: MetricsCsvFileLogger.ExecutingAssembly.Name),
                new MetricsCsvField("AppVersion", propertyValue: MetricsCsvFileLogger.ExecutingAssembly.Version.ToString()),
                new MetricsCsvField("OperatingSystemPlatform", "operatingSystemPlatform"),
                new MetricsCsvField("PlatformArchitecture", "platformArchitecture"),
                new MetricsCsvField("SeverityLevel", (ctx) => ParseSeverity(ctx, "severityLevel")),
                new MetricsCsvField("OperationId", (ctx) => ctx.ActivityId.ToString()),
                new MetricsCsvField("OperationParentId", (ctx) => ctx.ParentActivityId.ToString()),
                new MetricsCsvField("Metadata", (ctx) => ParseMetadata(ctx, MetadataContract.DefaultCategory)),
                new MetricsCsvField("Metadata_Host", (ctx) => ParseMetadata(ctx, MetadataContract.HostCategory)),
                new MetricsCsvField("ToolResults", "toolResults"),
                new MetricsCsvField("Tags", "tags")
            };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLogger"/> class.
        /// </summary>
        /// <param name="csvFilePath">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="maximumFileSizeBytes">The maximum size of each CSV file (in bytes) before a new file (rollover) will be created.</param>
        /// <param name="retryPolicy"></param>
        public MetricsCsvFileLogger(string csvFilePath, long maximumFileSizeBytes, IAsyncPolicy retryPolicy = null)
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
                if (eventId.Id == (int)LogType.Metric)
                {
                    try
                    {
                        try
                        {
                            this.semaphore.Wait();
                            string message = MetricsCsvFileLogger.CreateMessage(eventContext);
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
            messageBuilder.AppendJoin(',', MetricsCsvFileLogger.CsvFields.Select(field => $"\"{field.GetFieldValue(context)?.Replace("\"", "\"\"")}\""));

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

        private static string ParseMetadata(EventContext ctx, string key)
        {
            string metadata = string.Empty;

            try
            {
                if (ctx.Properties.ContainsKey(key))
                {
                    IDictionary<string, object> metadataSet = ctx.Properties[key] as IDictionary<string, object>;
                    if (metadataSet != null)
                    {
                        List<string> convertibleValues = new List<string>();
                        foreach (var entry in metadataSet)
                        {
                            // Metadata collections can contain objects that are more than simply
                            // key/value pairs of simple primitive data types such as strings and integers.
                            // We do not attempt to support the more advanced data types for the moment.
                            if (entry.Value is IConvertible)
                            {
                                convertibleValues.Add($"{entry.Key}={entry.Value}");
                            }
                        }

                        if (convertibleValues.Any())
                        {
                            metadata = string.Join(';', convertibleValues);
                        }
                    }
                }
            }
            catch
            {
                // Best effort only.
            }

            return metadata;
        }

        private static string ParseSeverity(EventContext ctx, string key)
        {
            int severity = (int)LogLevel.Information;

            try
            {
                if (ctx.Properties.ContainsKey(key))
                {
                    IConvertible value = ctx.Properties[key] as IConvertible;
                    if (int.TryParse(value?.ToString(), out int level))
                    {
                        severity = (int)level;
                    }
                    else if (Enum.TryParse<LogLevel>(value?.ToString(), out LogLevel logLevel))
                    {
                        severity = (int)logLevel;
                    }
                }
            }
            catch
            {
                // Best effort only.
            }

            return severity.ToString();
        }

        private static string ParseTimestamp(EventContext ctx, string key)
        {
            string timestamp = null;

            try
            {
                if (ctx.Properties.ContainsKey(key))
                {
                    IConvertible value = ctx.Properties[key] as IConvertible;
                    if (value != null)
                    {
                        if (value is string)
                        {
                            timestamp = value.ToString();
                        }
                        else if (value is DateTime)
                        {
                            timestamp = ((DateTime)value).ToString("o");
                        }
                    }
                }
            }
            catch
            {
                // Best effort only.
            }

            return timestamp ?? DateTime.UtcNow.ToString("o");
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
                                string columnHeaders = string.Join(",", MetricsCsvFileLogger.CsvFields.Select(field => $"\"{field.ColumnName}\""));
                                fileStream.Write(MetricsCsvFileLogger.ContentEncoding.GetBytes(columnHeaders));
                            }

                            byte[] bufferContents = MetricsCsvFileLogger.ContentEncoding.GetBytes(this.buffer.ToString());

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

    internal class MetricsCsvField
    {
        // We are very purposefully using member variables here vs. properties to keep
        // the evaluation of the EventContext objects as efficient as possible
        // (i.e. avoiding unnecessary method callstacks).
        private string propertyName;
        private string propertyValue;
        private Func<EventContext, string> propertyQuery;

        public MetricsCsvField(string columnName, Func<EventContext, string> query)
        {
            this.ColumnName = columnName;
            this.propertyQuery = query;
        }

        public MetricsCsvField(string columnName, string propertyName = null, string propertyValue = null)
        {
            this.ColumnName = columnName;
            this.propertyName = propertyName;
            this.propertyValue = propertyValue;
        }

        public string ColumnName { get; }

        public string GetFieldValue(EventContext context)
        {
            string value = null;
            if (context?.Properties != null)
            {
                if (this.propertyValue != null)
                {
                    value = this.propertyValue;
                }
                else if (this.propertyName != null)
                {
                    if (context.Properties.TryGetValue(this.propertyName, out object propertyValue) && propertyValue != null)
                    {
                        if (propertyValue is DateTime)
                        {
                            value = ((DateTime)propertyValue).ToString("o");
                        }
                        else
                        {
                            value = propertyValue.ToString();
                        }
                    }
                }
                else if (this.propertyQuery != null)
                {
                    value = this.propertyQuery.Invoke(context);
                }
            }

            return value;
        }
    }
}
