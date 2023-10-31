// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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
        private static readonly Encoding ContentEncoding = Encoding.UTF8;
        private ConcurrentQueue<(string FileName, string FileContent)> fileQueue;
        private string logDirectory;
        private string fileDirectory;
        private string fileExtension;
        private List<string> filePaths;
        private IAsyncPolicy fileAccessRetryPolicy;
        private IFileSystem fileSystem;
        private Task flushTask;
        private SemaphoreSlim semaphore;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="OcpFileLogger"/> class.
        /// </summary>
        /// <param name="logDirectory">The path to the CSV file to which the metrics should be written.</param>
        /// <param name="retryPolicy"></param>
        public OcpFileLogger(string logDirectory, IAsyncPolicy retryPolicy = null)
        {
            logDirectory.ThrowIfNullOrWhiteSpace(nameof(logDirectory));

            this.logDirectory = logDirectory;
            this.fileDirectory = Path.GetDirectoryName(logDirectory);
            this.fileExtension = Path.GetExtension(logDirectory);
            this.filePaths = new List<string>();
            this.fileSystem = new FileSystem();
            this.fileQueue = new ConcurrentQueue<(string FileName, string FileContent)>();
            this.fileAccessRetryPolicy = retryPolicy ?? Policy.Handle<IOException>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries));
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
                try
                {
                    this.semaphore.Wait();
                    string fileContent = string.Empty;
                    string fileName = string.Empty;
                    switch (eventId.Id)
                    {
                        case (int)LogType.Metrics:
                            this.WriteMeasurement(eventContext);
                            break;

                        case (int)LogType.Error:
                            this.WriteError(eventContext);
                            break;

                        case (int)LogType.Trace:
                            this.WriteError(eventContext);
                            break;

                        default: 
                            break;
                    }

                    if (this.flushTask == null)
                    {
                        this.flushTask = this.MonitorBufferAsync();
                    }

                }
                catch (Exception e)
                {
                    var d = e;
                    // Best effort. We do not want to crash the application on failures to access
                    // the CSV file.
                }
                finally
                {
                    this.semaphore.Release();
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

        private void WriteMeasurement(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(Environment.NewLine);

            Measurement measurement = new Measurement()
            {
                Name = context.GetFieldValue("MetricName"),
                Unit = context.GetFieldValue("MetricUnit"),
                Value = context.GetFieldValue("MetricValue"),
                Validators = new List<MeasurementValidator> { new MeasurementValidator() { } },
            };

            this.fileQueue.Enqueue((nameof(Measurement), JsonConvert.SerializeObject(measurement)));
        }

        private void WriteError(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.Append(Environment.NewLine);
            string errorText = context.GetFieldValue("errors");
            string stackTrace = context.GetFieldValue("errorCallstack");
            JArray jsonArray = JArray.Parse(errorText);

            foreach (JObject obj in jsonArray)
            {
                string errorType = obj.Value<string>("errorType");
                string errorMessage = obj.Value<string>("errorMessage");

                Error error = new Error()
                {
                    Message = errorMessage,
                    SourceLocation = new SourceLocation() { File = stackTrace },
                    Symptom = errorType,
                };

                this.fileQueue.Enqueue((nameof(Measurement), JsonConvert.SerializeObject(error)));
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
                        if (!this.fileSystem.Directory.Exists(this.logDirectory))
                        {
                            this.fileSystem.Directory.CreateDirectory(this.logDirectory);
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
            while (!this.fileQueue.IsEmpty)
            {
                try
                {
                    await this.semaphore.WaitAsync();
                        
                    if (this.fileQueue.TryDequeue(out var fileData))
                    {
                        string fileName = fileData.FileName;
                        string fileContent = fileData.FileContent;
                        await this.WriteFileAsync(fileName, fileContent);
                    }
                        
                }
                finally
                {
                    this.semaphore.Release();
                }
            }
        }

        private Task WriteFileAsync(string filename, string content)
        {
            return this.fileAccessRetryPolicy.ExecuteAsync(async () =>
            {
                string filePath = this.fileSystem.Path.Join(this.logDirectory, $"{filename.ToLower()}.jsonl");
                using (FileSystemStream fileStream = this.fileSystem.FileStream.New(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Position = fileStream.Length;
                    fileStream.Write(OcpFileLogger.ContentEncoding.GetBytes(content + Environment.NewLine));
                    await fileStream.FlushAsync();
                }
            });
        }
    }
}
