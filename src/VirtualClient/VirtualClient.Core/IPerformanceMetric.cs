using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// Represents a performance metric that is tracked throughout a period
    /// of time.
    /// </summary>
    public interface IPerformanceMetric
    {
        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string MetricName { get; }

        /// <summary>
        /// The unit of measurement of the metric.
        /// </summary>
        public string MetricUnit { get; }

        /// <summary>
        /// Captures the current metric value.
        /// </summary>
        void Capture();

        /// <summary>
        /// Resets the performance metric tracker.
        /// </summary>
        void Reset();

        /// <summary>
        /// Returns a snapshot of the metric value.
        /// </summary>
        Metric Snapshot();
    }
}
