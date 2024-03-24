// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// This is an example workload that is used to illustrate testing patterns
    /// for both unit and functional tests.
    /// </summary>
    public class Example2WorkloadExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ProcessManager processManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Example2WorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.processManager = this.systemManagement.ProcessManager;
            this.stateManager = this.systemManagement.StateManager;
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
        /// Parameter indicates the name of the test.
        /// </summary>
        public string TestName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TestName));
            }
        }

        /// <summary>
        /// The path to the workload executable.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the results file (if defined).
        /// </summary>
        protected string ResultsFilePath { get; set; }

        /// <summary>
        /// The workload state object ID.
        /// </summary>
        protected string StateId
        {
            get
            {
                return $"{nameof(Example2WorkloadExecutor)}-state";
            }
        }

        /// <summary>
        /// Mimics a requirement of performing some system settings update that requires a reboot.
        /// </summary>
        protected async Task ApplySystemSettingsAsync(CancellationToken cancellationToken)
        {
            string configurationCommand = this.Platform == PlatformID.Win32NT
                ? "configureSystem.exe"
                : "configureSystem";

            using (IProcessProxy process = this.processManager.CreateElevatedProcess(this.Platform, configurationCommand))
            {
                await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(10))
                    .ConfigureAwait(false);

                process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // This is just to illustrate aspects of the workload that require saving state (e.g. to the file system).
            // Workloads that have to communicate between different instances of the Virtual Client often use state to
            // communicate (e.g. client/server interactions).
            WorkloadState workloadState = await this.stateManager.GetStateAsync<WorkloadState>(this.StateId, cancellationToken)
                .ConfigureAwait(false);

            if (workloadState == null)
            {
                // Mimics a requirement of performing some system settings update that requires a reboot.
                await this.ApplySystemSettingsAsync(cancellationToken)
                    .ConfigureAwait(false);

                await this.stateManager.SaveStateAsync<WorkloadState>(this.StateId, new WorkloadState { IsFirstRun = false }, cancellationToken)
                    .ConfigureAwait(false);

                // A reboot happens out-of-band from the executor to ensure all threads/processes running have a 
                // chance to exit gracefully and for telemetry to be fully emitted from off of the system before
                // processing the actual reboot request.
                this.RequestReboot();
            }
            else
            {
                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes the workload dependencies and requirements.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Almost all workload executors access the workloads via structure packages.
            DependencyPath workloadPackage = await this.InitializeWorkloadPackageAsync(telemetryContext, cancellationToken);
            await this.InitializeWorkloadExecutablesAsync(workloadPackage, telemetryContext, cancellationToken);
        }

        private void CaptureMetrics(IProcessProxy process, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            string results = process.StandardOutput.ToString();
            Example2WorkloadMetricsParser resultsParser = new Example2WorkloadMetricsParser(results);
            IList<Metric> metrics = resultsParser.Parse();

            this.Logger.LogMetrics(
                "SomeWorkload",
                this.TestName,
                startTime,
                endTime,
                metrics,
                null,
                this.CommandLine,
                this.Tags,
                telemetryContext);
        }

        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy workload = this.processManager.CreateProcess(this.ExecutablePath, this.CommandLine))
            {
                DateTime startTime = DateTime.UtcNow;
                await workload.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                DateTime endTime = DateTime.UtcNow;

                await this.LogProcessDetailsAsync(workload, telemetryContext);
                workload.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);

                this.CaptureMetrics(workload, startTime, endTime, telemetryContext);
            }
        }

        private async Task<DependencyPath> InitializeWorkloadPackageAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("workloadPackage", workloadPackage);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            return this.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
        }

        private async Task InitializeWorkloadExecutablesAsync(DependencyPath workloadPackage, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // In this example, there is a main executable but it uses a set of additional
            // executables during execution. On Linux systems, all of these need to be attributed
            // as executable.
            List<string> executablePaths = null;
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    executablePaths = new List<string>
                    {
                        this.Combine(workloadPackage.Path, "SomeWorkload"),
                        this.Combine(workloadPackage.Path, "SomeTool1"),
                        this.Combine(workloadPackage.Path, "SomeTool2")
                    };
                    break;

                case PlatformID.Win32NT:
                    executablePaths = new List<string>
                    {
                        this.Combine(workloadPackage.Path, "SomeWorkload.exe"),
                        this.Combine(workloadPackage.Path, "SomeTool1.exe"),
                        this.Combine(workloadPackage.Path, "SomeTool2.exe")
                    };
                    break;
            }

            telemetryContext.AddContext("workloadExecutables", executablePaths);

            foreach (string path in executablePaths)
            {
                if (!this.fileSystem.File.Exists(path))
                {
                    throw new WorkloadException($"Required workload binary/executable does not exist at the path '{path}'");
                }

                await this.systemManagement.MakeFileExecutableAsync(path, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }

            // We purposefully placed the main workload executable first in the list.
            this.ExecutablePath = executablePaths.First();
        }

        internal class WorkloadState
        {
            [JsonProperty(PropertyName = "isFirstRun")]
            public bool IsFirstRun { get; set; }
        }
    }
}
