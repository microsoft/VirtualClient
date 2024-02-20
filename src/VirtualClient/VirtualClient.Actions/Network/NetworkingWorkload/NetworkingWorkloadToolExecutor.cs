// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Networking Workload Tool Executor class.
    /// </summary>
    public abstract class NetworkingWorkloadToolExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadToolExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        protected NetworkingWorkloadToolExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Name of the scenario.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///  Path to the executable of powershell7.
        /// </summary>
        public string PowerShell7ExePath { get; internal set; }

        /// <summary>
        /// Tool executable path.
        /// </summary>
        public string ExecutablePath { get; internal set; }

        /// <summary>
        /// Powershell script path. 
        /// </summary>
        public string PowerShellScriptPath { get; internal set; }

        /// <summary>
        /// Path to the metrics/results.
        /// </summary>
        public string ResultsPath { get; internal set; }

        /// <summary>
        /// Role (Server/Client).
        /// </summary>
        public string Role { get; internal set; }

        /// <summary>
        ///  Name of the tool (NTttcp,CPS,Latte,SockPerf).
        /// </summary>
        public NetworkingWorkloadTool Tool { get; set; }

        /// <summary>
        /// Process name of the tool.
        /// </summary>
        public string ProcessName { get; set; }

        /// <summary>
        /// The name of the PowerShell7 NuGet package downloaded from the NuGet feed.
        /// </summary>
        public string PowerShell7PackageName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadToolExecutor.PowerShell7PackageName), out IConvertible powerShell7Name);
                return powerShell7Name?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadToolExecutor.PowerShell7PackageName)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the timeout to use when polling the server-side API for heartbeat and
        /// state changes.
        /// </summary>
        public TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        public ISystemManagement SystemManagement
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        /// True/false whether the workload (e.g. NTttcp, CPS, SockPerf) is expected
        /// to emit results. Latte and SockPerf server-side workloads do not emit results.
        /// </summary>
        protected bool WorkloadEmitsResults { get; set; }

        /// <summary>
        /// Returns true if the local instance is in the Client role.
        /// </summary>
        protected bool IsInClientRole { get; set; }

        /// <summary>
        /// Returns true if the local instance is in the Server role.
        /// </summary>
        protected bool IsInServerRole { get; set; }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected virtual Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the tool.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsInClientRole)
            {
                await this.ExecuteClientAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (this.IsInServerRole)
            {
                await this.ExecuteServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the client-side workload.
        /// </summary>
        protected async Task ExecuteClientAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            await this.EnableInboundFirewallAccessAsync(cancellationToken)
                .ConfigureAwait(false);

            Item<NetworkingWorkloadState> state = await NetworkingWorkloadExecutor.ServerApiClient.GetStateAsync<NetworkingWorkloadState>(
                nameof(NetworkingWorkloadState),
                cancellationToken).ConfigureAwait(false);

            if (state != null && state.Definition.ToolState == NetworkingWorkloadToolState.Running)
            {
                NetworkingWorkloadState workloadState = state.Definition;
                await this.DeleteResultsFileAsync().ConfigureAwait(false);

                // Note:
                // We found that certain of the workloads do not exit when they are supposed to. We enforce an
                // absolute timeout to ensure we do not waste too much time with a workload that is stuck.
                TimeSpan workloadTimeout = TimeSpan.FromSeconds(state.Definition.WarmupTime + (state.Definition.TestDuration * 2));

                string commandArguments = this.GetCommandLineArguments();
                DateTime startTime = DateTime.UtcNow;

                await this.ExecuteWorkloadAsync(commandArguments, workloadTimeout, relatedContext, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;

                if (!cancellationToken.IsCancellationRequested)
                {
                    // There is sometimes a delay in the output of the results to the results
                    // file. We will poll for the results for a period of time before giving up.
                    await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), relatedContext, cancellationToken)
                        .ConfigureAwait(false);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.CaptureMetricsAsync(commandArguments, startTime, endTime, relatedContext)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Executes the server-side workload.
        /// </summary>
        protected Task ExecuteServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The server-side logic to execute the server workload CANNOT block. It must return immediately.
            return Task.Run(async () =>
            {
                EventContext relatedContext = telemetryContext.Clone();
                Item<NetworkingWorkloadState> state = await NetworkingWorkloadExecutor.LocalApiClient.GetStateAsync<NetworkingWorkloadState>(
                    nameof(NetworkingWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (state != null)
                {
                    relatedContext.AddContext("initialState", state);

                    await this.EnableInboundFirewallAccessAsync(cancellationToken)
                        .ConfigureAwait(false);

                    try
                    {
                        await this.DeleteResultsFileAsync().ConfigureAwait(false);

                        // Note:
                        // We found that certain of the workloads do not exit when they are supposed to. We enforce an
                        // absolute timeout to ensure we do not waste too much time with a workload that is stuck.
                        TimeSpan workloadTimeout = TimeSpan.FromSeconds(state.Definition.WarmupTime + (state.Definition.TestDuration * 2));

                        string commandArguments = this.GetCommandLineArguments();
                        DateTime startTime = DateTime.UtcNow;
                        List<Task> workloadTasks = new List<Task>
                        {
                            this.ConfirmProcessRunningAsync(state, relatedContext, cancellationToken),
                            this.ExecuteWorkloadAsync(commandArguments, workloadTimeout, relatedContext, cancellationToken)
                        };

                        await Task.WhenAll(workloadTasks).ConfigureAwait(false);
                        DateTime endTime = DateTime.UtcNow;

                        if (this.WorkloadEmitsResults)
                        {
                            // There is sometimes a delay in the output of the results to the results
                            // file. We will poll for the results for a period of time before giving up.
                            await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), relatedContext, cancellationToken)
                                .ConfigureAwait(false);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.CaptureMetricsAsync(commandArguments, startTime, endTime, relatedContext)
                                    .ConfigureAwait(false);
                            }
                        }
                    }
                    finally
                    {
                        await NetworkingWorkloadExecutor.LocalApiClient.DeleteStateAsync(nameof(NetworkingWorkloadState), cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            });
        }

        /// <summary>
        /// Enable the firewall rule for the tool executable.
        /// </summary>
        protected async Task EnableInboundFirewallAccessAsync(CancellationToken cancellationToken)
        {
            if (this.ExecutablePath != null)
            {
                FirewallEntry firewallEntry = new FirewallEntry(
                    $"Virtual Client: Allow {this.ExecutablePath}",
                    "Allows client and server instances of the Virtual Client to communicate via the self-hosted API service.",
                    this.ExecutablePath);

                await this.SystemManagement.FirewallManager.EnableInboundAppAsync(firewallEntry, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes the powershell script.
        /// </summary>
        /// <param name="commandArguments">The command arguments to use to run the workload toolset.</param>
        /// <param name="timeout">The absolute timeout for the workload.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns package path.
        /// </summary>
        protected DependencyPath GetDependencyPath(string packageName, CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = packageManager.GetPackageAsync(packageName, cancellationToken)
               .GetAwaiter().GetResult();

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{packageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            return workloadPackage;
        }

        /// <summary>
        /// Set state of the workload to running after process starts.
        /// </summary>
        protected async Task ConfirmProcessRunningAsync(Item<NetworkingWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            try
            {
                DateTime timeout = DateTime.UtcNow.AddMinutes(2);

                while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
                {
                    if (this.IsProcessRunning(this.ProcessName))
                    {
                        state.Definition.ToolState = NetworkingWorkloadToolState.Running;
                        await NetworkingWorkloadExecutor.UpdateStateAsync(
                            NetworkingWorkloadExecutor.LocalApiClient,
                            state,
                            telemetryContext,
                            cancellationToken).ConfigureAwait(false);

                        this.Logger.LogTraceMessage($"Synchronization: {state.Definition.Tool} workload confirmed running...", relatedContext);
                        break;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when Task.Delay options are cancelled.
            }
        }

        /// <summary>
        /// Checks if the process exists.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>true if process name exists.</returns>
        protected virtual bool IsProcessRunning(string processName)
        {
            bool success = false;
            List<Process> processlist = new List<Process>(Process.GetProcesses());

            if (processlist.Where(
                process => string.Equals(
                process.ProcessName,
                processName,
                StringComparison.OrdinalIgnoreCase)).Count() != 0)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Produces powershell script parameters using the workload parameters provided.
        /// </summary>
        /// <returns>Powershell script parameters as a string.</returns>
        protected abstract string GetCommandLineArguments();

        /// <summary>
        /// Returns true if results are found in the results file within the polling/timeout
        /// period specified.
        /// </summary>
        protected virtual async Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IFile fileAccess = this.SystemManagement.FileSystem.File;
            IDictionary<string, double> results = null;
            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            EventContext relatedContext = telemetryContext.Clone();

            while (DateTime.UtcNow < pollingTimeout && !cancellationToken.IsCancellationRequested)
            {
                if (fileAccess.Exists(this.ResultsPath))
                {
                    try
                    {
                        string resultsContent = await this.SystemManagement.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(resultsContent))
                        {
                            this.Logger.LogMessage($"{this.TypeName}.WorkloadOutputFileContents", relatedContext
                                .AddContext("results", resultsContent));

                            break;
                        }
                    }
                    catch (IOException)
                    {
                        // This can be hit if the application is exiting/cancelling while attempting to read
                        // the results file.
                    }
                }

                await this.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            if (results?.Any() == false)
            {
                throw new WorkloadResultsException(
                    $"Results not found. The workload '{this.ExecutablePath}' did not produce any valid results.",
                    ErrorReason.WorkloadFailed);
            }
        }

        private async Task DeleteResultsFileAsync()
        {
            if (this.SystemManagement.FileSystem.File.Exists(this.ResultsPath))
            {
                await this.SystemManagement.FileSystem.File.DeleteAsync(this.ResultsPath)
                    .ConfigureAwait(false);
            }
        }
    }
}
