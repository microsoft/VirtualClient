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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Latte client-side workload executor.
    /// </summary>
    [SupportedPlatforms("win-arm64,win-x64")]
    public class LatteClientExecutor : LatteExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LatteClientExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public LatteClientExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LatteClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken, TimeSpan? timeout = null)
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
                                this.CleanupTasks.Add(() => process.SafeKill(this.Logger));
                                await process.StartAndWaitAsync(cancellationToken, timeout, withExitConfirmation: true);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await this.LogProcessDetailsAsync(process, relatedContext, "Latte");
                                    process.ThrowIfWorkloadFailed();

                                    this.CaptureMetrics(
                                        process.StandardOutput.ToString(),
                                        process.FullCommand(),
                                        process.StartTime,
                                        process.ExitTime,
                                        relatedContext);
                                }
                            }
                            catch (TimeoutException exc)
                            {
                                // We give this a best effort but do not want it to prevent the next workload
                                // from executing.
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill(this.Logger);
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill(this.Logger);
                                throw;
                            }
                        }
                    });
                }

                return process;
            });
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected override void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (!string.IsNullOrWhiteSpace(results))
            {
                this.MetadataContract.AddForScenario(
                    this.Tool.ToString(),
                    commandArguments,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                MetricsParser parser = new LatteMetricsParser(results);
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
                    telemetryContext,
                    results);
            }
        }

        private void InitializeWindowsClientCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            this.CommandLineWindowsClient ??= string.Empty;

            if (this.CommandLineWindowsClient.Length > 0 && !char.IsWhiteSpace(this.CommandLineWindowsClient[^1]))
            {
                this.CommandLineWindowsClient += " ";
            }

            if (!this.CommandLineWindowsClient.Contains("--tcp", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsClient += this.Protocol.ToLowerInvariant() == "tcp" ? " --tcp" : string.Empty;
            }

            if (!this.CommandLineWindowsClient.Contains("-i", StringComparison.OrdinalIgnoreCase) && this.Iterations != 0)
            {
                this.CommandLineWindowsClient += $" -i {this.Iterations} ";
            }

            if (!this.CommandLineWindowsClient.Contains("-riopoll", StringComparison.OrdinalIgnoreCase) && this.RioPoll != 0)
            {
                this.CommandLineWindowsClient += $" -riopoll {this.RioPoll}";
            }

            if (this.Protocol != null && !this.CommandLineWindowsClient.Contains($"-{this.Protocol.ToString().ToLowerInvariant()}"))
            {
                this.CommandLineWindowsClient += $" -{this.Protocol.ToString().ToLowerInvariant()}";
            }

            this.CommandLineWindowsClient = this.CommandLineWindowsClient.Trim();
        }
    }
}
