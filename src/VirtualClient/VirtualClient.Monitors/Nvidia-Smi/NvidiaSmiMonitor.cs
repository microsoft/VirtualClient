// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The Performance Counter Monitor for Virtual Client
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class NvidiaSmiMonitor : VirtualClientIntervalBasedMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NvidiaSmiMonitor"/> class.
        /// </summary>
        public NvidiaSmiMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            return Task.Run(async () =>
            {
                try
                {
                    if (this.Platform == PlatformID.Unix)
                    {
                        // Check that nvidia-smi is installed. If not, we exit the monitor.
                        bool toolsetInstalled = await this.VerifyToolsetInstalledAsync(telemetryContext, cancellationToken);

                        if (toolsetInstalled)
                        {
                            await this.WaitAsync(this.MonitorWarmupPeriod, cancellationToken);

                            int iterations = 0;
                            while (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    iterations++;
                                    if (this.IsIterationComplete(iterations))
                                    {
                                        break;
                                    }

                                    await this.QueryC2CAsync(telemetryContext, cancellationToken);
                                    await this.QueryGpuAsync(telemetryContext, cancellationToken);
                                    await this.WaitAsync(this.MonitorFrequency, cancellationToken);
                                }
                                catch (Exception exc)
                                {
                                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                                }
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Do not allow the monitor to crash the application.
                    this.Logger.LogErrorMessage(exc, telemetryContext);
                }
            });
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();

            if (this.MonitorFrequency <= TimeSpan.Zero)
            {
                throw new MonitorException(
                    $"The monitor frequency defined/provided for the '{this.TypeName}' component '{this.MonitorFrequency}' is not valid. " +
                    $"The frequency must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task QueryC2CAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "nvidia-smi";
            string commandArguments = "c2c -s";

            try
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Nvidia_SMI_GPU_Links");
                        process.ThrowIfMonitorFailed();

                        if (process.StandardOutput.Length > 0)
                        {
                            string results = process.StandardOutput.ToString();
                            IList<Metric> metrics = NvidiaSmiResultsParser.ParseC2CResults(results);

                            if (metrics?.Any() == true)
                            {
                                this.Logger.LogMetrics(
                                    "nvidia-smi",
                                    "Nvidia GPU Links",
                                    process.StartTime,
                                    process.ExitTime,
                                    metrics,
                                    "GPU",
                                    $"{command} {commandArguments}",
                                    null,
                                    telemetryContext,
                                    toolResults: results);
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected whenever ctrl-C is used.
            }
            catch (Exception exc)
            {
                // This would be expected on new VM while nvidia-smi is still being installed.
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }
        }
       
        private async Task QueryGpuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // This is the Nvidia smi query gpu command
            // e.g.
            // nvidia-smi --query-gpu=--query-gpu=timestamp,name,pci.bus_id,driver_version,pstate,pcie.link.gen.max,pcie.link.gen.current,utilization.gpu,utilization.memory,temperature.gpu,temperature.memory,
            // power.draw.average,clocks.gr,clocks.sm,clocks.video,clocks.mem,memory.total,memory.free,memory.used,power.draw.instant,pcie.link.gen.gpucurrent,
            // pcie.link.width.current,ecc.errors.corrected.volatile.device_memory,ecc.errors.corrected.volatile.dram,ecc.errors.corrected.volatile.sram,
            // ecc.errors.corrected.volatile.total,ecc.errors.corrected.aggregate.device_memory,ecc.errors.corrected.aggregate.dram,ecc.errors.corrected.aggregate.sram,
            // ecc.errors.corrected.aggregate.total,ecc.errors.uncorrected.volatile.device_memory,ecc.errors.uncorrected.volatile.dram,ecc.errors.uncorrected.volatile.sram,
            // ecc.errors.uncorrected.volatile.total,ecc.errors.uncorrected.aggregate.device_memory,ecc.errors.uncorrected.aggregate.dram,ecc.errors.uncorrected.aggregate.sram,
            // ecc.errors.uncorrected.aggregate.total
            // --format=csv,nounits

            string command = "nvidia-smi";
            string commandArguments = "--query-gpu=timestamp,index,name,pci.bus_id,driver_version,pstate,pcie.link.gen.max,pcie.link.gen.current,utilization.gpu,utilization.memory,temperature.gpu,temperature.memory," +
                "power.draw.average,clocks.gr,clocks.sm,clocks.video,clocks.mem,memory.total,memory.free,memory.used,power.draw.instant,pcie.link.gen.gpucurrent," +
                "pcie.link.width.current,ecc.errors.corrected.volatile.device_memory,ecc.errors.corrected.volatile.dram,ecc.errors.corrected.volatile.sram," +
                "ecc.errors.corrected.volatile.total,ecc.errors.corrected.aggregate.device_memory,ecc.errors.corrected.aggregate.dram,ecc.errors.corrected.aggregate.sram," +
                "ecc.errors.corrected.aggregate.total,ecc.errors.uncorrected.volatile.device_memory,ecc.errors.uncorrected.volatile.dram,ecc.errors.uncorrected.volatile.sram," +
                "ecc.errors.uncorrected.volatile.total,ecc.errors.uncorrected.aggregate.device_memory,ecc.errors.uncorrected.aggregate.dram,ecc.errors.uncorrected.aggregate.sram," +
                "ecc.errors.uncorrected.aggregate.total " + 
                "--format=csv,nounits";

            DateTime nextIteration = DateTime.UtcNow;

            try
            {
                await this.WaitAsync(nextIteration, cancellationToken);

                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, null, telemetryContext, cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // We cannot log the process details here. The output is too large.
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Nvidia_SMI_GPU_Status");
                        process.ThrowIfMonitorFailed();

                        if (process.StandardOutput.Length > 0)
                        {
                            string results = process.StandardOutput.ToString();
                            IList<Metric> metrics = NvidiaSmiResultsParser.ParseQueryResults(results);

                            if (metrics?.Any() == true)
                            {
                                foreach (var metric in metrics)
                                {
                                    metric.Metadata.TryGetValue("gpu_index", out IConvertible index);

                                    this.Logger.LogMetric(
                                        "nvidia-smi",
                                        "Nvidia GPU Status",
                                        process.StartTime,
                                        process.ExitTime,
                                        metric.Name,
                                        metric.Value,
                                        metric.Unit,
                                        $"GPU {index}".Trim(),
                                        $"{command} {commandArguments}",
                                        new List<string> { "Nvidia", "GPU", $"GPU {index}" },
                                        telemetryContext,
                                        description: metric.Description,
                                        relativity: metric.Relativity,
                                        toolResults: results);
                                }
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected whenever ctrl-C is used.
            }
            catch (Exception exc)
            {
                // This would be expected on new VM while nvidia-smi is still being installed.
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }
        }

        private async Task<bool> VerifyToolsetInstalledAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            bool isInstalled = false;
            using (IProcessProxy process = await this.ExecuteCommandAsync("nvidia-smi", "-h", null, telemetryContext, cancellationToken))
            {
                isInstalled = process.ExitCode == 0;
                if (!isInstalled)
                {
                    this.Logger.LogMessage(
                        "The Nvidia SMI toolset (nvidia-smi) is not installed. This monitor will not execute.",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddProcessContext(process));
                }
            }

            return isInstalled;
        }
    }
}