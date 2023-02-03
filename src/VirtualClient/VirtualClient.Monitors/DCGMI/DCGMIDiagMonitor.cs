// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The DCGMI Diag Monitor for GPU
    /// </summary>
    public class DCGMIDiagMonitor : VirtualClientIntervalBasedMonitor
    {
        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DCGMIDiagMonitor"/> class.
        /// </summary>
        public DCGMIDiagMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Level specifies which set of tests we need to run.
        /// </summary>
        public string Level
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DCGMIDiagMonitor.Level), out IConvertible level);
                return level?.ToString();
            }
        }

        /// <summary>
        /// Initializes enviroment to run DCGMI Diag Monitor.
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.ExecuteCommandAsync<DCGMIDiagMonitor>(@"nvidia-smi -pm 1", Environment.CurrentDirectory, cancellationToken)
                .ConfigureAwait(false);

            State installationState = await this.stateManager.GetStateAsync<State>(nameof(DCGMIDiagMonitor), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
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
                                $"{nameof(DCGMIDiagMonitor)} is not supported on the current Linux distro - {linuxDistributionInfo.LinuxDistribution.ToString()}.  through VC " +
                                $" Supported distros include:" +
                                $" Ubuntu, Debian, CentOS8, RHEL8, SUSE",
                                ErrorReason.LinuxDistributionNotSupported);
                    }

                    await this.ExecuteCommandAsync<DCGMIDiagMonitor>(@"nvidia-smi -e 1", Environment.CurrentDirectory, cancellationToken)
                        .ConfigureAwait(false);

                    await this.stateManager.SaveStateAsync(nameof(DCGMIDiagMonitor), new State(), cancellationToken)
                    .ConfigureAwait(false);

                    SystemManagement.IsRebootRequested = true;
                }
                else
                {
                    throw new WorkloadException(
                                $"{nameof(DCGMIDiagMonitor)} is not supported on the current platform {this.Platform} through VC." +
                                $"Supported Platforms include:" +
                                $" Unix ",
                                ErrorReason.PlatformNotSupported);
                }
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.
            State installationState = await this.stateManager.GetStateAsync<State>(nameof(DCGMIDiagMonitor), cancellationToken)
                .ConfigureAwait(false);
            if (installationState != null)
            {
                switch (this.Platform)
                {
                    case PlatformID.Win32NT:
                        // This is not supported on Windows, skipping
                        break;

                    case PlatformID.Unix:
                        await this.ExecuteDCGMDiagCommandAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                }
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
            where TExecutor : VirtualClientIntervalBasedMonitor
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

        private async Task ExecuteDCGMDiagCommandAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "dcgmi diag";
            string commandArguments = $"-r {this.Level} -j";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.Logger.LogTraceMessage($"Executing process '{command}' '{commandArguments}' at directory '{Environment.CurrentDirectory}'.");
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, $"{commandArguments}", Environment.CurrentDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        DateTime startTime = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        DateTime endTime = DateTime.UtcNow;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                // We cannot log the process details here. The output is too large.
                                this.Logger.LogProcessDetails<DCGMIDiagMonitor>(process, EventContext.Persisted());
                                process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

                                if (process.StandardOutput.Length > 0)
                                {
                                    Console.WriteLine("results are " + process.StandardOutput.ToString());
                                    DCGMIDiagCommandParser parser = new DCGMIDiagCommandParser(process.StandardOutput.ToString());
                                    IList<Metric> metrics = parser.Parse();

                                    if (metrics?.Any() == true)
                                    {
                                        this.Logger.LogPerformanceCounters("dcgmi diag", metrics, startTime, endTime, telemetryContext);
                                    }
                                }
                            }
                            catch
                            {
                                // We cannot log the process details here. The output is too large. We will log on errors
                                // though.
                                this.Logger.LogProcessDetails<DCGMIDiagMonitor>(process, EventContext.Persisted());
                                throw;
                            }
                        }

                        await Task.Delay(this.MonitorFrequency).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever ctrl-C is used.
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }
    }
}
