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
    public class LinuxPerformanceCounterMonitor : VirtualClientIntervalBasedMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxPerformanceCounterMonitor"/> class.
        /// </summary>
        public LinuxPerformanceCounterMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();
                IFileSystem fileSystem = systemManagement.FileSystem;

                // e.g.
                // atop -j 1 60
                int totalSamples = (int)this.MonitorFrequency.TotalSeconds;
                string command = "atop";
                string commandArguments = $"-j 1 {totalSamples}";

                await Task.Delay(this.MonitorWarmupPeriod, cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, $"{commandArguments}", Environment.CurrentDirectory))
                        {
                            this.CleanupTasks.Add(() => process.SafeKill());

                            DateTime startTime = DateTime.UtcNow;
                            await process.StartAndWaitAsync(cancellationToken);

                            DateTime endTime = DateTime.UtcNow;

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                try
                                {
                                    // Atop returns an exit code of 2 when the process is cancelled. We do not want to treat this as an
                                    // error in the traditional sense.
                                    if (process.ExitCode != 2)
                                    {
                                        process.ThrowIfErrored<MonitorException>(errorReason: ErrorReason.MonitorFailed);

                                        if (process.StandardOutput.Length > 0)
                                        {
                                            AtopParser parser = new AtopParser(process.StandardOutput.ToString(), this.MetricFilters);
                                            IList<Metric> metrics = parser.Parse();

                                            if (metrics?.Any() == true)
                                            {
                                                this.Logger.LogPerformanceCounters("atop", metrics, startTime, endTime, telemetryContext);
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    throw;
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
                        this.Logger.LogMessage($"{this.TypeName}.MetricCaptureError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                    }
                }
            });
        }

        /// <summary>
        /// Return true/false whether the monitor is supported on the platform/system. Supported on
        /// Windows platforms only.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Unix;
            if (isSupported)
            {
                isSupported = base.IsSupported();
            }

            return isSupported;
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
    }
}