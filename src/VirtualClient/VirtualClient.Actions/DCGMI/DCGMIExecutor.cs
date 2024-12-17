// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    /// The DCGMI Executor for GPU
    /// </summary>
    [SupportedPlatforms("linux-x64")]
    public class DCGMIExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Name of Diag (Diagnostics) subsystem.
        /// </summary>
        protected const string Diagnostics = "Diagnostics";

        /// <summary>
        /// Name of Discovery subsystem.
        /// </summary>
        protected const string Discovery = "Discovery";

        /// <summary>
        /// Name of FieldGroup subsystem.
        /// </summary>
        protected const string FieldGroup = "FieldGroup";

        /// <summary>
        /// Name of Group subsystem.
        /// </summary>
        protected const string Group = "Group";

        /// <summary>
        /// Name of Health subsystem.
        /// </summary>
        protected const string Health = "Health";

        /// <summary>
        /// Name of Modules subsystem.
        /// </summary>
        protected const string Modules = "Modules";

        /// <summary>
        /// Name of CUDATestGenerator(proftester) subsystem.
        /// </summary>
        protected const string CUDATestGenerator = "CUDATestGenerator";

        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DCGMIExecutor"/> class.
        /// </summary>
        /// /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DCGMIExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// DCGMI Subsystem Name.
        /// </summary>
        public string Subsystem
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DCGMIExecutor.Subsystem), out IConvertible subsystem);
                return subsystem?.ToString();
            }
        }

        /// <summary>
        /// Level specifies which set of tests we need to run.
        /// </summary>
        public string Level
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DCGMIExecutor.Level), out IConvertible level);
                return level?.ToString();
            }
        }

        /// <summary>
        /// FieldID to determine load generator for proftester subsystem.
        /// </summary>
        public string FieldIDProftester
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DCGMIExecutor.FieldIDProftester), out IConvertible fieldidproftester);
                return fieldidproftester?.ToString();
            }
        }

        /// <summary>
        /// List of FieldIDs to get metrics while running dmon command.
        /// </summary>
        public string ListOfFieldIDsDmon
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DCGMIExecutor.ListOfFieldIDsDmon), out IConvertible fieldidsdmon);
                return fieldidsdmon?.ToString();
            }
        }

        /// <summary>
        /// The version of CUDA to be installed in Linux Systems
        /// </summary>
        public string LinuxCudaVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DCGMIExecutor.LinuxCudaVersion), "11.6");
            }
        }

        /// <summary>
        /// Initializes enviroment to run DCGMI.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var linuxDistributionInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken);

            telemetryContext.AddContext("LinuxDistribution", linuxDistributionInfo.LinuxDistribution);

            switch (linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:
                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL8:
                case LinuxDistribution.SUSE:
                    break;

                default:
                    throw new WorkloadException(
                        $"{nameof(DCGMIExecutor)} is not supported on the current Linux distro - {linuxDistributionInfo.LinuxDistribution.ToString()}.  through VC " +
                        $" Supported distros include:" +
                        $" Ubuntu, Debian, CentOS8, RHEL8, SUSE",
                        ErrorReason.LinuxDistributionNotSupported);
            }

            if (this.Subsystem == DCGMIExecutor.Diagnostics)
            {
                await this.ExecuteCommandAsync<DCGMIExecutor>(@"nvidia-smi -pm 1", Environment.CurrentDirectory, cancellationToken)
                        .ConfigureAwait(false);

                State installationState = await this.stateManager.GetStateAsync<State>(nameof(DCGMIExecutor), cancellationToken)
                        .ConfigureAwait(false);

                if (installationState == null)
                {
                    await this.ExecuteCommandAsync<DCGMIExecutor>(@"nvidia-smi -e 1", Environment.CurrentDirectory, cancellationToken)
                        .ConfigureAwait(false);

                    await this.stateManager.SaveStateAsync(nameof(DCGMIExecutor), new State(), cancellationToken)
                    .ConfigureAwait(false);

                    this.RequestReboot();
                }
            }

            if (this.Subsystem == DCGMIExecutor.Health)
            {
                await this.ExecuteCommandAsync<DCGMIExecutor>(@"dcgmi health -s mpi", Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    // This is not supported on Windows, skipping
                    break;

                case PlatformID.Unix:
                    if (this.Subsystem == DCGMIExecutor.Diagnostics)
                    {
                        State installationState = await this.stateManager.GetStateAsync<State>(nameof(DCGMIExecutor), cancellationToken)
                                .ConfigureAwait(false);
                        if (installationState != null)
                        {
                            await this.ExecuteDCGMIDiagnosticsSubsystemAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        }
                    }
                    else if (this.Subsystem == DCGMIExecutor.Discovery)
                    {
                        await this.ExecuteDCGMIDiscoverySubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }
                    else if (this.Subsystem == DCGMIExecutor.FieldGroup)
                    {
                        await this.ExecuteDCGMIFieldGroupSubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }
                    else if (this.Subsystem == DCGMIExecutor.Group)
                    {
                        await this.ExecuteDCGMIGroupSubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }
                    else if (this.Subsystem == DCGMIExecutor.Health)
                    {
                        await this.ExecuteDCGMIHealthSubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }
                    else if (this.Subsystem == DCGMIExecutor.Modules)
                    {
                        await this.ExecuteDCGMIModulesSubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }
                    else if (this.Subsystem == DCGMIExecutor.CUDATestGenerator)
                    {
                        await this.ExecuteDCGMIProfTesterSubsystemAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    }

                    break;
            }
        }

        /// <summary>
        /// Executes the commands.
        /// </summary>
        /// <param name="command">Command that needs to be executed</param>
        /// <param name="workingDirectory">The directory where we want to execute the command</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>Output of the workload command.</returns>
        protected async Task<string> ExecuteCommandAsync<TExecutor>(string command, string workingDirectory, CancellationToken cancellationToken)
            where TExecutor : VirtualClientComponent
        {
            string output = string.Empty;
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{command}'  at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", command);

                await this.Logger.LogMessageAsync($"{typeof(TExecutor).Name}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, null, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        process.RedirectStandardOutput = true;
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "DCGMI", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        output = process.StandardOutput.ToString();
                    }

                    return output;
                }).ConfigureAwait(false);
            }

            return output;
        }

        private async Task ExecuteDCGMIDiagnosticsSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = $"dcgmi diag -r {this.Level} -j";
                DateTime startTime = DateTime.UtcNow;

                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIDiscoverySubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "dcgmi discovery -l";
                DateTime startTime = DateTime.UtcNow;

                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIFieldGroupSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "dcgmi fieldgroup -l";
                DateTime startTime = DateTime.UtcNow;
                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIGroupSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "dcgmi group -l";
                DateTime startTime = DateTime.UtcNow;
                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIHealthSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "dcgmi health -c -j";
                DateTime startTime = DateTime.UtcNow;
                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIModulesSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string command = "dcgmi modules -l";
                DateTime startTime = DateTime.UtcNow;
                string results = await this.ExecuteCommandAsync<DCGMIExecutor>(command, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, command, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteDCGMIProfTesterSubsystemAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                List<Task<string>> tasksList = new List<Task<string>>();
                string dcgmproftestercommand = $"/usr/bin/dcgmproftester{(int)Convert.ToDouble(this.LinuxCudaVersion)} --no-dcgm-validation -t {this.FieldIDProftester} -d 10";
                string dmoncommand = $"dcgmi dmon -e {this.ListOfFieldIDsDmon} -c 15";
                DateTime startTime = DateTime.UtcNow;

                tasksList.Add(Task.Run(async () => await this.ExecuteCommandAsync<DCGMIExecutor>(dcgmproftestercommand, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false)));

                tasksList.Add(Task.Run(async () => await this.ExecuteCommandAsync<DCGMIExecutor>(dmoncommand, Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false)));

                string[] outputresults = await Task.WhenAll<string>(tasksList);

                string dcgmiproftesterresults = outputresults[0];
                string dcgmidmonresults = outputresults[1];

                this.CaptureDmonResultsAsync(dcgmidmonresults, dmoncommand, this.StartTime, DateTime.Now, telemetryContext, cancellationToken);
                this.CaptureWorkloadResultsAsync(dcgmiproftesterresults, dcgmproftestercommand, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            }
        }

        private void CaptureDmonResultsAsync(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "DCGMI",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    DCGMIResultsParser dcgmiResultsParser = new DCGMIResultsParser(results, $"{this.Subsystem}Dmon");
                    IList<Metric> metrics = dcgmiResultsParser.Parse();

                    this.Logger.LogMetrics(
                        "DCGMI",
                        scenarioName: $"{this.Subsystem}Dmon",
                        startTime,
                        endTime,
                        metrics,
                        string.Empty,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private void CaptureWorkloadResultsAsync(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "DCGMI",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    DCGMIResultsParser dcgmiResultsParser = new DCGMIResultsParser(results, this.Subsystem);
                    IList<Metric> metrics = dcgmiResultsParser.Parse();

                    this.Logger.LogMetrics(
                        "DCGMI",
                        scenarioName: this.Subsystem,
                        startTime,
                        endTime,
                        metrics,
                        string.Empty,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }
    }
}
