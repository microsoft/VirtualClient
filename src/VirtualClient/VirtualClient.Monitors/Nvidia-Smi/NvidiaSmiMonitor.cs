// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
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

    /// <summary>
    /// The Performance Counter Monitor for Virtual Client
    /// </summary>
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
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    // This is not supported on Windows, skipping
                    break;

                case PlatformID.Unix:
                    await this.QueryC2CAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    await this.QueryGpuAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    break;
            }
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
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            // This is the Nvidia smi c2c command
            string command = "nvidia-smi";
            string c2cCommandArguments = "c2c -s";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            try
            {
                DateTime startTime = DateTime.UtcNow;
                IProcessProxy process = await this.ExecuteCommandAsync(command, c2cCommandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true);
                DateTime endTime = DateTime.UtcNow;

                if (!cancellationToken.IsCancellationRequested)
                {
                    // We cannot log the process details here. The output is too large.
                    await this.LogProcessDetailsAsync(process, telemetryContext, "Nvidia-Smi-c2c", logToFile: true);
                    process.ThrowIfErrored<MonitorException>(errorReason: ErrorReason.MonitorFailed);

                    if (process.StandardOutput.Length > 0)
                    {
                        NvidiaSmiC2CParser parser = new NvidiaSmiC2CParser(process.StandardOutput.ToString());
                        IList<Metric> metrics = parser.Parse();

                        if (metrics?.Any() == true)
                        {
                            this.Logger.LogPerformanceCounters("nvidia", metrics, startTime, endTime, telemetryContext);
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
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

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
            int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
            string command = "nvidia-smi";
            string commandArguments = "--query-gpu=timestamp,name,pci.bus_id,driver_version,pstate,pcie.link.gen.max,pcie.link.gen.current,utilization.gpu,utilization.memory,temperature.gpu,temperature.memory," +
                "power.draw.average,clocks.gr,clocks.sm,clocks.video,clocks.mem,memory.total,memory.free,memory.used,power.draw.instant,pcie.link.gen.gpucurrent," +
                "pcie.link.width.current,ecc.errors.corrected.volatile.device_memory,ecc.errors.corrected.volatile.dram,ecc.errors.corrected.volatile.sram," +
                "ecc.errors.corrected.volatile.total,ecc.errors.corrected.aggregate.device_memory,ecc.errors.corrected.aggregate.dram,ecc.errors.corrected.aggregate.sram," +
                "ecc.errors.corrected.aggregate.total,ecc.errors.uncorrected.volatile.device_memory,ecc.errors.uncorrected.volatile.dram,ecc.errors.uncorrected.volatile.sram," +
                "ecc.errors.uncorrected.volatile.total,ecc.errors.uncorrected.aggregate.device_memory,ecc.errors.uncorrected.aggregate.dram,ecc.errors.uncorrected.aggregate.sram," +
                "ecc.errors.uncorrected.aggregate.total " + 
                "--format=csv,nounits";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            DateTime nextIteration = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await this.WaitAsync(nextIteration, cancellationToken);
                    nextIteration = DateTime.UtcNow.Add(this.MonitorFrequency);

                    using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, $"{commandArguments}", Environment.CurrentDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        DateTime startTime = DateTime.UtcNow;
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        DateTime endTime = DateTime.UtcNow;

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // We cannot log the process details here. The output is too large.
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Nvidia-Smi-gpu", logToFile: true);
                            process.ThrowIfErrored<MonitorException>(errorReason: ErrorReason.MonitorFailed);

                            if (process.StandardOutput.Length > 0)
                            {
                                NvidiaSmiQueryGpuParser parser = new NvidiaSmiQueryGpuParser(process.StandardOutput.ToString());
                                IList<Metric> metrics = parser.Parse();

                                if (metrics?.Any() == true)
                                {
                                    this.Logger.LogPerformanceCounters("nvidia", metrics, startTime, endTime, telemetryContext);
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
        }
    }
}