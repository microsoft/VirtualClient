// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a client-side role responsibilities 
    /// in the client/server workload.
    /// </summary>
    /// <remarks>
    /// This is on implementation pattern for client/server workloads. In this model, the client and server
    /// roles inherit from the <see cref="ExampleClientServerExecutor"/> and override behavior as
    /// is required by the particular role. For this particular example, the client role is responsible for
    /// confirming the server-side is online followed by running a workload against it.
    /// </remarks>
    internal class ExampleClientExecutor : ExampleClientServerExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(30);
            this.StateConfirmationTimeout = TimeSpan.FromMinutes(10);
        }

        /// <summary>
        /// 'ServerPort' parameter defined in the profile action.
        /// </summary>
        public int ServerPort
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerPort));
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// The API client for communications with the server.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan StateConfirmationTimeout { get; set; }

        /// <summary>
        /// It is common to use local member variables or properties to keep track of the names of 
        /// workload binaries/executables. Dependending upon the OS platform (Linux vs. Windows) we are on
        /// the names of the binaries might be different.
        /// </summary>
        protected string WorkloadExecutablePath { get; set; }

        /// <summary>
        /// It is common to use local member variables or properties to keep track of dependency packages
        /// and paths to common binaries/scripts/executables. This one represents the location of the
        /// package that was installed on the system containing the workload executable itself.
        /// </summary>
        protected DependencyPath WorkloadPackage { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // The ExecuteAsync method is where the core workload/test logic is executed. By the time we are here, we should
            // be confident that all required dependencies have been verified in the InitializeAsync() method below and
            // that we are ready to rock!!

            IEnumerable<ClientInstance> targetServers = this.GetTargetServers();
            List<Task> clientWorkloadTasks = new List<Task>();

            foreach (ClientInstance server in targetServers)
            {
                // Reliability/Recovery:
                // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                // over again.
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);

                        // 1) Confirm server is online.
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                        await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                            .ConfigureAwait(false);

                        // 2) Confirm the server-side application (e.g. web server) is online.
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                        await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromSeconds(30), cancellationToken)
                            .ConfigureAwait(false);

                        this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                        this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                        // 3) Execute the client workload.
                        // ===========================================================================
                        IPAddress ipAddress = IPAddress.Parse(server.IPAddress);
                        await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // Component initialization logic is typically used to confirm any initial requirements before the ExecuteAsync
            // method is called. This includes for example that expected packages are installed on the system. This logic is 
            // also used to set local member variables and properties that can be used/referenced later by other methods that are
            // executed within the ExecuteAsync workflow.
            //
            // By the time this method executes, we should have confirmed ALL required dependencies exist and we are ready to 
            // execute the workload/test.

            // We will be referencing the workload package/paths later on. The binaries/executables we will run on in this package.
            this.WorkloadPackage = await this.PackageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            if (this.Platform == PlatformID.Win32NT)
            {
                // On Windows the binary has a .exe in the name.
                this.WorkloadExecutablePath = this.Combine(this.WorkloadPackage.Path, "ExampleWorkload.exe");
            }
            else
            {
                // PlatformID.Unix
                // On Unix/Linux the binary does NOT have a .exe in the name. 
                this.WorkloadExecutablePath = this.Combine(this.WorkloadPackage.Path, "ExampleWorkload");

                // Binaries on Unix/Linux must be attributed with an attribute that makes them "executable".
                await this.SystemManagement.MakeFileExecutableAsync(this.WorkloadExecutablePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }

            // We also typically check to make sure any expected binaries, scripts, executables etc... that are required are found
            // in the workload package that was installed.
            if (!this.FileSystem.File.Exists(this.WorkloadExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.WorkloadExecutablePath}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Allows the parameters passed into the component to be validated. This allows the
        /// developer to ensure the definitions in the workload profile are valid and have defined
        /// required information.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            this.ThrowIfParameterNotDefined(nameof(this.ServerPort));
        }

        private Task CaptureMetricsAsync(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // We often separate out the logic of running the workload that produces results from the logic
            // that is responsible for parsing the metrics from the results and capturing telemetry around them.

            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage($"{nameof(ExampleWorkloadExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                // Due to the complexity at times of parsing results (which can be in a wide range of different formats depending upon the workload),
                // parsing logic is often implemented in a "parser" class. Whereas this is not required, it often helps to keep the logic
                // simpler in the workload executor and additionally makes testing the 2 different classes easier.
                ExampleWorkloadMetricsParser resultsParser = new ExampleWorkloadMetricsParser(results);
                IList<Metric> workloadMetrics = resultsParser.Parse();

                // We have extension methods in the Virtual Client codebase that make it easier to log certain types
                // of data in a consistent way. The 'LogTestMetrics' method is an extension method to ILogger instances
                // that ensures metrics are routed to the appropriate logger handlers and onward to specific log files
                // or cloud endpoints like Event Hub.

                this.Logger.LogMetrics(
                    toolName: "ExampleWorkload",         // The name of the tool used to produce the metrics (e.g. FIO, DiskSpd, Prime95).
                    scenarioName: this.Scenario,         // The scenario represents 1 distinct way the tool was run. A given workload tool may be ran multiple different ways in a single profile.
                    scenarioStartTime: startTime,        // The time at which the workload/tool execution started.
                    scenarioEndTime: endTime,            // The time at which the workload/tool execution finished.
                    metrics: workloadMetrics,            // One or more metrics parsed from the results/output of the workload/tool.
                    metricCategorization: null,          // Used occasionally to categorize a set of metrics where there can be sub-categorizations (e.g. remote/managed disks vs. local/physical disks).
                    scenarioArguments: commandArguments, // The command line arguments used when running the workload (if applicable).
                    this.Tags,                           // The 'Tags' as defined for the component in the workload profile.
                    telemetryContext);                   // Allows any amount of additional context information to be included with the metric telemetry events emitted.
            }

            return Task.CompletedTask;
        }

        private Task ExecuteWorkloadAsync(IPAddress serverIpAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // When writing new events in a given method, tt is common practice to clone a telemetry context object and add
            // any additional context to that new object that could be useful for triage/debugging. In this example, we
            // want to make sure that we are capturing the command line executable path and arguments that we are about to
            // run. This is very helpful for repro when issues happen.
            string commandArguments = $"Workload --workload=ClientServer --duration=00:01:00 --port={this.ServerPort} --ipAddress={serverIpAddress}";

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext("packagePath", this.WorkloadPackage.Path)
                .AddContext("command", this.WorkloadExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{nameof(ExampleWorkloadExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                DateTime startTime = DateTime.UtcNow; // date times ALWAYS in UTC

                // We create a operating system process to host the executing workload, start it and wait for it to exit.
                using (IProcessProxy workloadProcess = this.ProcessManager.CreateProcess(this.WorkloadExecutablePath, commandArguments, this.WorkloadPackage.Path))
                {
                    this.CleanupTasks.Add(() => workloadProcess.SafeKill());

                    await workloadProcess.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    DateTime endTime = DateTime.UtcNow;

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // ALWAYS log the details for the process. This helper method will ensure we capture the exit code, standard output, standard
                        // error etc... This is very helpful for triage/debugging.
                        await this.LogProcessDetailsAsync(workloadProcess, telemetryContext, "ExampleWorkload")
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

                    await this.CaptureMetricsAsync(workloadProcess.StandardOutput.ToString(), commandArguments, startTime, endTime, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            });
        }

        private IEnumerable<ClientInstance> GetTargetServers()
        {
            // Reverse proxy scenario or traditional client/server scenario?
            IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.ReverseProxy, throwIfNotExists: false);
            if (targetServers?.Any() != true)
            {
                targetServers = this.GetLayoutClientInstances(ClientRole.Server);
            }

            return targetServers;
        }
    }
}