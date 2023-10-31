// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    /// NTttcp(Test Bandwith and Throughput) Tool Server Executor. 
    /// </summary>
    [UnixCompatible]
    public class SockPerfServerExecutor : SockPerfExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            using (process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                            {
                                if (!process.Start())
                                {
                                    await this.LogProcessDetailsAsync(process, relatedContext, "SockPerf");

                                    // ************** Server will throw 137 sometimes
                                    // PORT =  8201 # TCP sockperf: ERROR: Message received was larger than expected, message ignored. 
                                    // ************** Need investigation
                                    List<int> successCodes = new List<int>() { 0, 137 };
                                    process.ThrowIfErrored<WorkloadException>(successCodes, errorReason: ErrorReason.WorkloadFailed);
                                }
                                else
                                {
                                    try
                                    {
                                        // Wait until the cancellation token is signalled.
                                        await this.WaitAsync(cancellationToken);
                                        process.Close();

                                        await process.WaitForExitAsync(cancellationToken);
                                        await this.LogProcessDetailsAsync(process, relatedContext, "SockPerf");
                                    }
                                    finally
                                    {
                                        // SockPerf must be explicitly terminated given the current implementation. If it is not,
                                        // the process will remain running in the background.
                                        process.SafeKill();
                                    }
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                            throw;
                        }
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        /// <summary>
        /// Returns the Sockperf server-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            // sockperf server -i 10.0.1.1 -p 8201 --tcp
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string protocolParam = this.Protocol.ToLowerInvariant() == "tcp" ? "--tcp" : string.Empty;

            return $"server -i {serverIPAddress} -p {this.Port} {protocolParam}".Trim();
        }

        /// <summary>
        /// Not applicable on the server-side
        /// </summary>
        protected override Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            // Sockperf server-side does not generate results.
            return Task.CompletedTask;
        }

        /// <summary>
        /// Not applicable on the server-side
        /// </summary>
        protected override Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Sockperf server-side does not generate results.
            return Task.CompletedTask;
        }
    }
}
