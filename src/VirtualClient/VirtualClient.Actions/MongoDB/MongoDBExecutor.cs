// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.IO.Packaging;
    using System.Reflection.Metadata.Ecma335;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Prime95 workload executor.
    /// </summary>
    public class MongoDBExecutor : VirtualClientComponent
    {
        private IPackageManager packageManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="MongoDBExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MongoDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The retry policy to apply to package install for handling transient errors.
        /// </summary>
        public IAsyncPolicy InstallRetryPolicy { get; set; } = Policy
            .Handle<WorkloadException>(exc => exc.Reason == ErrorReason.DependencyInstallationFailed)
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2));

        /// <summary>
        /// Defines the name of the YCSB package.
        /// </summary>
        public string YCSBPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.YCSBPackageName));
            }
        }

        /// <summary>
        /// The file path where logs will be written.
        /// </summary>
        protected string YcsbPackagePath { get; set; }

        /// <summary>
        /// Workload package path.
        /// </summary>
        protected string MongoDBPackagePath { get; set; }

        /// <summary>
        /// Initializes the environment for execution of the MongoDB YCSB workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath mongoDBPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            if (mongoDBPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.MongoDBPackagePath = mongoDBPackage.Path;

            DependencyPath ycsbPackage = await this.packageManager.GetPackageAsync(this.YCSBPackageName, CancellationToken.None);

            if (mongoDBPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.YcsbPackagePath = ycsbPackage.Path;

            // string pathtobin = this.PlatformSpecifics.Combine(this.PackageDirectory, "bin");

        }

        /// <summary>
        /// Executes the MongoDB YCSB workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string mountPath = this.PlatformSpecifics.Combine(this.MongoDBPackagePath, "mongodb-linux-x86_64-ubuntu2004-5.0.15", "bin", "mongod");

            try
            {
                string ycsbPath = this.PlatformSpecifics.Combine(this.YcsbPackagePath, "ycsb-0.5.0", "bin", "ycsb");
                string loadArguments = this.GetCommandLineArguments("load");
                string runArguments = this.GetCommandLineArguments("run");

                Console.WriteLine($"mountPath - {mountPath}");
                Console.WriteLine($"ycsbPath - {ycsbPath}");
                Console.WriteLine($"created  db location ");

                await this.ExecuteCommandAsync("mkdir", " -p /tmp/mongodb", this.MongoDBPackagePath, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                Console.WriteLine($"executing mongo process");
                await this.ExecuteCommandAsync($"{mountPath}", " --fork --dbpath /tmp/mongodb --logpath /tmp/mongod.log", this.MongoDBPackagePath, telemetryContext, cancellationToken)
                           .ConfigureAwait(false);

                Console.WriteLine($"executed mongo process");
                /* ./ycsb load mongodb -s -P /home/azureuser/vc/content/linux-x64/packages/ycsb/ycsb-0.5.0/workloads/workloada -p recordcount=1000000 -threads 16
                */
                DateTime startTime = DateTime.UtcNow;
                var loadOutput = await this.ExecuteCommandAsync($"{ycsbPath}", loadArguments, this.YcsbPackagePath, telemetryContext, cancellationToken)
                         .ConfigureAwait(false);
                DateTime finishTime = DateTime.UtcNow;
                Console.WriteLine($"loadOutput - {loadOutput}");

                await this.CaptureMetricsAsync(loadOutput, loadArguments, startTime, finishTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                /*./ycsb run mongodb -s -P /home/azureuser/vc/content/linux-x64/packages/ycsb/ycsb-0.5.0/workloads/workloada -p operationcount=1000000 -p recordcount=1000000 -threads 16
                */
                startTime = DateTime.UtcNow;
                var runOutput = await this.ExecuteCommandAsync($"{ycsbPath}", runArguments, this.YcsbPackagePath, telemetryContext, cancellationToken)
                         .ConfigureAwait(false);
                finishTime = DateTime.UtcNow;

                Console.WriteLine($"runOutput - {runOutput}");

                await this.CaptureMetricsAsync(runOutput, runArguments, startTime, finishTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                await this.ShutDownMongoDB(mountPath, telemetryContext, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                await this.ShutDownMongoDB(mountPath, telemetryContext, cancellationToken);
                telemetryContext.AddError(ex);
                this.Logger.LogTraceMessage($"{nameof(ExampleWorkloadExecutor)}.Exception", telemetryContext);
            }
        }

        private async Task<string> ExecuteCommandAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = EventContext.Persisted()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments);

            using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, commandArguments, workingDirectory))
            {
                this.CleanupTasks.Add(() => process.SafeKill());
                this.LogProcessTrace(process);

                await process.StartAndWaitAsync(cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    if (process.IsErrored())
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfWorkloadFailed();
                    }
                }

                return process.StandardOutput.ToString();
            }
        }

        private string GetCommandLineArguments(string commandType)
        {
            string workloadPath = this.PlatformSpecifics.Combine(this.YcsbPackagePath, "ycsb-0.5.0", "workloads", "workloada");
            if (commandType == "load")
            {
                return $"load mongodb -s -P {workloadPath} -p recordcount=1000000 -threads 16";
            }
            else
            {
                return $"run mongodb -s -P {workloadPath} -p operationcount=1000000 -p recordcount=1000000 -threads 16";
            }
        }

        private Task CaptureMetricsAsync(string results, string commandArguments, DateTime startTime, DateTime endtime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));
                this.Logger.LogMessage($"{nameof(MongoDBExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                MongoDBMetricsParser resultsParser = new MongoDBMetricsParser(results);
                IList<Metric> workloadMetrics = resultsParser.Parse();

                this.Logger.LogMetrics(
                    toolName: nameof(MongoDBExecutor),
                    scenarioName: this.Scenario,
                    scenarioStartTime: startTime,
                    scenarioEndTime: endtime,
                    metrics: workloadMetrics,
                    metricCategorization: null,
                    scenarioArguments: commandArguments,
                    this.Tags,
                    telemetryContext);
            }

            return Task.CompletedTask;
        }

        private async Task ShutDownMongoDB(string mountPath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            /*  sudo /home/azureuser/vc/content/linux-x64/packages/mongodb/mongodb-linux-x86_64-ubuntu2004-5.0.15/bin/mongod--dbpath /tmp/mongodb--shutdown
                */
            await this.ExecuteCommandAsync($"{mountPath}", "--dbpath /tmp/mongodb --shutdown", this.MongoDBPackagePath, telemetryContext, cancellationToken).ConfigureAwait(false);

            await this.ExecuteCommandAsync("rm", "-rf /tmp/mongo*", this.MongoDBPackagePath, telemetryContext, cancellationToken).ConfigureAwait(false);

            /* Adding delay of 1minute for graceful shutdown of mongodb */
            await Task.Delay(60000).ConfigureAwait(false);

        }
    }
}