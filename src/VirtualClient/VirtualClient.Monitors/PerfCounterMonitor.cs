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
    public class PerfCounterMonitor : VirtualClientIntervalBasedMonitor
    {
        private PerformanceTracker performanceTracker;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerfCounterMonitor"/> class.
        /// </summary>
        public PerfCounterMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Returns the list of default performance counters to capture from the system.
        /// </summary>
        protected static IEnumerable<WindowsPerformanceCounter> GetWindowsDefaultCounters()
        {
            return new List<WindowsPerformanceCounter>
            {
                // CPU/Processor Counters
                new WindowsPerformanceCounter("Processor", "% Idle Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Processor", "% Interrupt Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Processor", "Interrupts/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Processor", "% User Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Processor", "% Privileged Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Processor", "% Processor Time", "_Total", CaptureStrategy.Average),

                // Disk/IO Counters
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk Queue Length", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk Read Queue Length", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk Write Queue Length", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk sec/Transfer", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "% Disk Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "% Disk Read Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "% Disk Write Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "% Idle Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Bytes/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Reads/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Transfers/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("PhysicalDisk", "Disk Writes/sec", "_Total", CaptureStrategy.Average),

                // System Memory Counters
                new WindowsPerformanceCounter("Memory", "Available Bytes", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Cache Bytes", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Committed Bytes", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Cache Faults/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Page Faults/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Demand Zero Faults/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Transition Faults/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Page Reads/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Page Writes/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Pages/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Pages Input/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Memory", "Pages Output/sec", CaptureStrategy.Average),

                // Network/IO Counters
                new WindowsPerformanceCounter("IPv4", "Datagrams/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("IPv4", "Datagrams Received/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("IPv4", "Datagrams Sent/sec", CaptureStrategy.Average),

                // System Counters
                new WindowsPerformanceCounter("System", "Context Switches/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("System", "System Calls/sec", CaptureStrategy.Average),
                new WindowsPerformanceCounter("System", "Processes", CaptureStrategy.Average),
                new WindowsPerformanceCounter("System", "Threads", CaptureStrategy.Average),

                // Hyper-V counters
                // These will typically be available only on a host/node. The counter framework handles cases
                // where they do not exist.
                new WindowsPerformanceCounter("Hyper-V Hypervisor Logical Processor", "% Total Run Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Hyper-V Hypervisor Logical Processor", "% Hypervisor Run Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Hyper-V Hypervisor Logical Processor", "Hypervisor Microarchitectural Buffer Flushes/sec", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Hyper-V Hypervisor Root Virtual Processor", "% Total Run Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Hyper-V Hypervisor Virtual Processor", "% Total Run Time", "_Total", CaptureStrategy.Average),
                new WindowsPerformanceCounter("Hyper-V Hypervisor Virtual Processor", "% VTL2 Run Time", "_Total", CaptureStrategy.Average)
            };
        }

        /// <summary>
        /// Dispose of resources used by the instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!this.disposed)
                {
                    if (this.performanceTracker != null)
                    {
                        this.performanceTracker.ForEach(metric => (metric as IDisposable)?.Dispose());
                    }
                }

                this.disposed = true;
            }
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    await this.CaptureWindowsCountersAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    break;

                case PlatformID.Unix:
                    await this.CaptureLinuxCountersAsync(telemetryContext, cancellationToken)
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

        private Task CaptureWindowsCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.performanceTracker = new PerformanceTracker(this.Logger);
            this.performanceTracker.AddRange(PerfCounterMonitor.GetWindowsDefaultCounters());
            this.StartTime = DateTime.UtcNow;

            this.performanceTracker.Snapshot += (sender, metrics) =>
            {
                try
                {
                    if (metrics?.Any() == true)
                    {
                        this.EndTime = DateTime.UtcNow;
                        this.Logger.LogPerformanceCounters(".NET SDK", metrics, this.StartTime, this.EndTime, telemetryContext);
                        this.StartTime = DateTime.UtcNow;
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext);
                }
            };

            // We allot a warm-up period before we begin tracking counters to enable
            // workloads to be well underway and utilizing the system to expected capacity.
            return this.performanceTracker.BeginTrackingAsync(
                TimeSpan.FromSeconds(1),
                this.MonitorFrequency,
                this.MonitorWarmupPeriod,
                cancellationToken);
        }

        private async Task CaptureLinuxCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
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
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }
        }
    }
}