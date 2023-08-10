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
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetServerOnline(false);

            await base.InitializeAsync(telemetryContext, cancellationToken);

            DependencyPath ctsTrafficPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.CtsTrafficPackagePath = ctsTrafficPackage.Path;
        }

        /// <summary>
        /// Start ctstraffic workload at server side.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                string ctsTrafficCommandArgs = "-Listen:* -Consoleverbosity:1 -StatusFilename:{this.StatusFileName} " +
                $@"-ConnectionFilename:{this.ConnectionsFileName} -ErrorFileName:{this.ErrorFileName} -Port:{this.Port} " +
                $@"-Pattern:{this.Pattern} -Transfer:{this.BytesToTransfer} -ServerExitLimit:{this.ServerExitLimit} " +
                $@"-Buffer:{this.BufferInBytes} -TimeLimit:150000";

                string numaNodeCommandArgs = $@"{this.NumaNodeIndex} ""{this.CtsTrafficExe} {ctsTrafficCommandArgs}""";

                if (this.NumaNodeIndex == -1)
                {
                    await this.ExecuteAndWaitCommandAsync(this.CtsTrafficExe, ctsTrafficCommandArgs, this.CtsTrafficPackagePath, telemetryContext, cancellationToken);

                    using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken))
                    {
                        response.ThrowOnError<WorkloadException>();
                    }
                }
                else
                {
                    await this.ExecuteAndWaitCommandAsync(this.ProcessInNumaNodeExe, numaNodeCommandArgs, this.CtsTrafficPackagePath, telemetryContext, cancellationToken);

                    using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken))
                    {
                        response.ThrowOnError<WorkloadException>();
                    }
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

        private async Task ExecuteAndWaitCommandAsync(
            string command,
            string commandArguments,
            string workingDirectory,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            commandArguments.ThrowIfNullOrWhiteSpace(nameof(commandArguments));
            workingDirectory.ThrowIfNullOrWhiteSpace(nameof(workingDirectory));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments)
                .AddContext(nameof(workingDirectory), workingDirectory);

            await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                IProcessProxy process = null;
                if (!cancellationToken.IsCancellationRequested)
                {
                    ProcessManager processManager = this.Dependencies.GetService<ProcessManager>();

                    process = processManager.CreateProcess(command, commandArguments, workingDirectory);

                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.Logger.LogTraceMessage($"Executing: {command} {commandArguments}".Trim(), relatedContext);

                    if (!process.Start())
                    {
                        throw new WorkloadException($"The workload did not start as expected.", ErrorReason.WorkloadFailed);
                    }

                    this.SetServerOnline(true);
                    this.SaveServerStateAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await process.WaitForExitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    await this.LogProcessDetailsAsync(process, relatedContext, "CtsTraffic", logToFile: true);
                    process.ThrowIfWorkloadFailed();

                    await this.CaptureMetricsAsync(process, commandArguments, relatedContext, cancellationToken);

                    await this.LocalApiClient.DeleteStateAsync(nameof(CtsTrafficServerState), cancellationToken)
                        .ConfigureAwait(false);
                }
            });

            await this.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        private async Task SaveServerStateAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.serverState = await this.LocalApiClient.GetOrCreateStateAsync<CtsTrafficServerState>(
                    nameof(CtsTrafficServerState),
                    cancellationToken,
                    logger: this.Logger);

                this.serverState.Definition.ServerSetupCompleted = true;

                using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync<CtsTrafficServerState>(nameof(CtsTrafficServerState), this.serverState, cancellationToken))
                {
                    response.ThrowOnError<WorkloadException>();
                }
            }
        }
    }
}