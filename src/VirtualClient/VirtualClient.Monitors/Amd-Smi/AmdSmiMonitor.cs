// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Monitors.Amd_Smi;

    /// <summary>
    /// The Performance Counter Monitor for Virtual Client
    /// </summary>
    public class AmdSmiMonitor : VirtualClientIntervalBasedMonitor
    {
        private ISystemManagement systemManagement;
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmdSmiMonitor"/> class.
        /// </summary>
        public AmdSmiMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                switch (this.Platform)
                {
                    case PlatformID.Win32NT:
                        await this.QueryGpuMetricAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                        break;

                    case PlatformID.Unix:
                        await this.QueryGpuMetricAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("executing xgmi");
                            await this.QueryGpuXGMIAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        }

                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] ExecuteAsync failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        protected void ValidateParameters()
        {
            if (this.MonitorFrequency <= TimeSpan.Zero)
            {
                throw new MonitorException(
                    $"The monitor frequency defined/provided for the '{this.TypeName}' component '{this.MonitorFrequency}' is not valid. " +
                    $"The frequency must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task QueryGpuMetricAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string amdSmiMonitorCommand = "amd-smi";
            string commandArgumentsForPower = "metric";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken).ConfigureAwait(false);
            int i = 0;

            while (!cancellationToken.IsCancellationRequested && i < 1)
            {
                try
                {
                    i++;
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, amdSmiMonitorCommand, commandArgumentsForPower, Environment.CurrentDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        DateTime startTime = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                        DateTime endTime = DateTime.UtcNow;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

                            if (process.StandardOutput.Length > 0)
                            {
                                AmdSmiMetricsParser parser = new AmdSmiMetricsParser(process.StandardOutput.ToString());
                                IList<Metric> metrics = parser.Parse();

                                if (metrics?.Any() == true)
                                {
                                    this.Logger.LogPerformanceCounters("amd", metrics, startTime, endTime, telemetryContext);
                                }
                            }
                        }
                    }

                    await Task.Delay(this.MonitorFrequency).ConfigureAwait(false);
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

        private async Task QueryGpuXGMIAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandArguments = "xgmi -m --json";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken).ConfigureAwait(false);
            int i = 0; 

            while (!cancellationToken.IsCancellationRequested && i < 1)
            {
                i++;
                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    var (metrics1, startTime1, endTime1) = await this.ExecuteXGMICommand(commandArguments, cancellationToken);
                    await Task.Delay(500).ConfigureAwait(false);
                    var (metrics2, startTime2, endTime2) = await this.ExecuteXGMICommand(commandArguments, cancellationToken);

                    stopwatch.Stop();
                    long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                    IList<Metric> aggregatedMetrics = this.AmdSmiXGMIBandwidthAggregator(metrics1, metrics2, elapsedMilliseconds);

                    if (aggregatedMetrics?.Any() == true)
                    {
                        this.Logger.LogPerformanceCounters("amd", aggregatedMetrics, startTime1, endTime2, telemetryContext);
                    }

                    await Task.Delay(this.MonitorFrequency).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                { 
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }

        private async Task<(IList<Metric>, DateTime, DateTime)> ExecuteXGMICommand(string commandArguments, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, "amd-smi", commandArguments, Environment.CurrentDirectory))
            {
                this.CleanupTasks.Add(() => process.SafeKill());
                DateTime startTime = DateTime.UtcNow;
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                DateTime endTime = DateTime.UtcNow;

                AmdSmiXGMIQueryGpuParser parser = new AmdSmiXGMIQueryGpuParser(process.StandardOutput.ToString());
                return (parser.Parse(), startTime, endTime);
            }
        }

        private IList<Metric> AmdSmiXGMIBandwidthAggregator(IList<Metric> metrics1, IList<Metric> metrics2, long time)
        {
            List<Metric> aggregatedMetrics = new List<Metric>();

            if (metrics1.Any() && metrics2.Any())
            {
                foreach (Metric counter1 in metrics1)
                {
                    foreach (Metric counter2 in metrics2)
                    {
                        if (counter1.Metadata["gpu.id"] == counter2.Metadata["gpu.id"])
                        {
                            double bandwidth = (counter2.Value - counter1.Value) / (((double)time) / 1000.0);
                            aggregatedMetrics.Add(new Metric($"xgmi.bw", (bandwidth / 1024), unit: "MB/s", metadata: counter1.Metadata));
                        }
                    }
                }
            }

            return aggregatedMetrics;
        }
    }
}
