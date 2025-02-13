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
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Utilities;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The Performance Counter Monitor for Virtual Client
    /// </summary>
    public class AmdSmiMonitor : VirtualClientIntervalBasedMonitor
    {
        /// <summary>
        /// Name of Metric subsystem.
        /// </summary>
        protected const string Metric = "metric";

        /// <summary>
        /// Name of XGMI subsystem.
        /// </summary>
        protected const string XGMI = "xgmi";

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

        /// <summary>
        /// AMDSMI Subsystem Name.
        /// </summary>
        public string Subsystem
        {
            get
            {
                this.Parameters.TryGetValue(nameof(AmdSmiMonitor.Subsystem), out IConvertible subsystem);
                return subsystem?.ToString();
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Subsystem == AmdSmiMonitor.Metric)
            {
                await this.QueryGpuMetricAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }

            if (this.Subsystem == AmdSmiMonitor.XGMI)
            {
                await this.QueryGpuXGMIAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
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

        private string GetAmdSmiCommand()
        {
            string command = string.Empty;
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    command = "amdsmi";
                    break;

                case PlatformID.Unix:
                    command = "amd-smi";
                    break;
            }

            return command;
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

        /// <summary>
        /// Query the gpu for utilization information
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        private async Task QueryGpuMetricAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
            string commandArguments = "metric --csv";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, this.GetAmdSmiCommand(), $"{commandArguments}", Environment.CurrentDirectory))
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
                                process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

                                if (process.StandardOutput.Length > 0)
                                {
                                    AmdSmiMetricQueryGpuParser parser = new AmdSmiMetricQueryGpuParser(process.StandardOutput.ToString());
                                    IList<Metric> metrics = parser.Parse();

                                    if (metrics?.Any() == true)
                                    {
                                        this.Logger.LogPerformanceCounters("amd", metrics, startTime, endTime, telemetryContext);
                                    }
                                }
                            }
                            catch
                            {
                                await this.LogProcessDetailsAsync(process, EventContext.Persisted());
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

        private async Task QueryGpuXGMIAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
            string commandArguments = "xgmi -m --json";
            DateTime startTime1, endTime1, startTime2, endTime2;
            IList<Metric> metrics1, metrics2, aggregatedMetrics;

            Stopwatch stopwatch;
            long elapsedMilliseconds;

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, this.GetAmdSmiCommand(), $"{commandArguments}", Environment.CurrentDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        stopwatch = Stopwatch.StartNew();

                        startTime1 = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        endTime1 = DateTime.UtcNow;

                        AmdSmiXGMIQueryGpuParser parser = new AmdSmiXGMIQueryGpuParser(process.StandardOutput.ToString());
                        metrics1 = parser.Parse();
                    }

                    await Task.Delay(500).ConfigureAwait(false);

                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, this.GetAmdSmiCommand(), $"{commandArguments}", Environment.CurrentDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        startTime2 = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        endTime2 = DateTime.UtcNow;
                        stopwatch.Stop();
                        elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

                        AmdSmiXGMIQueryGpuParser parser = new AmdSmiXGMIQueryGpuParser(process.StandardOutput.ToString());
                        metrics2 = parser.Parse();
                    }

                    aggregatedMetrics = this.AmdSmiXGMIBandwidthAggregator(metrics1, metrics2, time: elapsedMilliseconds);

                    if (aggregatedMetrics?.Any() == true)
                    {
                        this.Logger.LogPerformanceCounters("amd", aggregatedMetrics, startTime1, endTime2, telemetryContext);
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
    }
}
