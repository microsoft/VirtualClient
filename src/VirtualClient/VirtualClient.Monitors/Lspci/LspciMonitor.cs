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
    public class LspciMonitor : VirtualClientIntervalBasedMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LspciMonitor"/> class.
        /// </summary>
        public LspciMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                    await this.QueryGpuAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    break;
            }
        }

        /// <inheritdoc/>
        protected override void ValidateParameters()
        {
            base.ValidateParameters();
            if (this.MonitorFrequency <= TimeSpan.Zero)
            {
                throw new MonitorException(
                    $"The monitor frequency defined/provided for the '{this.TypeName}' component '{this.MonitorFrequency}' is not valid. " +
                    $"The frequency must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private async Task QueryGpuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            // This is the Nvidia smi query gpu command
            // e.g.
            // nvidia-smi --query-gpu=timestamp,name,pci.bus_id,driver_version,pstate,pcie.link.gen.max,pcie.link.gen.current,temperature.gpu,utilization.gpu,utilization.memory,memory.total,memory.free,memory.used --format=csv,nounits
            int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
            string command = "nvidia-smi";
            string commandArguments = "--query-gpu=timestamp,name,pci.bus_id,driver_version,pstate,pcie.link.gen.max,pcie.link.gen.current,temperature.gpu,utilization.gpu,utilization.memory,memory.total,memory.free,memory.used " +
                "--format=csv,nounits";

            await Task.Delay(this.MonitorWarmupPeriod, cancellationToken)
                .ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, $"{commandArguments}", Environment.CurrentDirectory))
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
                                // this.Logger.LogProcessDetails<LspciMonitor>(process, EventContext.Persisted());
                                process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

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
                            catch
                            {
                                // We cannot log the process details here. The output is too large. We will log on errors
                                // though.
                                this.Logger.LogProcessDetails<LspciMonitor>(process, EventContext.Persisted());
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
                    // This would be expected on new VM while nvidia-smi is still being installed.
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }
    }
}