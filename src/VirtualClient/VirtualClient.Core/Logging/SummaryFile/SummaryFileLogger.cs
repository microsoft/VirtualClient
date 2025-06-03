// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
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
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// An <see cref="ILogger"/> implementation for writing metrics data to a CSV file.
    /// </summary>
    public class SummaryFileLogger : ILogger, IFlushableChannel, IDisposable
    {
        internal const string DefaultFileName = "summary.txt";
        internal const int MaxLineLength = 250;
        private static readonly Encoding ContentEncoding = Encoding.UTF8;
        private static readonly string DashLine = new string('-', 100);
        private static readonly string AsteriskLine = new string('*', 100);

        private IList<(string, string, string)> components = new List<(string, string, string)>();
        private IList<(string, Metric)> metricsCache = new List<(string, Metric)>();
        private ConcurrentBuffer buffer;
        private string fileDirectory;
        private string filePath;
        private IAsyncPolicy fileAccessRetryPolicy;
        private IFileSystem fileSystem;
        private Task flushTask;
        private bool initialized;
        private SemaphoreSlim semaphore;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryFileLogger"/> class.
        /// </summary>
        /// <param name="filePath">The path where the summary file should be written.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient errored attempts to access the summary file for write operations.</param>
        public SummaryFileLogger(string filePath, IAsyncPolicy retryPolicy = null)
        {
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));

            string effectiveFilePath = Path.GetFullPath(filePath);

            this.filePath = effectiveFilePath;
            this.fileDirectory = Path.GetDirectoryName(effectiveFilePath);
            this.fileSystem = new FileSystem();
            this.buffer = new ConcurrentBuffer();
            this.fileAccessRetryPolicy = retryPolicy ?? Policy.Handle<IOException>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries));
            this.semaphore = new SemaphoreSlim(1, 1);

            VirtualClientRuntime.CleanupTasks.Add(new Action_(() =>
            {
                // this.WriteFinalSummary();
                this.Flush();
            }));
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
                try
                {
                    this.semaphore.Wait();
                    string message = string.Empty;
                    if (eventId.Id == (int)LogType.Metric)
                    {
                        if (eventId.Name.EndsWith("SucceededOrFailed", StringComparison.OrdinalIgnoreCase))
                        {
                            // these are the component "Succeeded or Failed"
                            message = this.CreateSucceededOrFailedMessage(eventContext);
                        }
                        else if (eventId.Name.EndsWith("ScenarioResult", StringComparison.OrdinalIgnoreCase))
                        {
                            this.CacheMetric(eventContext);
                        }
                    }
                    else if (eventId.Id == (int)LogType.MetricsCollection)
                    {
                        if (eventId.Name.EndsWith("LogMetricsEnd", StringComparison.OrdinalIgnoreCase))
                        {
                            message = this.CreateMetricsMessage(eventContext);
                        }
                    }
                    else if (eventId.Id == (int)LogType.Error)
                    {
                        message = SummaryFileLogger.CreateErrorMessage(eventId, eventContext);
                    }
                    else if (eventId.Id == (int)LogType.Trace)
                    {
                        if (eventId.Name.Equals("ProfileExecution.Begin", StringComparison.OrdinalIgnoreCase))
                        {
                            message = SummaryFileLogger.CreateProfileBeginMessage(eventContext);
                        }
                        else if (eventId.Name.EndsWith(".ProcessDetails", StringComparison.OrdinalIgnoreCase))
                        {
                            message = SummaryFileLogger.CreateProcessMessage(eventContext);
                        }
                        else if (eventId.Name.StartsWith("Exit Code: ", StringComparison.OrdinalIgnoreCase))
                        {
                            this.WriteFinalSummary(eventId.Name);
                            // message = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + $"  |   *** Virtual Client {eventId.Name}" + Environment.NewLine;
                        }
                    }

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

        private void WriteFinalSummary(string exitCode)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendMessage(SummaryFileLogger.AsteriskLine);

            int maxLength = this.components.Max(m => m.Item1.Length + m.Item2.Length) + 10;
            foreach (var component in this.components)
            {
                builder.AppendMessage(
                    $"Component: {component.Item1} -> {component.Item2} {new string(' ', maxLength - component.Item1.Length - component.Item2.Length)} | {component.Item3} |");
            }

            builder.AppendMessage(exitCode);
            builder.AppendMessage(SummaryFileLogger.AsteriskLine);
            this.buffer.Append(builder.ToString());
        }

        private static string CreateProcessMessage(EventContext context)
        {
            string tab = new string(' ', 4);
            StringBuilder builder = new StringBuilder();
            var process = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(context.Properties["process"]));
            // string durationMs = context.Properties["durationMs"].ToString();
            builder.AppendMessage(SummaryFileLogger.DashLine);
            builder.AppendMessage("Process Details");
            builder.AppendMessage(tab + $"PID          : {process["id"]}");
            builder.AppendMessage(tab + $"Workding Dir : {process["workingDir"]}");
            builder.AppendMessage(tab + $"Command      : {process["command"]}");
            builder.AppendMessage(tab + $"Exit Code    : {process["exitCode"]}");
            // builder.AppendMessage($"Duration     : {durationMs} ms");

            builder.AppendMessage(tab + "Standard Output:");
            AppendOutput(process["standardOutput"].ToString(), builder);

            builder.AppendMessage(tab + "Standard Error:");
            AppendOutput(process["standardError"].ToString(), builder);

            return builder.ToString();

            static void AppendOutput(string text, StringBuilder sb)
            {
                const int maxLen = 150;
                string tab2 = new string(' ', 8);
                List<string> lines = new List<string>();

                using (StringReader reader = new StringReader(text))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        while (line.Length > maxLen)
                        {
                            lines.Add(line.Substring(0, maxLen));
                            line = line.Substring(maxLen);
                        }

                        lines.Add(line);
                    }
                }

                if (lines.Count > 6)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        sb.AppendMessage(tab2 + lines[i]);
                    }

                    sb.AppendMessage(tab2 + tab2 + "... (output truncated) ...");
                    for (int i = lines.Count - 3; i < lines.Count; i++)
                    {
                        sb.AppendMessage(tab2 + lines[i]);
                    }
                }
                else
                {
                    foreach (var l in lines)
                    {
                        sb.AppendMessage(tab2 + l);
                    }
                }

                if (lines.Count == 0)
                {
                    sb.AppendMessage(tab2 + "(none)");
                }
            }
        }

        private static string CreateProfileBeginMessage(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();

            messageBuilder.AppendMessage($"Virtual Client Version: {context.Properties["appVersion"].ToString()}");
            messageBuilder.AppendMessage($"Client Id: {context.Properties["clientId"].ToString()}");
            messageBuilder.AppendMessage($"Profile: {context.Properties["executionProfile"].ToString()}");
            messageBuilder.AppendMessage($"Execution Arguments: {context.Properties["executionArguments"].ToString()}");
            messageBuilder.AppendMessage($"Experiment Id: {context.Properties["experimentId"].ToString()}");

            var dependencies = context.Properties["executionProfileDependencies"] as JArray;

            if (dependencies != null)
            {
                var types = dependencies.Select(m => m["type"]?.ToString());
                messageBuilder.AppendMessage($"Dependencies: {string.Join(",", types)}");
            }

            var actions = context.Properties["executionProfileActions"] as JArray;

            if (actions != null)
            {
                var types = actions.Select(m => m["type"]?.ToString());
                messageBuilder.AppendMessage($"Actions: {string.Join(",", types)}");
            }

            var monitors = context.Properties["executionProfileMonitors"] as JArray;

            if (monitors != null)
            {
                var types = monitors.Select(m => m["type"]?.ToString());
                messageBuilder.AppendMessage($"Monitors: {string.Join(",", types)}");
            }

            return messageBuilder.ToString();
        }

        private string CreateSucceededOrFailedMessage(EventContext context)
        {
            StringBuilder messageBuilder = new StringBuilder();

            string componentName = context.Properties["toolName"].ToString();
            string scenarioName = context.Properties["scenarioName"].ToString();
            string outcome = context.Properties["metricName"].ToString();
            outcome = outcome == "Succeeded" ? "PASS" : "* Failed *";
            TimeSpan duration = (DateTime)context.Properties["scenarioEndTime"] - (DateTime)context.Properties["scenarioStartTime"];

            messageBuilder.AppendLine();
            messageBuilder.AppendMessage($"Component : {componentName} -> {scenarioName}");
            messageBuilder.AppendMessage(SummaryFileLogger.DashLine);
            messageBuilder.AppendMessage($"Duration  : {duration.ToString()}");
            messageBuilder.AppendMessage($"Outcome   : {outcome}");
            messageBuilder.AppendLine();

            this.components.Add((componentName, scenarioName, outcome));

            return messageBuilder.ToString();
        }

        private void CacheMetric(EventContext context)
        {
            // Only cache critical metrics to filter out monitor data and non-critical metrics
            if (context.Properties["metricVerbosity"].ToString() == "0")
            {
                string scenarioName = context.Properties["scenarioName"].ToString();
                string metricName = context.Properties["metricName"].ToString();
                double metricValue = (double)context.Properties["metricValue"];
                string metricUnit = context.Properties["metricUnit"].ToString();

                this.metricsCache.Add((scenarioName, new Metric(metricName, metricValue, metricUnit)));
            }
        }

        private string CreateMetricsMessage(EventContext context)
        {
            string tab = new string(' ', 4);
            string outputTable = Environment.NewLine + "Metrics:" + Environment.NewLine + tab;
            DataTable table = new DataTable();
            table.Columns.Add("Scenario", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Value", typeof(double));
            table.Columns.Add("Unit", typeof(string));

            foreach ((string scenario, Metric metric) in this.metricsCache)
            {
                DataRow row = table.NewRow();
                row["Scenario"] = scenario; // You can modify this as needed
                row["Name"] = metric.Name;
                row["Value"] = metric.Value;
                row["Unit"] = metric.Unit;
                table.Rows.Add(row);
            }

            Dictionary<string, int> colWidths = new Dictionary<string, int>();
            foreach (DataColumn col in table.Columns)
            {
                outputTable += ("| " + col.ColumnName);
                var maxLabelSize = table.Rows.OfType<DataRow>()
                        .Select(m => (m.Field<object>(col.ColumnName)?.ToString() ?? string.Empty).Length)
                        .OrderByDescending(m => m).FirstOrDefault();

                maxLabelSize = Math.Max(col.ColumnName.Length, maxLabelSize);

                colWidths.Add(col.ColumnName, maxLabelSize);
                for (int i = 0; i < maxLabelSize - col.ColumnName.Length + 1; i++)
                {
                    outputTable += " ";
                }
            }

            outputTable += "|" + Environment.NewLine + tab;
            outputTable += new string('-', colWidths.Values.Sum() + 13) + Environment.NewLine + tab; // 13 is the extra width for the spaces and pipes

            foreach (DataRow dataRow in table.Rows)
            {
                for (int j = 0; j < dataRow.ItemArray.Length; j++)
                {
                    outputTable += "| " + dataRow.ItemArray[j];
                    for (int i = 0; i < colWidths[table.Columns[j].ColumnName] - dataRow.ItemArray[j].ToString().Length + 1; i++)
                    {
                        outputTable += " ";
                    }
                }

                outputTable += "|" + Environment.NewLine + tab;
            }

            outputTable += Environment.NewLine;

            StringBuilder messageBuilder = new StringBuilder();
            messageBuilder.AppendMessage(outputTable);
            return messageBuilder.ToString();
        }

        private static string CreateErrorMessage(EventId eventId, EventContext context)
        {
            string tab = new string(' ', 4);
            StringBuilder messageBuilder = new StringBuilder();
            string errorMessage = eventId.Name;
            var errors = context.Properties[EventContextExtensions.ErrorProperty] as List<object>;
            foreach (dynamic error in errors)
            {
                messageBuilder.AppendMessage($"*** Error ***");
                messageBuilder.AppendMessage(tab + $"Error Type: {error.errorType}");
                messageBuilder.AppendMessage(tab + $"Error Message: {error.errorMessage}");
            }

            messageBuilder.AppendMessage(tab + $"Error Call Stack: {context.Properties[EventContextExtensions.ErrorCallstackProperty]}");
            messageBuilder.AppendLine();

            return messageBuilder.ToString();
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

                        if (this.fileSystem.File.Exists(this.filePath))
                        {
                            await this.fileSystem.File.DeleteAsync(this.filePath);
                        }

                        using (FileSystemStream fileStream = this.fileSystem.FileStream.New(this.filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            if (fileStream.Length == 0)
                            {
                            }

                            byte[] bufferContents = SummaryFileLogger.ContentEncoding.GetBytes(this.buffer.ToString());

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
        }

    }

    /// <summary>
    /// 
    /// </summary>
    internal static class SummaryStringBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stringBuilder"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static void AppendMessage(this StringBuilder stringBuilder, string message)
        {
            string datetime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string prefix = $"{datetime}  |  ";
            int prefixLength = prefix.Length;

            // The max length of the actual message per line
            int maxMessageLength = SummaryFileLogger.MaxLineLength - prefixLength;

            string[] lines = message.Split(Environment.NewLine, StringSplitOptions.None);
            foreach (string line in lines)
            {
                int currentIndex = 0;
                while (currentIndex < line.Length)
                {
                    int remaining = line.Length - currentIndex;
                    int lengthToTake = Math.Min(remaining, maxMessageLength);
                    string lineSegment = line.Substring(currentIndex, lengthToTake);

                    if (currentIndex == 0)
                    {
                        stringBuilder.AppendLine(prefix + lineSegment);
                    }
                    else
                    {
                        stringBuilder.AppendLine(new string(' ', prefixLength) + lineSegment);
                    }

                    currentIndex += lengthToTake;
                }
            }
        }

        internal static void AppendLine(this StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(string.Empty);
        }
    }
}
