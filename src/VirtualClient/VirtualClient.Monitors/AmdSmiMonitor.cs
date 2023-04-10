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
    public class AmdSmiMonitor : VirtualClientIntervalBasedMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmdSmiMonitor"/> class.
        /// </summary>
        public AmdSmiMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    await this.QueryGpuAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                    break;

                case PlatformID.Unix:
                    // not supported at the moment
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

        /// <summary>
        /// Query the gpu for utilization information
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task QueryGpuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
            IFileSystem fileSystem = systemManagement.FileSystem;

            int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
            string command = "amdsmi";
            string commandArguments = "metric --csv";

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
                                process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.MonitorFailed);

                                if (process.StandardOutput.Length > 0)
                                {
                                    AmdSmiQueryGpuParser parser = new AmdSmiQueryGpuParser(process.StandardOutput.ToString());
                                    IList<Metric> metrics = parser.Parse();

                                    if (metrics?.Any() == true)
                                    {
                                        this.Logger.LogPerformanceCounters("amd", metrics, startTime, endTime, telemetryContext);
                                    }
                                }
                            }
                            catch
                            {
                                this.Logger.LogProcessDetails<AmdSmiMonitor>(process, EventContext.Persisted());
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
    }
}