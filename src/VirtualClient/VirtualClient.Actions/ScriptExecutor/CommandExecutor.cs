// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using MimeMapping;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// A generic command executor with file upload and metrics upload support.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class CommandExecutor : ExecuteCommand
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="ScriptExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CommandExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The Regex Based, semi-colon separated relative log file/Folder Paths
        /// </summary>
        public IEnumerable<string> LogPaths
        {
            get
            {
                this.Parameters.TryGetCollection<string>(nameof(this.LogPaths), out IEnumerable<string> logPaths);
                return logPaths;
            }
        }

        /// <summary>
        /// The ToolName for better logging and metadata
        /// </summary>
        public string ToolName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ToolName), string.Empty);
            }

            set
            {
                this.Parameters[nameof(this.ToolName)] = value;
            }
        }

        /// <summary>
        /// A retry policy to apply to file access/move operations.
        /// </summary>
        protected IAsyncPolicy FileOperationsRetryPolicy { get; set; } = RetryPolicies.FileOperations;

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DateTime startTime = DateTime.UtcNow;

            try
            {
                await base.ExecuteAsync(telemetryContext, cancellationToken);
            }
            finally
            {
                DateTime endTime = DateTime.UtcNow;
                await this.CaptureMetricsAsync(startTime, endTime, telemetryContext, cancellationToken);
                await this.CaptureLogsAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Captures the workload logs and the Workload metrics.
        /// </summary>
        protected Task CaptureMetricsAsync(DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.MetadataContract.AddForScenario(
                this.Scenario,
                SensitiveData.ObscureSecrets(this.Command));

            this.MetadataContract.Apply(telemetryContext);

            bool metricsFileFound = false;

            try
            {
                ////if (this.fileSystem.File.Exists(this.MetricsFilePath))
                ////{
                ////    metricsFileFound = true;
                ////    telemetryContext.AddContext("metricsFilePath", this.MetricsFilePath);
                ////    string results = await this.fileSystem.File.ReadAllTextAsync(this.MetricsFilePath);

                ////    if (!string.IsNullOrWhiteSpace(results))
                ////    {
                ////        JsonMetricsParser parser = new JsonMetricsParser(results, this.Logger, telemetryContext);
                ////        IList<Metric> workloadMetrics = parser.Parse();

                ////        this.Logger.LogMetrics(
                ////            this.ToolName,
                ////            (this.MetricScenario ?? this.Scenario) ?? "Script",
                ////            process.StartTime,
                ////            process.ExitTime,
                ////            workloadMetrics,
                ////            null,
                ////            process.FullCommand(),
                ////            this.Tags,
                ////            telemetryContext);
                ////    }
                ////}
            }
            finally
            {
                telemetryContext.AddContext(nameof(metricsFileFound), metricsFileFound);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Captures the workload logs based on LogFiles parameter of ScriptExecutor.
        /// All the files inmatching sub-folders and all the matching files along with metrics file will be moved to the 
        /// central Virtual Client logs directory.
        /// </summary>
        protected Task CaptureLogsAsync(CancellationToken cancellationToken)
        {
            // e.g.
            // /logs/anytool/executecustomscript1
            // /logs/anytool/executecustomscript2
            string destinitionLogsDir = this.Combine(this.GetLogDirectory(this.ToolName), DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm-ss"));

            IEnumerable<string> files = this.fileSystem.Directory.EnumerateFiles(this.GetLogDirectory(), "*.*", SearchOption.AllDirectories);

            ////if (this.LogPaths?.Any() == true)
            ////{
            ////    foreach (string logPath in this.LogPaths)
            ////    {
            ////        if (string.IsNullOrWhiteSpace(logPath))
            ////        {
            ////            continue;
            ////        }

            ////        string fullLogPath = this.fileSystem.Path.GetFullPath(this.fileSystem.Path.Combine(this.ExecutableDirectory, logPath));

            ////        // Check for Matching Sub-Directories 
            ////        if (this.fileSystem.Directory.Exists(fullLogPath))
            ////        {
            ////            foreach (string logFilePath in this.fileSystem.Directory.GetFiles(fullLogPath, "*", SearchOption.AllDirectories))
            ////            {
            ////                var logs = await this.MoveLogsAsync(logFilePath, destinitionLogsDir, cancellationToken, sourceRootDirectory: fullLogPath);
            ////                await this.RequestLogUploadsAsync(logs);
            ////            }
            ////        }

            ////        // Check for Matching FileNames
            ////        foreach (string logFilePath in this.fileSystem.Directory.GetFiles(this.ExecutableDirectory, logPath, SearchOption.AllDirectories))
            ////        {
            ////            var logs = await this.MoveLogsAsync(logFilePath, destinitionLogsDir, cancellationToken);
            ////            await this.RequestLogUploadsAsync(logs);
            ////        }
            ////    }
            ////}

            ////// Move test-metrics.json file if that exists
            ////string metricsFilePath = this.Combine(this.ExecutableDirectory, "test-metrics.json");
            ////if (this.fileSystem.File.Exists(metricsFilePath))
            ////{
            ////    var logs = await this.MoveLogsAsync(metricsFilePath, destinitionLogsDir, cancellationToken);
            ////    await this.RequestLogUploadsAsync(logs);
            ////}
            return Task.CompletedTask;
        }

        /// <summary>
        /// Requests a file upload for each of the log file paths provided.
        /// </summary>
        protected async Task RequestLogUploadsAsync(IEnumerable<string> logPaths)
        {
            if (logPaths?.Any() == true && this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                foreach (string logPath in logPaths)
                {
                    FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                        new FileContext(
                            this.fileSystem.FileInfo.New(logPath),
                            MimeUtility.GetMimeMapping(logPath),
                            Encoding.UTF8.WebName,
                            this.ExperimentId,
                            this.AgentId,
                            this.ToolName,
                            this.Scenario,
                            null,
                            this.Roles?.FirstOrDefault()));

                    await this.RequestFileUploadAsync(descriptor);
                }
            }
        }

        /////// <summary>
        /////// Move the log files to central logs directory (retaining source directory structure) and Upload to Content Store.
        /////// </summary>
        ////private async Task<IEnumerable<string>> MoveLogsAsync(string sourcePath, string destinationDirectory, CancellationToken cancellationToken, string sourceRootDirectory = null)
        ////{
        ////    List<string> targetLogs = new List<string>();
        ////    if (!string.Equals(sourcePath, this.ExecutablePath))
        ////    {
        ////        if (!this.fileSystem.Directory.Exists(destinationDirectory))
        ////        {
        ////            this.fileSystem.Directory.CreateDirectory(destinationDirectory);
        ////        }

        ////        string destPath = sourcePath;
        ////        await (this.FileOperationsRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
        ////        {
        ////            await Task.Run(() =>
        ////            {
        ////                string fileName = Path.GetFileName(sourcePath);

        ////                if (!string.IsNullOrEmpty(sourceRootDirectory))
        ////                {
        ////                    // Compute relative path from sourceRoot to sourcePath
        ////                    string relativePath = this.fileSystem.Path.GetRelativePath(sourceRootDirectory, sourcePath);
        ////                    string destDir = this.fileSystem.Path.Combine(destinationDirectory, this.fileSystem.Path.GetDirectoryName(relativePath));

        ////                    if (!this.fileSystem.Directory.Exists(destDir))
        ////                    {
        ////                        this.fileSystem.Directory.CreateDirectory(destDir);
        ////                    }

        ////                    destPath = this.Combine(destDir, fileName);
        ////                }
        ////                else
        ////                {
        ////                    destPath = this.Combine(destinationDirectory, fileName);
        ////                }

        ////                this.fileSystem.File.Move(sourcePath, destPath, true);
        ////                targetLogs.Add(destPath);
        ////            });
        ////        });
        ////    }

        ////    return targetLogs;
        ////}
    }
}