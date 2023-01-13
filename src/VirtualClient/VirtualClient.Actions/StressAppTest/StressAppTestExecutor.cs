namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The StressAppTest workload executor.
    /// </summary>
    public class StressAppTestExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="StressAppTestExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public StressAppTestExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(StressAppTestExecutor.CommandLine));
            }
        }

        /// <summary>
        /// The TimeInSeconds argument defined in the profile.
        /// </summary>
        public int TimeInSeconds
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(StressAppTestExecutor.TimeInSeconds));
            }
        }

        /// <summary>
        /// The UseCpuStressfulMemoryCopy argument defined in the profile, Switch to toggle StressAppTest built-in option to use more CPU-Stressful memory copy
        /// </summary>
        public bool UseCpuStressfulMemoryCopy
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(StressAppTestExecutor.UseCpuStressfulMemoryCopy));
            }
        }

        /// <summary>
        /// The path to the StressAppTest package.
        /// </summary>
        private string PackageDirectory { get; set; }

        /// <summary>
        /// The path to the StressAppTest executable file.
        /// </summary>
        private string ExecutableName { get; set; }

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void ValidateParameters()
        {
            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. The action in the profile does not contain the " +
                    $"required '{nameof(this.Scenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.TimeInSeconds <= 0)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.TimeInSeconds)}' arguments defined. {nameof(this.TimeInSeconds)} should be an integer greater than 0",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.CommandLine.Contains("-l"))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.CommandLine)}' arguments defined. {nameof(this.CommandLine)} should not contain a custom log file, with " +
                    $"-l parameter. That is being appended programatically",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the StressAppTest workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(
                this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackageDirectory = workloadPackage.Path;

            switch (this.Platform)
            {
                case PlatformID.Unix:
                    this.ExecutableName = this.PlatformSpecifics.Combine(this.PackageDirectory, "stressapptest");
                    break;

                default:
                    throw new WorkloadException(
                        $"The StressAppTest workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }

            await this.systemManagement.MakeFileExecutableAsync(this.ExecutableName, this.Platform, cancellationToken)
                .ConfigureAwait(false);

            if (!this.fileSystem.File.Exists(this.ExecutableName))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.ExecutableName}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Executes the StressAppTest workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ValidateParameters();
            DateTime startTime = DateTime.UtcNow;

            string commandLineArguments = this.CommandLine;
            commandLineArguments += " -s " + this.TimeInSeconds;
            if (this.UseCpuStressfulMemoryCopy && !commandLineArguments.Contains("-W"))
            {
                commandLineArguments += " -W";
            }

            string currentTimestamp = startTime.ToString("yyyyMMddHHmmssffff");
            string resultsFileName = "stressapptestLogs_" + currentTimestamp + ".txt";
            commandLineArguments += " -l " + resultsFileName;
            commandLineArguments = commandLineArguments.Trim();

            // Example command with arguments: ./stressapptest -s 60 -l stressapptestLogs_202301131037407031.txt

            await this.ExecuteCommandAsync(
                this.ExecutableName, commandLineArguments, this.PackageDirectory, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;
            this.LogStressAppTestOutput(startTime, endTime, commandLineArguments, resultsFileName, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes the StressAppTest workload command and generates the results file for the logs
        /// </summary>
        private async Task ExecuteCommandAsync(
            string pathToExe,
            string commandLineArguments,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(StressAppTestExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(
                        pathToExe,
                        commandLineArguments,
                        workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<StressAppTestExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Logs the StressAppTest workload metrics.
        /// </summary>
        private void LogStressAppTestOutput(
            DateTime startTime,
            DateTime endTime,
            string commandLineArguments,
            string resultsFileName,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string resultsPath = this.PlatformSpecifics.Combine(this.PackageDirectory, resultsFileName);
                if (!this.fileSystem.File.Exists(resultsPath))
                {
                    throw new WorkloadResultsException(
                        $"The StressAppTest results file was not found at path '{resultsPath}'.",
                        ErrorReason.WorkloadResultsNotFound);
                }

                string rawText = this.fileSystem.File.ReadAllText(resultsPath);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    throw new WorkloadResultsException(
                        "The StressAppTest workload did not produce valid results. The results file is blank",
                        ErrorReason.WorkloadResultsNotFound);
                }

                StressAppTestMetricsParser parser = new StressAppTestMetricsParser(rawText);
                IList<Metric> workloadMetrics = parser.Parse();

                foreach (Metric metric in workloadMetrics)
                {
                    telemetryContext
                        .AddContext("testRunResult", metric.Tags[0] ?? string.Empty);

                    this.Logger.LogMetrics(
                        toolName: "StressAppTest",
                        scenarioName: this.Scenario,
                        startTime,
                        endTime,
                        metric.Name,
                        metric.Value,
                        metric.Unit,
                        metricCategorization: "StressAppTest",
                        scenarioArguments: commandLineArguments,
                        metric.Tags,
                        telemetryContext);
                }
            }
        }
    }
}