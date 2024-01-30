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
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Generic Script executor for Powershell, Bash etc scripts.
    /// </summary>
    public class ScriptExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="ScriptExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public ScriptExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The commandline arguments to be used with the Script executable
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// The relative Script Path to be used to initiate the script
        /// </summary>
        public string ScriptPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ScriptPath));
            }
        }

        /// <summary>
        /// The Regex Based, semi-colon separated relative log file/Folder Paths
        /// </summary>
        public string LogPaths
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.LogPaths));
            }
        }

        /// <summary>
        /// The ToolName for better logging and metadata
        /// </summary>
        public string ToolName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ToolName));
            }
        }

        /// <summary>
        /// The full path to the script executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the workload package.
        /// </summary>
        protected DependencyPath WorkloadPackage { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the provided script.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);

            this.WorkloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            this.ExecutablePath = this.Combine(this.WorkloadPackage.Path, this.ScriptPath);

            await this.systemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The script cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.ExecutablePath}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.ExecutablePath,
                    this.CommandLine,
                    this.WorkloadPackage.Path,
                    telemetryContext,
                    cancellationToken,
                    false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName, logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        if (!string.IsNullOrWhiteSpace(process.StandardError.ToString()))
                        {
                            this.Logger.LogWarning($"StandardError: {process.StandardError}", telemetryContext);
                        }

                        await this.CaptureMetricsAsync(process, telemetryContext, cancellationToken);
                        await this.CaptureLogsAsync(cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Captures the workload logs and the Workload metrics.
        /// </summary>
        protected async Task CaptureMetricsAsync(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    this.Scenario,
                    process.FullCommand());

                this.MetadataContract.Apply(telemetryContext);

                string metricsFilePath = this.Combine(this.WorkloadPackage.Path, "test-metrics.json");

                if (this.fileSystem.File.Exists(metricsFilePath))
                {
                    string results = await this.fileSystem.File.ReadAllTextAsync(metricsFilePath);

                    JsonMetricsParser parser = new JsonMetricsParser(results, this.Logger, telemetryContext);
                    IList<Metric> workloadMetrics = parser.Parse();

                    this.Logger.LogMetrics(
                        this.ToolName,
                        this.Scenario,
                        process.StartTime,
                        process.ExitTime,
                        workloadMetrics,
                        null,
                        process.FullCommand(),
                        this.Tags,
                        telemetryContext);
                }
                else
                {
                    this.Logger.LogWarning($"The {metricsFilePath} was not found on the system. No parsed metrics captured for {this.ToolName}.", telemetryContext);
                }
            }
        }

        /// <summary>
        /// Captures the workload logs based on LogFiles parameter of ScriptExecutor
        /// </summary>
        protected async Task CaptureLogsAsync(CancellationToken cancellationToken)
        {
            string destinitionLogsDir = this.Combine(this.PlatformSpecifics.LogsDirectory, this.ToolName.ToLower(), $"{this.Scenario.ToLower()}_{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}");
            if (!this.fileSystem.Directory.Exists(destinitionLogsDir))
            {
                this.fileSystem.Directory.CreateDirectory(destinitionLogsDir);
            }

            foreach (string logPath in this.LogPaths.Split(";"))
            {
                string fullLogPath = this.Combine(this.WorkloadPackage.Path, logPath);

                // Check for Matching Sub-Directories 
                if (this.fileSystem.Directory.Exists(fullLogPath))
                {
                    foreach (string logFilePath in this.fileSystem.Directory.GetFiles(fullLogPath, "*", SearchOption.AllDirectories))
                    {
                        if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                        {
                            await this.UploadLogAsync(blobManager, logFilePath, cancellationToken);
                        }

                        this.MoveFileToCentralLogsDirectory(logFilePath, destinitionLogsDir);
                    }
                }

                // Check for Matching FileNames
                foreach (string logFilePath in this.fileSystem.Directory.GetFiles(this.WorkloadPackage.Path, logPath, SearchOption.AllDirectories))
                {
                    if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                    {
                        await this.UploadLogAsync(blobManager, logFilePath, cancellationToken);
                    }

                    this.MoveFileToCentralLogsDirectory(logFilePath, destinitionLogsDir);
                }
            }
        }

        /// <summary>
        /// Upload Logs to Blob Storage
        /// </summary>
        protected Task UploadLogAsync(IBlobManager blobManager, string logPath, CancellationToken cancellationToken)
        {
            FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                new FileContext(
                    this.fileSystem.FileInfo.New(logPath),
                    HttpContentType.PlainText,
                    Encoding.UTF8.WebName,
                    this.ExperimentId,
                    this.AgentId,
                    this.ToolName,
                    this.Scenario,
                    null,
                    this.Roles?.FirstOrDefault()));

            return this.UploadFileAsync(blobManager, this.fileSystem, descriptor, cancellationToken, deleteFile: false);
        }

        /// <summary>
        /// Move the log files to central logs directory
        /// </summary>
        protected void MoveFileToCentralLogsDirectory(string sourcePath, string destinitionDirectory)
        {
            string fileName = Path.GetFileName(sourcePath);

            string destinitionPath = this.Combine(destinitionDirectory, fileName);
            this.fileSystem.File.Move(sourcePath, destinitionPath, true);
        }
    }
}