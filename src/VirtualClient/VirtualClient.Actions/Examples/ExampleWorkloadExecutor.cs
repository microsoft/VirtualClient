// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a workload or a test on
    /// the system.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExampleWorkloadExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.processManager = this.systemManagement.ProcessManager;

            // Dependencies:
            // ==============================================================================================================
            // The core 'dependencies' for the Virtual Client are passed into each and every component whether it is an
            // action/executor, monitor or dependency installer/handler. These core dependencies can be used to interoperate 
            // with the Virtual Client core runtime platform as well as the system on which it is running. The core dependencies
            // include the following. The interfaces and implementations can be found in the VirtualClient.Core project:
            // - IApiClientManager
            //   Virtual Client API client creation and management.
            //
            // - IBlobManager
            //   Used to interface with blob stores (e.g. Azure Blob store).
            //
            // - IDiskManager
            //   Used to access information about disks on the system, to create mount points and to initialize/format disks.
            //
            // - IFileSystem (.NET out-of-box abstraction)
            //   Used to access folders and files on the file system.
            //
            // - IFirewallManager
            //   Used to add rules and ports to the local firewall on the system.
            //
            // - ILogger (.NET out-of-box abstraction)
            //   Used to log ALL telemetry emitted by the Virtual Client.
            //
            // - IPackageManager
            //   Used to access dependency packages on the system and to download/extract packages from remote stores (e.g. storage account blobs).
            //
            // - ProcessManager
            //   Used to create and manage operating system processes used to run executables for workloads, tests etc...
            //
            // - IStateManager
            //   Used to create and manage state objects on the local system.
            //
            // - ISystemInfo
            //   Used to supply information about the system on which Virtual Client is running (e.g. OS platform, CPU architecture etc...).
            //
            // - ISystemManagement
            //   Core interface provides all core dependencies for the Virtual Client platform. This interface contains all of the dependencies
            //   noted above and is used primarily as an extensibility point for each of them.

            // Parameters:
            // ==============================================================================================================
            // The 'parameters' passed into the component are those that are defined in the workload or monitoring profile for
            // that particular component. Parameters allow a given action/executor, monitor or dependency installer/handler
            // component to be flexible for supporting different scenarios. Certain parameters (defined at the top of the
            // profile) can be overridden on the command line by the user/automation.

            // General Flow:
            // ==============================================================================================================
            // All components in the Virtual Client codebase follow a consistent logical workflow. This is because all
            // Virtual Client components derive from the base class 'VirtualClientComponent'. The component methods are executed
            // in the following order:
            //
            // 1) IsSupported
            //    Whether or not the component should be executed on the system.
            //    - Does this component support the current platform/architecture?
            //    - Does this component support the current distro of the operating system.
            //    - Should this component be executed? 
            //
            // 2) Validate
            //    Was the component given the information it needs? Allows the developer to perform validations/checks on the 
            //    parameters provided to ensure they are correct and that expected parameters exist.
            // 
            // 3) InitializeAsync
            //    Does the component have the dependencies it needs to succeed? Allows the developer to perform preliminary/initialization work:
            //    - confirming required packages/dependencies
            //    - setting class member variables and properties that will be used later.
            //
            // 4) ExecuteAsync
            //    Allows the developer to do the main body of work associated with the component. This might be running a workload or test 
            //    and capturing metrics. This might be running a stress test on the system that does not itself produce any specific metrics.
            //
            // 5) CleanupAsync
            //    Allows the developer to perform any cleanup steps/operations required. Its a good idea to always cleanup any artifacts
            //    that were created during the execution of the component in case the same profile is executed again on the system
            //    (i.e. components should be idempotent).
        }

        /// <summary>
        /// Parameter defines the command line arguments to pass to the workload executable.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// 'ExampleParameter1' parameter defined in the workload profile.
        /// </summary>
        public string ExampleParameter1
        {
            get
            {
                // Some parameters are optional. When they are optional, it is important to return either 
                // a null value or a default value when it is not defined in the profile. This is an example
                // of returning a null value.
                this.Parameters.TryGetValue(nameof(this.ExampleParameter1), out IConvertible parameter1Value);
                return parameter1Value?.ToString();
            }
        }

        /// <summary>
        /// 'ExampleParameter2' parameter defined in the workload profile.
        /// </summary>
        public int ExampleParameter2
        {
            get
            {
                // Some parameters are optional. When they are optional, it is important to return either 
                // a null value or a default value when it is not defined in the profile. This is an example
                // of returning a default value.
                return this.Parameters.GetValue<int>(nameof(this.ExampleParameter2), defaultValue: 1234);
            }
        }

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
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // The ExecuteAsync method is where the core workload/test logic is executed. By the time we are here, we should
            // be confident that all required dependencies have been verified in the InitializeAsync() method below and
            // that we are ready to rock!!

            try
            {
                // We use the ILogger to emit telemetry. There are a number of extension/helper methods available
                // that help with keeping telemetry logic simple, clean and consistent.
                this.Logger.LogTraceMessage($"{nameof(ExampleWorkloadExecutor)}.Starting", telemetryContext);

                // Dates should ALWAYS be represented in UTC.
                DateTime startTime = DateTime.UtcNow;

                string workloadResults = await this.ExecuteWorkloadAsync(this.CommandLine, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                DateTime finishTime = DateTime.UtcNow;

                await this.CaptureMetricsAsync(workloadResults, this.CommandLine, startTime, finishTime, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when a Task.Delay is cancelled.
            }
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
            this.WorkloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
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
                await this.systemManagement.MakeFileExecutableAsync(this.WorkloadExecutablePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }

            // We also typically check to make sure any expected binaries, scripts, executables etc... that are required are found
            // in the workload package that was installed.
            if (!this.fileSystem.File.Exists(this.WorkloadExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.WorkloadExecutablePath}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Determines if the executor should be included in the set of components executed.
        /// </summary>
        protected override bool IsSupported()
        {
            return base.IsSupported();
        }

        /// <summary>
        /// Allows the parameters passed into the component to be validated. This allows the
        /// developer to ensure the definitions in the workload profile are valid and have defined
        /// required information.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            this.ThrowIfParameterNotDefined(nameof(this.ExampleParameter1));
            this.ThrowIfParameterNotDefined(nameof(this.ExampleParameter2));
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
                foreach (var item in workloadMetrics)
                {
                    item.Metadata.Add("product", "VirtualClient");
                    item.Metadata.Add("company", "Microsoft");
                    item.Metadata.Add("csp", "Azure");
                }

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

        private Task<string> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // When writing new events in a given method, tt is common practice to clone a telemetry context object and add
            // any additional context to that new object that could be useful for triage/debugging. In this example, we
            // want to make sure that we are capturing the command line executable path and arguments that we are about to
            // run. This is very helpful for repro when issues happen.
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext("packagePath", this.WorkloadPackage.Path)
                .AddContext("command", this.WorkloadExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{nameof(ExampleWorkloadExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                // This example shows how to integrate with monitors that run "on-demand" to do background profiling
                // work while the workload is running. To integrate with any one or more of these monitors (defined in monitor profiles),
                // simply wrap the logic for running the workload in a 'BackgroundProfiling' block.
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // We create a operating system process to host the executing workload, start it and
                    // wait for it to exit.
                    using (IProcessProxy workloadProcess = this.processManager.CreateProcess(this.WorkloadExecutablePath, commandArguments, this.WorkloadPackage.Path))
                    {
                        this.CleanupTasks.Add(() => workloadProcess.SafeKill());

                        await workloadProcess.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // ALWAYS log the details for the process. This helper method will ensure we capture the exit code, standard output, standard
                            // error etc... This is very helpful for triage/debugging.
                            await this.LogProcessDetailsAsync(workloadProcess, telemetryContext, "ExampleWorkload", logToFile: true)
                                .ConfigureAwait(false);

                            this.Logger.LogSystemEvent(
                                "ProcessResult",
                                "ExampleWorkload",
                                workloadProcess.ExitCode.ToString(),
                                workloadProcess.ExitCode == 0 ? LogLevel.Information : LogLevel.Error,
                                telemetryContext,
                                eventCode: workloadProcess.ExitCode,
                                eventInfo: new Dictionary<string, object>
                                {
                                    { "toolset", "ExampleWorkload" },
                                    { "command", $"{this.WorkloadExecutablePath} {commandArguments}" },
                                    { "exitCode", workloadProcess.ExitCode },
                                    { "standardOutput", workloadProcess.StandardOutput?.ToString() },
                                    { "standardError", workloadProcess.StandardError?.ToString() },
                                    { "workingDirectory", this.WorkloadPackage.Path }
                                });

                            this.Logger.LogSystemEvent(
                                "KeyResult",
                                "ExampleWorkload",
                                "keyresult_100",
                                workloadProcess.ExitCode == 0 ? LogLevel.Information : LogLevel.Error,
                                telemetryContext,
                                eventCode: 100);

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
    }
}