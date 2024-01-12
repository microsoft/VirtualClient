// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Apache http server benchmarking workload executor
    /// </summary>
    public class ApacheBenchExecutor : VirtualClientComponent
    {
        private IPackageManager packageManager;
        private IFileSystem fileSystem;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// The ApacheBench workload executor
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ApacheBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.processManager = this.systemManagement.ProcessManager;
            this.SystemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Parameter defines the command line arguments to pass to the workload executable.
        /// </summary>
        public string CommandLine
        {
            get
            {
                string commandLine = this.Parameters.GetValue<string>(nameof(this.CommandLine));
                if (string.IsNullOrWhiteSpace(commandLine))
                {
                    commandLine = "-k -n 50000 -c 10 http://localhost:80/";
                }

                return commandLine;
            }
        }

        /// <summary>
        /// Provides methods for managing system requirements.
        /// </summary>
        protected ISystemManagement SystemManagement { get; }

        /// <summary>
        /// It is common to use local member variables or properties to keep track of the names of 
        /// workload binaries/executables. Depending upon the OS platform (Linux vs. Windows) we are on
        /// the names of the binaries might be different.
        /// </summary>
        protected string WorkloadExecutablePath { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                Task.Delay(3000);
                DateTime startTime = DateTime.UtcNow;
                string workloadResults = string.Empty;
                using (IProcessProxy process = await this.ExecuteCommandAsync(this.WorkloadExecutablePath, this.CommandLine, string.Empty, telemetryContext, cancellationToken, runElevated: true))
                {
                    workloadResults = process.StandardOutput.ToString();
                }

                DateTime finishTime = DateTime.UtcNow;

                await this.CaptureMetricsAsync(workloadResults, this.CommandLine, startTime, finishTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                telemetryContext.AddError(ex);
                this.Logger.LogTraceMessage($"{nameof(ExampleWorkloadExecutor)}.Exception", telemetryContext);
            }
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Task.Delay(3000);
            ApacheBenchState state = await this.stateManager.GetStateAsync<ApacheBenchState>($"{nameof(ApacheBenchState)}", cancellationToken)
                ?? new ApacheBenchState();

            if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath workloadPackagePath = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None)
                    .ConfigureAwait(false);
                string apache24Directory = this.PlatformSpecifics.Combine(workloadPackagePath.Path + "\\Apache24");
                string httpdConfFilePath = this.PlatformSpecifics.Combine(apache24Directory + "\\conf", "\\httpd.conf");

                // Replacing the default path to the directory path.
                await this.fileSystem.File.ReplaceInFileAsync(
                    httpdConfFilePath, "Define SRVROOT \"c:\\/Apache24\"", $"Define SRVROOT \"{apache24Directory}\"", cancellationToken);

                string httpdExecutablePath = this.PlatformSpecifics.Combine(apache24Directory + "\\bin\\httpd.exe");
                if (!state.ApacheBenchStateInitialized)
                {
                    await this.ExecuteCommandAsync(httpdExecutablePath, "-k install", $"{apache24Directory}\\bin", telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
                }

                // this.WorkloadExecutablePath = $"{apache24Directory}\\bin\\ab.exe";
                this.WorkloadExecutablePath = this.PlatformSpecifics.Combine(apache24Directory + "\\bin\\ab.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                var executionCommands = new List<string>
                {
                    "ufw allow 'Apache'",
                    "systemctl start apache2"
                };

                foreach (var command in executionCommands)
                {
                    await this.ExecuteCommandAsync(command, "/usr/sbin/", telemetryContext, cancellationToken, runElevated: true)
                        .ConfigureAwait(false);
                }

                this.WorkloadExecutablePath = "/usr/bin/ab";
            }

            state.ApacheBenchStateInitialized = true;
            await this.stateManager.SaveStateAsync<ApacheBenchState>($"{nameof(ApacheBenchState)}", state, cancellationToken);
        }

        private Task<string> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext("command", this.WorkloadExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{nameof(ApacheBenchExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy workloadProcess = this.processManager.CreateProcess(this.WorkloadExecutablePath, commandArguments))
                    {
                        this.CleanupTasks.Add(() => workloadProcess.SafeKill());
                        await workloadProcess.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(workloadProcess, telemetryContext, nameof(ApacheBenchExecutor), logToFile: true)
                                .ConfigureAwait(false);

                            workloadProcess.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                            if (workloadProcess.StandardOutput.Length == 0)
                            {
                                throw new WorkloadException(
                                    $"Unexpected workload results outcome. The workload did not produce any results to standard output.",
                                    ErrorReason.WorkloadResultsNotFound);
                            }
                        }

                        return workloadProcess.StandardOutput.ToString();
                    }
                }
            });
        }

        private static Task OpenFirewallPortsAsync(int port, IFirewallManager firewallManager, CancellationToken cancellationToken)
        {
            return firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "Apache http server: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { port })
                },
                cancellationToken);
        }

        private Task CaptureMetricsAsync(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage($"{nameof(ApacheBenchExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                ApacheBenchMetricsParser resultsParser = new ApacheBenchMetricsParser(results);
                IList<Metric> workloadMetrics = resultsParser.Parse();

                this.Logger.LogMetrics(
                    toolName: nameof(ApacheBenchExecutor),
                    scenarioName: this.Scenario, 
                    scenarioStartTime: startTime,
                    scenarioEndTime: endTime,
                    metrics: workloadMetrics,
                    metricCategorization: null,
                    scenarioArguments: commandArguments,
                    this.Tags,
                    telemetryContext);
            }

            return Task.CompletedTask;
        }

        internal class ApacheBenchState : State
        {
            public ApacheBenchState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool ApacheBenchStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(ApacheBenchState.ApacheBenchStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(ApacheBenchState.ApacheBenchStateInitialized)] = value;
                }
            }
        }
    }
}
