// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides components for tracking performance metrics of the system.
    /// </summary>
    public class PerformanceTracker : List<IPerformanceMetric>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceTracker"/> class.
        /// </summary>
        /// <param name="logger">The application telemetry logger.</param>
        public PerformanceTracker(ILogger logger)
        {
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Event handler executes each time performance metrics are snapshot.
        /// </summary>
        public event EventHandler<IEnumerable<Metric>> Snapshot;

        /// <summary>
        /// The application telemetry logger.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The interval at which the results of the metrics captured will be snapshot. This is the point
        /// at which for example the metric values will be written to telemetry.
        /// </summary>
        /// <param name="monitorInterval">
        /// The interval at which the metric trackers will capture performance data.
        /// </param>
        /// <param name="snapshotInterval">
        /// The interval at which the results of the metrics captured will be snapshot. This is the point
        /// at which for example the metric values will be written to telemetry.
        /// </param>
        /// <param name="cancellationToken">A token that can be used to cancel the performance tracking.</param>
        /// <returns></returns>
        public Task BeginTrackingAsync(TimeSpan monitorInterval, TimeSpan snapshotInterval, CancellationToken cancellationToken)
        {
            return this.BeginTrackingAsync(monitorInterval, snapshotInterval, TimeSpan.Zero, cancellationToken);
        }

        /// <summary>
        /// The interval at which the results of the metrics captured will be snapshot. This is the point
        /// at which for example the metric values will be written to telemetry.
        /// </summary>
        /// <param name="monitorInterval">
        /// The interval at which the metric trackers will capture performance data.
        /// </param>
        /// <param name="snapshotInterval">
        /// The interval at which the results of the metrics captured will be snapshot. This is the point
        /// at which for example the metric values will be written to telemetry.
        /// </param>
        /// <param name="warmupPeriod">A period of time to wait before capturing performance snapshots.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the performance tracking.</param>
        /// <returns></returns>
        public Task BeginTrackingAsync(TimeSpan monitorInterval, TimeSpan snapshotInterval, TimeSpan warmupPeriod, CancellationToken cancellationToken)
        {
            Task monitoringTask = Task.Run(() =>
            {
                DateTime startTime = DateTime.UtcNow;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Task.Delay(monitorInterval, cancellationToken).GetAwaiter().GetResult();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (DateTime.UtcNow >= startTime.Add(warmupPeriod))
                            {
                                this.ForEach(metric =>
                                {
                                    try
                                    {
                                        metric.Capture();
                                    }
                                    catch (Exception exc)
                                    {
                                        this.Logger.LogMessage($"{nameof(PerformanceTracker)}.MetricCaptureError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
                                    }
                                });
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // This type of exception will happen if the cancellation token is cancelled
                        // in the Task.Delay() call above. We don't want to surface this as an exception
                        // because it is entirely expected.
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogErrorMessage(exc, EventContext.Persisted().AddError(exc));
                    }
                }
            });

            Task snapshotTask = Task.Run(() =>
            {
                DateTime startTime = DateTime.UtcNow;
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        Task.Delay(snapshotInterval, cancellationToken).GetAwaiter().GetResult();
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            if (DateTime.UtcNow >= startTime.Add(warmupPeriod))
                            {
                                List<Metric> performanceSnapshots = new List<Metric>();
                                this.ForEach(metric =>
                                {
                                    try
                                    {
                                        Metric performanceSnapshot = metric.Snapshot();
                                        if (performanceSnapshot != null && performanceSnapshot != Metric.None)
                                        {
                                            performanceSnapshots.Add(performanceSnapshot);
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        this.Logger.LogMessage($"{nameof(PerformanceTracker)}.MetricSnapshotError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
                                    }
                                });

                                this.Snapshot?.Invoke(this, performanceSnapshots);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // This type of exception will happen if the cancellation token is cancelled
                        // in the Task.Delay() call above. We don't want to surface this as an exception
                        // because it is entirely expected.
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogErrorMessage(exc, EventContext.Persisted().AddError(exc));
                    }
                }
            });

            return Task.WhenAny(monitoringTask, snapshotTask);
        }
    }
}
