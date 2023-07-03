// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// CtsTraffic Server executor.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class CtsTrafficServerExecutor : CtsTrafficExecutor
    {
        private Item<CtsTrafficServerState> serverState;

        /// <summary>
        /// Initializes a new instance of the <see cref="CtsTrafficServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public CtsTrafficServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Parameter defines exactly the number of connections an instance of ctsTraffic should handle before exiting.
        /// </summary>
        public int ServerExitLimit
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerExitLimit), 1);
            }
        }

        /// <summary>
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetServerOnline(false);
            base.InitializeAsync(telemetryContext, cancellationToken);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Start ctstraffic workload at server side.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.serverState = await this.LocalApiClient.GetOrCreateStateAsync<CtsTrafficServerState>(
                    nameof(CtsTrafficServerState),
                    cancellationToken,
                    logger: this.Logger);

            try
            {
                // 1. Start 1st phase
                // 2. Create phase 1 state
                // 3. Wait for phase 1 process to finish
                // 4. delete phase1 state
                // 5. Start 2nd phase
                // 6. Create phase 2 state
                // 7. wait for phase 2 to finish
                // 8. Delet phase 2 state

                string primaryCommand = $@"-Listen:* -Consoleverbosity:1 -Statusfilename:{this.StatusFileName} " +
                $@"-Connectionfilename:{this.ConnectionsFileName} -ErrorFileName:{this.ErrorFileName} -Port:{this.PrimaryPort} " +
                $@"-Pattern:{this.Pattern} -Transfer:32 " +
                $@"-TimeLimit:150000 -ServerExitLimit:1";

                string secondaryCommand = $@"{this.NumaNode} '{this.CtsTrafficExe} -Listen:* -Consoleverbosity:1 -Statusfilename:{this.StatusFileName} " +
                $@"-Connectionfilename:{this.ConnectionsFileName} -ErrorFileName:{this.ErrorFileName} -Port:{this.SecondaryPort} " +
                $@"-Pattern:{this.Pattern} -Transfer:{this.BytesToTransfer} -ServerExitLimit:{this.ServerExitLimit} " +
                $@"-Buffer:{this.BufferInBytes} -TimeLimit:150000'";

                bool phase1State = true;

                this.ExecuteWorkload(this.CtsTrafficExe, primaryCommand, phase1State, telemetryContext, cancellationToken);

                this.ExecuteWorkload(this.ProcessInNumaNodeExe, secondaryCommand, !phase1State, telemetryContext, cancellationToken);

                using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }

            }
            catch
            {
                this.SetServerOnline(false);

                using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }

                throw;
            }
        }

        private async Task<IProcessProxy> ExecuteAndWaitCommandAsync(
            string command,
            string commandArguments,
            string workingDirectory,
            bool phase1State,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments)
                .AddContext(nameof(workingDirectory), workingDirectory);

            IProcessProxy process = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                ProcessManager processManager = this.Dependencies.GetService<ProcessManager>();

                process = processManager.CreateProcess(command, commandArguments, workingDirectory);

                this.CleanupTasks.Add(() => process.SafeKill());
                this.Logger.LogTraceMessage($"Executing: {command} {SensitiveData.ObscureSecrets(commandArguments)}".Trim(), relatedContext);

                using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }

                process.Start();
                this.SetServerOnline(true);
                this.SaveServerStateAsync(phase1State, !phase1State, cancellationToken);

                await process.WaitForExitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return process;
        }

        private void ExecuteWorkload(string exe, string command, bool phase1State, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext, async () =>
            {
                DateTime startTime = DateTime.UtcNow;

                using (IProcessProxy process = await this.ExecuteAndWaitCommandAsync(
                    exe,
                    command,
                    this.CtsTrafficPackagePath,
                    phase1State,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "CtsTraffic", logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        await this.CaptureMetricsAsync(process, command, telemetryContext, cancellationToken);
                    }
                }
            });
        }

        private async Task SaveServerStateAsync(bool phase1State, bool phase2State, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.serverState.Definition.ServerSetupPhase1Completed = phase1State;
                this.serverState.Definition.ServerSetupPhase2Completed = phase2State;

                using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync<CtsTrafficServerState>(nameof(CtsTrafficServerState), this.serverState, cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }
            }
        }
    }
}