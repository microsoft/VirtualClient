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
    using Polly;
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
        /// The Script Path can be an absolute Path, or be relative to the Virtual Client Executable 
        /// or be relative to platformspecific package if the script is downloaded using DependencyPackageInstallation.
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
                return this.Parameters.GetValue<string>(nameof(this.ToolName), string.Empty);
            }

            set
            {
                this.Parameters[nameof(this.ToolName)] = value;
            }
        }

        /// <summary>
        /// True if VC should create elevated process
        /// to execute the script. Default = false.
        /// </summary>
        public bool RunElevated
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.RunElevated), false);
            }

            set
            {
                this.Parameters[nameof(this.RunElevated)] = value;
            }
        }

        /// <summary>
        /// The full path to the script executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the directory containing script executable.
        /// </summary>
        public string ExecutableDirectory { get; set; }

        /// <summary>
        /// A retry policy to apply to file access/move operations.
        /// </summary>
        public IAsyncPolicy FileOperationsRetryPolicy { get; set; } = RetryPolicies.FileOperations;

        /// <summary>
        /// Initializes the environment for execution of the provided script.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);

            string scriptFileLocation = string.Empty;
            if (!string.IsNullOrWhiteSpace(this.PackageName))
            {
                DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
                this.ExecutablePath = this.fileSystem.Path.GetFullPath(this.fileSystem.Path.Combine(workloadPackage.Path, this.ScriptPath));
            }
            else if (this.fileSystem.Path.IsPathRooted(this.ScriptPath))
            {
                this.ExecutablePath = this.ScriptPath;
            }
            else
            {
                this.ExecutablePath = this.fileSystem.Path.GetFullPath(this.fileSystem.Path.Combine(this.PlatformSpecifics.CurrentDirectory, this.ScriptPath));
            }

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload script was not found at '{this.ExecutablePath}'. The script cannot be executed " +
                    $"successfully without this binary/executable.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.ExecutableDirectory = this.fileSystem.Path.GetDirectoryName(this.ExecutablePath);
            this.ToolName = string.IsNullOrWhiteSpace(this.ToolName) ? $"{this.fileSystem.Path.GetFileNameWithoutExtension(this.ExecutablePath)}" : this.ToolName;
            await this.systemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = this.ExecutablePath;
                string commandArguments = SensitiveData.ObscureSecrets(this.CommandLine);

                telemetryContext
                    .AddContext(nameof(command), command)
                    .AddContext(nameof(commandArguments), commandArguments);

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.ExecutableDirectory, telemetryContext, cancellationToken, this.RunElevated))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, this.ToolName, logToFile: true);
                        process.ThrowIfWorkloadFailed();

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

                string metricsFilePath = this.Combine(this.ExecutableDirectory, "test-metrics.json");
                bool metricsFileFound = false;

                try
                {
                    if (this.fileSystem.File.Exists(metricsFilePath))
                    {
                        metricsFileFound = true;
                        telemetryContext.AddContext(nameof(metricsFilePath), metricsFilePath);
                        string results = await this.fileSystem.File.ReadAllTextAsync(metricsFilePath);

                        JsonMetricsParser parser = new JsonMetricsParser(results, this.Logger, telemetryContext);
                        IList<Metric> workloadMetrics = parser.Parse();

                        this.Logger.LogMetrics(
                            this.ToolName,
                            this.MetricScenario ?? this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            workloadMetrics,
                            null,
                            process.FullCommand(),
                            this.Tags,
                            telemetryContext);
                    }
                }
                finally
                {
                    telemetryContext.AddContext(nameof(metricsFileFound), metricsFileFound);
                }
            }
        }

        /// <summary>
        /// Captures the workload logs based on LogFiles parameter of ScriptExecutor.
        /// All the files inmatching sub-folders and all the matching files along with metrics file will be moved to the 
        /// central Virtual Client logs directory. If the content store (--cs) argument is used with Virtual Client, then
        /// the captured logs will also be uploaded to blob content store.
        /// </summary>
        protected async Task CaptureLogsAsync(CancellationToken cancellationToken)
        {
            // e.g.
            // /logs/anytool/executecustomscript1
            // /logs/anytool/executecustomscript2
            string destinitionLogsDir = this.PlatformSpecifics.GetLogsPath(this.ToolName.ToLower(), (this.Scenario ?? "customscript").ToLower());
            if (!this.fileSystem.Directory.Exists(destinitionLogsDir))
            {
                this.fileSystem.Directory.CreateDirectory(destinitionLogsDir);
            }

            foreach (string logPath in this.LogPaths.Split(";"))
            {
                if (string.IsNullOrWhiteSpace(logPath))
                {
                    continue;
                }

                string fullLogPath = this.fileSystem.Path.GetFullPath(this.fileSystem.Path.Combine(this.ExecutableDirectory, logPath));

                // Check for Matching Sub-Directories 
                if (this.fileSystem.Directory.Exists(fullLogPath))
                {
                    foreach (string logFilePath in this.fileSystem.Directory.GetFiles(fullLogPath, "*", SearchOption.AllDirectories))
                    {
                        this.RequestUploadAndMoveToLogsDirectory(logFilePath, destinitionLogsDir, cancellationToken);
                    }
                }

                // Check for Matching FileNames
                foreach (string logFilePath in this.fileSystem.Directory.GetFiles(this.ExecutableDirectory, logPath, SearchOption.AllDirectories))
                {
                    await this.RequestUploadAndMoveToLogsDirectory(logFilePath, destinitionLogsDir, cancellationToken);
                }
            }

            // Move test-metrics.json file if that exists
            string metricsFilePath = this.Combine(this.ExecutableDirectory, "test-metrics.json");
            if (this.fileSystem.File.Exists(metricsFilePath))
            {
                await this.RequestUploadAndMoveToLogsDirectory(metricsFilePath, destinitionLogsDir, cancellationToken);
            }
        }

        /// <summary>
        /// Requests a file upload.
        /// </summary>
        protected Task RequestUpload(string logPath)
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

            return this.RequestFileUploadAsync(descriptor);
        }

        /// <summary>
        /// Move the log files to central logs directory and Upload to Content Store
        /// </summary>
        protected async Task RequestUploadAndMoveToLogsDirectory(string sourcePath, string destinitionDirectory, CancellationToken cancellationToken)
        {
            if (string.Equals(sourcePath, this.ExecutablePath))
            {
                return;
            }

            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                await this.RequestUpload(sourcePath);
            }

            await (this.FileOperationsRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(() =>
            {
                // e.g.
                // /logs/anytool/executecustomscript1/2023-06-27T21-13-12-51001Z-CustomScript.sh
                // /logs/anytool/executecustomscript1/2023-06-27T21-15-36-12018Z-CustomScript.sh
                string fileName = Path.GetFileName(sourcePath);
                string destinitionPath = this.Combine(destinitionDirectory, BlobDescriptor.SanitizeBlobPath($"{DateTime.UtcNow.ToString("o").Replace('.', '-')}-{fileName}"));
                this.fileSystem.File.Move(sourcePath, destinitionPath, true);

                return Task.CompletedTask;
            });
        }
    }
}