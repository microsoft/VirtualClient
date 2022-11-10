// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Latte server-side workload executor.
    /// </summary>
    [WindowsCompatible]
    public class LatteServerExecutor : LatteExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatteServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.WorkloadEmitsResults = false;
        }

        /// <inheritdoc/>
        protected override Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IProcessProxy process = null;

            EventContext relatedContext = telemetryContext.Clone()
               .AddContext("command", this.ExecutablePath)
               .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundProfiling profiling = BackgroundProfiling.Begin(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            using (process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                            {
                                if (!process.Start())
                                {
                                    this.Logger.LogProcessDetails<LatteServerExecutor>(process, relatedContext);
                                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                                }

                                this.CleanupTasks.Add(() => process.SafeKill());

                                // Run the server slightly longer than the test duration.
                                TimeSpan serverWaitTime = TimeSpan.FromMilliseconds(this.Iterations * .5);
                                await this.WaitAsync(serverWaitTime, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }
                        catch (Exception exc)
                        {
                            this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                            process.SafeKill();
                            throw;
                        }
                        finally
                        {
                            this.Logger.LogProcessDetails<LatteServerExecutor>(process, relatedContext);
                        }
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        /// <summary>
        /// Produces powershell script parameters using the workload parameters provided.
        /// </summary>
        /// <returns>Powershell script parameters as a string.</returns>
        protected override string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().PrivateIPAddress;
            return $"-a {serverIPAddress}:{this.Port} -rio -i {this.Iterations} -riopoll {this.RioPoll} -{this.Protocol.ToLowerInvariant()}";
        }

        /// <summary>
        /// Not applicable on the server-side
        /// </summary>
        protected override Task LogMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            // Latte server-side does not generate results.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Not applicable on the server-side
        /// </summary>
        protected override Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Latte server-side does not generate results.
            return Task.CompletedTask;
        }
    }
}
