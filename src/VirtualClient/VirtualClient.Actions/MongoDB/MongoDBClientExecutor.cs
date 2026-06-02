// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.MongoDB
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// MongoDB Client Executor - Handles YCSB workload execution against MongoDB server.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class MongoDBClientExecutor : MongoDBExecutor
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private IPackageManager packageManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MongoDBClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();

            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries * 2));

            this.ClientRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries));

            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Defines the name of the YCSB package.
        /// </summary>
        public string YCSBPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.YCSBPackageName), "ycsb");
            }
        }

        /// <summary>
        /// Gets the run command specified for the MongoDB YCSB workload.
        /// </summary>
        public string RunCommand
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MongoDBClientExecutor.RunCommand), string.Empty);
            }
        }

        /// <summary>
        /// Gets the load command specified for loading the MongoDB YCSB database.
        /// </summary>
        public string LoadCommand
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MongoDBClientExecutor.LoadCommand), string.Empty);
            }
        }

        /// <summary>
        /// Java Development Kit package name.
        /// </summary>
        public string JdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MongoDBClientExecutor.JdkPackageName), "javadevelopmentkit");
            }
        }

        /// <summary>
        /// Version of workload from YCSB that will be run. Ex. workloada, workload..., workloadf.
        /// </summary>
        public string WorkloadName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MongoDBClientExecutor.WorkloadName));
            }
        }

        /// <summary>
        /// The file path for YCSB workloads.
        /// </summary>
        protected string YcsbPackagePath { get; set; }

        /// <summary>
        /// The path to the YCSB executable script (platform-specific).
        /// </summary>
        protected string YcsbExecutablePath { get; set; }

        /// <summary>
        /// The setenv.sh path for YCSB workloads.
        /// </summary>
        protected string YcsbSetEnvPath { get; set; }

        /// <summary>
        /// Export string for JAVA_HOME.
        /// </summary>
        protected string JavaExportString { get; set; }

        /// <summary>
        /// The folder name for YCSB.
        /// </summary>
        protected string YCSBFolderName { get; set; } = "ycsb-0.17.0";

        /// <summary>
        /// The file path for JDK.
        /// </summary>
        protected string JDKPackagePath { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The retry policy to apply to each workload instance.
        /// </summary>
        protected IAsyncPolicy ClientRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Initializes the environment and dependencies for running YCSB workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            this.InitializeApiClients();

            // Checks to make sure packages were installed correctly and sets the paths for the packages.
            await this.InitializePackageLocationAsync(cancellationToken).ConfigureAwait(false);

            // Sets up YCSB dependencies and environment.
            await this.SetYCSBDependenciesAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            // Wait for server to be online
            await this.WaitForServerOnlineAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the MongoDB YCSB workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                // If loading database, log size after load
                if (this.Scenario?.StartsWith("loaddatabase", StringComparison.OrdinalIgnoreCase) == true)
                {
                    await this.LoadDatabaseAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    await this.LogDatabaseStatsAsync("MongoDB-StatsAfterLoad", telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }

                // Drop database if scenario is 'dropdatabase' and exit
                else if (this.Scenario?.StartsWith("dropdatabase", StringComparison.OrdinalIgnoreCase) == true)
                {
                    bool dbExists = await this.CheckDatabaseExistsAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    if (dbExists)
                    {
                        await this.LogDatabaseStatsAsync("MongoDB-StatsBeforeDrop", telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        await this.DropDatabaseAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        await this.LogDatabaseStatsAsync("MongoDB-StatsAfterDrop", telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        this.Logger.LogMessage(
                            $"{nameof(MongoDBClientExecutor)}.DatabaseDoesNotExistBeforeDrop",
                            LogLevel.Information,
                            telemetryContext.Clone().AddContext("database", "ycsb"));
                    }
                }
                else
                {
                    // makes the YCSB executable file
                    await this.systemManagement.MakeFileExecutableAsync(this.YcsbExecutablePath, this.Platform, cancellationToken)
                        .ConfigureAwait(false);

                    // Executes the run portion of the workload
                    DateTime runStartTime = DateTime.UtcNow;

                    // Replace connection string placeholder with actual server IP
                    string runCommand = this.RunCommand.Replace("{ServerIP}", this.ServerIpAddress);
                    runCommand = runCommand.Replace("{Port}", this.Port.ToString());

                    var runOutput = await this.ExecuteCommandAsync(
                        $"{this.YcsbExecutablePath}",
                        runCommand,
                        this.YcsbPackagePath,
                        telemetryContext,
                        cancellationToken).ConfigureAwait(false);
                
                    DateTime runFinishTime = DateTime.UtcNow;

                    // Formats and sends out metrics from runOutput (workload output) and other telemetry parameters
                    this.CaptureMetrics(runOutput, runCommand, runStartTime, runFinishTime, telemetryContext, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                telemetryContext.AddError(ex);
                this.Logger.LogTraceMessage($"{nameof(MongoDBClientExecutor)}.Exception", telemetryContext);
            }
        }

        /// <summary>
        /// Waits for the server to be online.
        /// </summary>
        private async Task WaitForServerOnlineAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogMessage(
                    $"{nameof(MongoDBClientExecutor)}.WaitingForServer",
                    LogLevel.Information,
                    telemetryContext.Clone().AddContext("serverIpAddress", this.ServerIpAddress));

                await this.ServerApiClient.PollForServerOnlineAsync(this.PollingTimeout, cancellationToken)
                    .ConfigureAwait(false);

                this.Logger.LogMessage(
                    $"{nameof(MongoDBClientExecutor)}.ServerOnline",
                    LogLevel.Information,
                    telemetryContext.Clone().AddContext("serverIpAddress", this.ServerIpAddress));
            }
        }

        /// <summary>
        /// Sets up YCSB dependencies and environment.
        /// </summary>
        private async Task SetYCSBDependenciesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // The path ending in ycsb.sh is specific to the linux platform
                    this.YcsbExecutablePath = this.PlatformSpecifics.Combine(this.YcsbPackagePath, this.YCSBFolderName, "bin", "ycsb.sh");

                    // Make the file executable
                    await this.systemManagement.MakeFileExecutableAsync(this.YcsbExecutablePath, this.Platform, cancellationToken)
                        .ConfigureAwait(false);

                    this.JavaExportString = $"export JAVA_HOME={this.JDKPackagePath}";

                    // Create script setenv.sh in YCSB_HOME/bin to set the variables
                    this.YcsbSetEnvPath = this.PlatformSpecifics.Combine(this.YcsbPackagePath, this.YCSBFolderName, "bin", "setenv.sh");
                    await this.ExecuteCommandAsync(
                        command: "bash",
                        commandArguments: $"-c \"echo {this.JavaExportString} > {this.YcsbSetEnvPath}\"",
                        workingDirectory: this.YcsbPackagePath,
                        telemetryContext: telemetryContext,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.SetYCSBDependenciesFailed", LogLevel.Error, relatedContext);
                    throw;
                }
            }
        }

        /// <summary>
        /// Loads data into the MongoDB database using YCSB.
        /// </summary>
        private async Task LoadDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Sets JAVA_HOME environment variable and makes the YCSB executable file
                    this.SetEnvironmentVariable(EnvironmentVariable.JAVA_HOME, this.JDKPackagePath, EnvironmentVariableTarget.Process);
                    await this.systemManagement.MakeFileExecutableAsync(this.YcsbExecutablePath, this.Platform, cancellationToken)
                        .ConfigureAwait(false);

                    // Replace connection string placeholder with actual server IP
                    string loadCommand = this.LoadCommand.Replace("{ServerIP}", this.ServerIpAddress);
                    loadCommand = loadCommand.Replace("{Port}", this.Port.ToString());

                    EventContext relatedContext = EventContext.Persisted()
                        .AddContext("scenario", this.Scenario)
                        .AddContext("database", "ycsb")
                        .AddContext("loadCommand", loadCommand)
                        .AddContext("serverIpAddress", this.ServerIpAddress);

                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.LoadingDatabase", LogLevel.Information, relatedContext);

                    // Execute the load command
                    DateTime loadStartTime = DateTime.UtcNow;

                    string loadOutput = await this.ExecuteCommandAsync(
                        this.YcsbExecutablePath,
                        loadCommand,
                        this.YcsbPackagePath,
                        telemetryContext,
                        cancellationToken).ConfigureAwait(false);

                    DateTime loadFinishTime = DateTime.UtcNow;

                    // Capture metrics for load-database scenario the same way as run workloads.
                    this.CaptureMetrics(loadOutput, loadCommand, loadStartTime, loadFinishTime, telemetryContext, cancellationToken);

                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.DatabaseLoadedSuccessfully", LogLevel.Information, relatedContext);
                }
                catch (Exception ex)
                {
                    EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.LoadDatabaseFailed", LogLevel.Error, relatedContext);
                    throw;
                }
            }
        }

        /// <summary>
        /// Drops the MongoDB database if specified in the scenario.
        /// </summary>
        private async Task DropDatabaseAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string dropDatabaseCommand = $"mongosh mongodb://{this.ServerIpAddress}:{this.Port}/ycsb --eval 'db.dropDatabase()'";
                    
                    EventContext relatedContext = EventContext.Persisted()
                        .AddContext("command", dropDatabaseCommand)
                        .AddContext("scenario", this.Scenario)
                        .AddContext("database", "ycsb")
                        .AddContext("serverIpAddress", this.ServerIpAddress);

                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.DroppingDatabase", LogLevel.Information, relatedContext);
                    
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                        this.Platform, "mongosh", $"mongodb://{this.ServerIpAddress}:{this.Port}/ycsb --eval 'db.dropDatabase()'"))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        this.LogProcessTrace(process);
                        
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                        
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "MongoDBClient-DropDatabase", logToFile: true)
                                .ConfigureAwait(false);
                            
                            if (process.ExitCode != 0)
                            {
                                this.Logger.LogMessage(
                                    $"{nameof(MongoDBClientExecutor)}.DropDatabaseWarning",
                                    LogLevel.Warning,
                                    relatedContext.Clone().AddContext("exitCode", process.ExitCode));
                            }
                            else
                            {
                                this.Logger.LogMessage(
                                    $"{nameof(MongoDBClientExecutor)}.DatabaseDroppedSuccessfully",
                                    LogLevel.Information,
                                    relatedContext);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                    this.Logger.LogMessage($"{nameof(MongoDBClientExecutor)}.DropDatabaseFailed", LogLevel.Warning, relatedContext);
                }
            }
        }

        /// <summary>
        /// Checks if the specified database exists.
        /// </summary>
        private async Task<bool> CheckDatabaseExistsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                this.Platform, "mongosh", $"mongodb://{this.ServerIpAddress}:{this.Port} --eval \"show dbs\""))
            {
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                string output = process.StandardOutput.ToString();
                bool exists = output.Contains("ycsb", StringComparison.OrdinalIgnoreCase);
                
                this.Logger.LogMessage(
                    $"{nameof(MongoDBClientExecutor)}.CheckDatabaseExists",
                    LogLevel.Information,
                    telemetryContext.Clone()
                        .AddContext("database", "ycsb")
                        .AddContext("exists", exists)
                        .AddContext("serverIpAddress", this.ServerIpAddress));
                
                return exists;
            }
        }

        /// <summary>
        /// Logs the database statistics.
        /// </summary>
        private async Task LogDatabaseStatsAsync(string logTag, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                this.Platform, "mongosh", $"mongodb://{this.ServerIpAddress}:{this.Port}/ycsb --eval 'db.stats()'"))
            {
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                await this.LogProcessDetailsAsync(process, telemetryContext, logTag, logToFile: true)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Wrapper for the MongoDB workload executor.
        /// </summary>
        private async Task<string> ExecuteCommandAsync(
            string command,
            string commandArguments,
            string workingDirectory,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            EventContext relatedContext = EventContext.Persisted()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments);

            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                this.Platform, command, commandArguments, workingDirectory))
            {
                this.CleanupTasks.Add(() => process.SafeKill());
                this.LogProcessTrace(process);

                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "MongoDBClient", logToFile: true)
                        .ConfigureAwait(false);
                    process.ThrowIfWorkloadFailed();
                }

                return process.StandardOutput.ToString();
            }
        }

        /// <summary>
        /// Captures Metrics
        /// </summary>
        private void CaptureMetrics(
            string results,
            string commandArguments,
            DateTime startTime,
            DateTime endtime,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "MongoDBClient",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    results.ThrowIfNullOrWhiteSpace(nameof(results));
                    this.Logger.LogMessage(
                        $"{nameof(MongoDBClientExecutor)}.CaptureMetrics",
                        telemetryContext.Clone().AddContext("results", results));
                    
                    MongoDBMetricsParser resultsParser = new MongoDBMetricsParser(this.Scenario, results);
                    IList<Metric> metrics = resultsParser.Parse();

                    string metricScenarioName = this.NormalizeMetricScenarioName(this.MetricScenario ?? this.Scenario);
                    
                    this.Logger.LogMetrics(
                        toolName: "MongoDBClient",
                        scenarioName: metricScenarioName,
                        scenarioStartTime: startTime,
                        scenarioEndTime: endtime,
                        metrics: metrics,
                        metricCategorization: null,
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone().AddError(exc);
                    this.Logger.LogMessage(
                        $"{nameof(MongoDBClientExecutor)}.WorkloadOutputParsingFailed",
                        LogLevel.Warning,
                        relatedContext);
                }
            }
        }

        private string NormalizeMetricScenarioName(string scenarioName)
        {
            if (string.IsNullOrWhiteSpace(scenarioName))
            {
                return scenarioName;
            }

            return Regex.Replace(
                scenarioName,
                @"^workload([a-z])(?=_|$)",
                match => $"workload_{char.ToUpperInvariant(match.Groups[1].Value[0])}",
                RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Checks to make sure packages were installed correctly and sets the paths for the packages.
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath javaPackage = await this.GetPackageAsync(this.JdkPackageName, cancellationToken)
                    .ConfigureAwait(false);
                
                if (javaPackage == null)
                {
                    throw new DependencyException(
                       $"The expected package '{this.JdkPackageName}' does not exist on the system or is not registered.",
                       ErrorReason.WorkloadDependencyMissing);
                }

                this.JDKPackagePath = javaPackage.Path;

                DependencyPath ycsbPackage = await this.packageManager.GetPackageAsync(this.YCSBPackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                if (ycsbPackage == null)
                {
                    throw new DependencyException(
                        $"The expected package '{this.YCSBPackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                }

                this.YcsbPackagePath = ycsbPackage.Path;
            }
        }
    }
}
