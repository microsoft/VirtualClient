// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    
    /// <summary>
    /// Executor.
    /// </summary>
    public class CassandraExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;
        // private string cassandraDirectory = @".";

        /// <summary>
        /// Executor.
        /// </summary>
        public CassandraExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// Cassandra space separated input files or directories
        /// </summary>
        public string InputFilesOrDirs
        {
            get
            {
                this.Parameters.TryGetValue(nameof(CassandraExecutor.InputFilesOrDirs), out IConvertible inputFilesOrDirs);
                return inputFilesOrDirs?.ToString();
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                // Command and parameters specified in the workload configuration
                // Execute the command
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // this.Logger.LogInformation($"inside using profiling");
                    string command = this.Parameters.GetValue<string>("command");
                    string argument = this.Parameters.GetValue<string>("parameters");
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, argument, ".", telemetryContext, cancellationToken)
                        .ConfigureAwait(false))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Cassandra", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(process, telemetryContext, argument);
                        }

                        if (process.ExitCode != 0)
                        {
                            throw new WorkloadException($"Command failed with exit code {process.ExitCode}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error and rethrow
                this.Logger.LogMessage($"Failed to parse cassandra output: {ex.Message}", LogLevel.Warning, telemetryContext);
                throw new WorkloadException($"Failed to parse cassandra output: {ex.Message}", ex);
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(CassandraExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext)
                                .ConfigureAwait(false);

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, string commandArguments)
        {
            process.ThrowIfNull(nameof(process));

            this.MetadataContract.AddForScenario(
                "cassandra",
                commandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            CassandraMetricsParser parser = new CassandraMetricsParser(process.StandardOutput.ToString());
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "cassandra",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                null,
                commandArguments,
                this.Tags,
                telemetryContext);
        }
    }
}
