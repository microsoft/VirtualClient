using System.Threading;
using System.Threading.Tasks;
using VirtualClient.Common.Telemetry;

namespace VirtualClient.Actions.Wrathmark
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using Polly.Caching;

    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Class for the Wrathmark workload.
    /// </summary>
    public class WrathmarkWorkloadExecutor : VirtualClientComponent
    {
        private readonly ISystemManagement systemManagement;
        private readonly ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="WrathmarkWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public WrathmarkWorkloadExecutor(
            IServiceCollection dependencies,
            IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            // Follows the example, but is wrong. The services should be injected to this class, not resolved here
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.processManager = this.systemManagement.ProcessManager;
        }

        /// <summary>
        /// Name of the tool.
        /// </summary>
        protected string ToolName => "Wrathmark";
        
        /// <summary>
        /// Name of the workload executable.
        /// </summary>
        protected DependencyPath WorkloadPackage { get; }

        /// <summary>
        /// Path to the package containing the workload executable.
        /// </summary>
        protected string WorkloadExecutablePath { get; }

        /// <summary>
        /// Executes the Wrathmark component logic.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage(
                $"{nameof(WrathmarkWorkloadExecutor)}.Starting",
                telemetryContext);

            var startTime = DateTime.UtcNow;

            var results = await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);

            var endTime = DateTime.UtcNow;

            await this.CaptureMetricsAsync(results, startTime, endTime, telemetryContext, cancellationToken);
        }

        private Task<string> ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext("packagePath", this.WorkloadPackage.Path)
                .AddContext("command", this.WorkloadExecutablePath)
                ;

            return this.Logger.LogMessageAsync($"{nameof(WrathmarkWorkloadExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                // This example shows how to integrate with monitors that run "on-demand" to do background profiling
                // work while the workload is running. To integrate with any one or more of these monitors (defined in monitor profiles),
                // simply wrap the logic for running the workload in a 'BackgroundProfiling' block.
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // We create a operating system process to host the executing workload, start it and
                    // wait for it to exit.
                    using (IProcessProxy workloadProcess = this.processManager.CreateProcess(this.WorkloadExecutablePath, null, this.WorkloadPackage.Path))
                    {
                        this.CleanupTasks.Add(() => workloadProcess.SafeKill());

                        await workloadProcess.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // ALWAYS log the details for the process. This helper method will ensure we capture the exit code, standard output, standard
                            // error etc... This is very helpful for triage/debugging.
                            await this.LogProcessDetailsAsync(workloadProcess, telemetryContext, this.ToolName, logToFile: true)
                                .ConfigureAwait(false);

                            // If the workload process returned a non-success exit code, we throw an exception typically. The ErrorReason used here
                            // will NOT cause VC to crash.
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

        private Task CaptureMetricsAsync(
            string results, 
            DateTime start, 
            DateTime end, 
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage(
                    $"{nameof(WrathmarkWorkloadExecutor)}.CaptureMetrics", 
                    telemetryContext.Clone().AddContext("results", results));

                // TODO: Seems weird to new this up here. Shouldn't this be injected?
                var resultsParser = new WrathmarkMetricsParser(results);
                var metrics = resultsParser.Parse();

                this.Logger.LogMetrics(
                    toolName: this.ToolName,
                    scenarioName: this.Scenario,
                    scenarioStartTime: start,
                    scenarioEndTime: end,
                    metrics: metrics,
                    metricCategorization: null, 
                    scenarioArguments: null, 
                    this.Tags,               
                    telemetryContext);       
            }

            return Task.CompletedTask;
        }
    }
}
