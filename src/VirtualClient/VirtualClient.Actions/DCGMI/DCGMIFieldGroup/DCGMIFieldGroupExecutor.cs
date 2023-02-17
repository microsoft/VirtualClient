// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
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
    /// The DCGMI Field Group Executor for GPU
    /// </summary>
    [UnixCompatible]
    public class DCGMIFieldGroupExecutor : VirtualClientComponent
    {
        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DCGMIFieldGroupExecutor"/> class.
        /// </summary>
        /// /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DCGMIFieldGroupExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Initializes enviroment to run DCGMI Field Group.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken)
                                                .ConfigureAwait(false);

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
                            $"{nameof(DCGMIFieldGroupExecutor)} is not supported on the current Linux distro - {linuxDistributionInfo.LinuxDistribution.ToString()}.  through VC " +
                            $" Supported distros include:" +
                            $" Ubuntu, Debian, CentOS8, RHEL8, SUSE",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else
            {
                throw new WorkloadException(
                            $"{nameof(DCGMIFieldGroupExecutor)} is not supported on the current platform {this.Platform} through VC." +
                            $"Supported Platforms include:" +
                            $" Unix ",
                            ErrorReason.PlatformNotSupported);
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
                    await this.ExecuteDCGMIFieldGroupListCommandAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
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
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                        process.RedirectStandardOutput = true;
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<TExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();
                    }

                    return output;
                }).ConfigureAwait(false);
            }

            return output;
        }

        private async Task ExecuteDCGMIFieldGroupListCommandAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "dcgmi fieldgroup -l";
            this.StartTime = DateTime.Now;
            string results = await this.ExecuteCommandAsync<DCGMIFieldGroupExecutor>(command, Environment.CurrentDirectory, cancellationToken)
            .ConfigureAwait(false);

            Console.WriteLine("results are " + results);

            this.CaptureWorkloadResultsAsync(results, this.StartTime, DateTime.Now, telemetryContext, cancellationToken);
        }

        private void CaptureWorkloadResultsAsync(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    DCGMIFieldGroupCommandParser dcgmiFieldGroupCommandParser = new DCGMIFieldGroupCommandParser(results);
                    IList<Metric> metrics = dcgmiFieldGroupCommandParser.Parse();
                    this.Logger.LogMetrics(
                                "DCGMIFieldGroup",
                                scenarioName: this.Scenario,
                                startTime,
                                endTime,
                                metrics,
                                string.Empty,
                                this.Parameters.ToString(),
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
