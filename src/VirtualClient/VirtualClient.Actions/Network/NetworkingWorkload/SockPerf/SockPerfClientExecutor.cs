// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
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
    /// Socket Performance Tool Client Executor. 
    /// </summary>
    [UnixCompatible]
    public class SockPerfClientExecutor : SockPerfExecutor
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.WorkloadEmitsResults = true;
            this.fileSystem = dependencies.GetService<IFileSystem>();
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
                        using (process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                this.CleanupTasks.Add(() => process.SafeKill());
                                await process.StartAndWaitAsync(cancellationToken, timeout);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    if (process.IsErrored())
                                    {
                                        await this.LogProcessDetailsAsync(process, telemetryContext, "SockPerf", logToFile: true);
                                        process.ThrowIfWorkloadFailed();
                                    }

                                    await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), relatedContext, cancellationToken);

                                    string results = await this.LoadResultsAsync(this.ResultsPath, cancellationToken);
                                    await this.LogProcessDetailsAsync(process, telemetryContext, "SockPerf", results: results.AsArray(), logToFile: true);
                                }
                            }
                            catch (TimeoutException exc)
                            {
                                // We give this a best effort but do not want it to prevent the next workload
                                // from executing.
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill();
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill();
                                throw;
                            }
                        }
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        /// <summary>
        /// Returns the Sockperf client-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string protocolParam = this.Protocol.ToLowerInvariant() == "tcp" ? "--tcp" : string.Empty;

            // sockperf under-load -i 10.0.1.1 -p 8201 -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 10.0.1.0
            return $"{this.TestMode} " +
                $"-i {serverIPAddress} " +
                $"-p {this.Port} {protocolParam} " +
                $"-t {this.TestDuration} " +
                $"{(this.MessagesPerSecond.ToLowerInvariant() == "max" ? "--mps=max" : $"--mps {this.MessagesPerSecond}")} " +
                $"--full-rtt --msg-size {this.MessageSize} " +
                $"--client_ip {clientIPAddress} " +
                $"--full-log {this.ResultsPath}";
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected override async Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            IFile fileAccess = this.SystemManagement.FileSystem.File;

            if (fileAccess.Exists(this.ResultsPath))
            {
                string resultsContent = await this.LoadResultsAsync(this.ResultsPath, CancellationToken.None)
                    .ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(resultsContent))
                {
                    MetricsParser parser = new SockPerfMetricsParser(resultsContent, this.ConfidenceLevel);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        this.Tool.ToString(),
                        this.Name,
                        startTime,
                        endTime,
                        metrics,
                        string.Empty,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
                }
            }
        }
    }
}
