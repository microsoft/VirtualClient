// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides features for capturing Windows performance counter values over a period
    /// of time.
    /// </summary>
    public class WindowsPerformanceCounter : IPerformanceMetric, IDisposable
    {
        private PerformanceCounter counter;
        private ConcurrentBag<float> counterValues;
        private SemaphoreSlim semaphore;
        private DateTime nextCounterVerificationTime;
        private bool disposed;

        /// <summary>
        /// Intialize a new instance of the <see cref="WindowsPerformanceCounter"/> class.
        /// </summary>
        /// <param name="counterCategory">The performance counter category.</param>
        /// <param name="counterName">The performance counter name.</param>
        /// <param name="captureStrategy">The capture strategy to use over time while capturing performance values.</param>
        public WindowsPerformanceCounter(string counterCategory, string counterName, CaptureStrategy captureStrategy)
        {
            counterCategory.ThrowIfNullOrWhiteSpace(nameof(counterCategory));
            counterName.ThrowIfNullOrWhiteSpace(nameof(counterName));

            this.Category = counterCategory;
            this.Name = counterName;
            this.Strategy = captureStrategy;
            this.counterValues = new ConcurrentBag<float>();
            this.MetricName = $"\\{counterCategory}\\{counterName}"; // It is the standard way of writing 
            this.MetricRelativity = MetricRelativity.Undefined;
            this.semaphore = new SemaphoreSlim(1);
            this.nextCounterVerificationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPerformanceCounter"/> class.
        /// </summary>
        /// <param name="counterCategory">The performance counter category.</param>
        /// <param name="counterName">The performance counter name.</param>
        /// <param name="captureStrategy">The capture strategy to use over time while capturing performance values.</param>
        /// <param name="metricRelativity">The Metric Relativity for metrics value.</param>
        public WindowsPerformanceCounter(string counterCategory, string counterName, CaptureStrategy captureStrategy, MetricRelativity metricRelativity)
            : this(counterCategory, counterName, captureStrategy)
        {
            this.MetricRelativity = metricRelativity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPerformanceCounter"/> class.
        /// </summary>
        /// <param name="counterCategory">The performance counter category.</param>
        /// <param name="counterName">The performance counter name.</param>
        /// <param name="instanceName">The performance counter instance.</param>
        /// <param name="captureStrategy">The capture strategy to use over time while capturing performance values.</param>
        public WindowsPerformanceCounter(string counterCategory, string counterName, string instanceName, CaptureStrategy captureStrategy)
            : this(counterCategory, counterName, captureStrategy)
        {
            this.InstanceName = instanceName;
            this.MetricName = $"\\{counterCategory}({instanceName})\\{counterName}";
        }

        /// <summary>
        /// Intialize a new instance of the <see cref="WindowsPerformanceCounter"/> class.
        /// </summary>
        /// <param name="counterCategory">The performance counter category.</param>
        /// <param name="counterName">The performance counter name.</param>
        /// <param name="instanceName">The performance counter instance.</param>
        /// <param name="captureStrategy">The capture strategy to use over time while capturing performance values.</param>
        /// <param name="metricRelativity">The Metric Relativity for metrics value.</param>
        public WindowsPerformanceCounter(string counterCategory, string counterName, string instanceName, CaptureStrategy captureStrategy, MetricRelativity metricRelativity)
            : this(counterCategory, counterName, instanceName, captureStrategy)
        {
            this.MetricRelativity = metricRelativity;
        }

        /// <summary>
        /// The category of the performance counters (e.g. Processor).
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string MetricName { get; set; }

        /// <summary>
        /// The unit of measurement of the metric.
        /// </summary>
        public string MetricUnit { get; set; }

        /// <summary>
        /// Metric Relativity to check whether higher or lower is better.
        /// </summary>
        public MetricRelativity MetricRelativity { get; set; }

        /// <summary>
        /// The name of the performance counter (e.g. % Processor Time).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Name of the the performance counter instance (e.g. _Total).
        /// </summary>
        public string InstanceName { get; }

        /// <summary>
        /// The capture strategy to use over time while capturing performance values.
        /// </summary>
        public CaptureStrategy Strategy { get; }

        /// <summary>
        /// The set of counter values that have been captured during the current
        /// interval.
        /// </summary>
        protected IEnumerable<float> CounterValues
        {
            get
            {
                return this.counterValues;
            }
        }

        /// <inheritdoc />
        public void Capture()
        {
            try
            {
                this.semaphore.Wait(CancellationToken.None);
                if (this.TryGetCounterValue(out float? counterValue))
                {
                    this.counterValues.Add(counterValue.Value);
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Reset()
        {
            try
            {
                this.semaphore.Wait(CancellationToken.None);
                this.counterValues.Clear();
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Metric Snapshot()
        {
            try
            {
                this.semaphore.Wait(CancellationToken.None);
                if (!this.counterValues.Any())
                {
                    return Metric.None;
                }

                float value = 0;
                switch (this.Strategy)
                {
                    case CaptureStrategy.Average:
                        float sum = 0;
                        foreach (float captureValue in this.counterValues)
                        {
                            sum += captureValue;
                        }

                        value = sum / this.counterValues.Count;
                        break;

                    case CaptureStrategy.Raw:
                        value = this.counterValues.First(); // a ConcurrentBag adds latest items first.
                        break;

                    default:
                        throw new MonitorException(
                            $"Unsupported performance capture strategy '{this.Strategy}' provided.",
                            ErrorReason.MonitorUnexpectedAnomaly);
                }

                this.counterValues.Clear();
                return new Metric(this.MetricName, value, null, this.MetricRelativity);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        /// <summary>
        /// Returns a string representation of the performance counter.
        /// </summary>
        public override string ToString()
        {
            return this.MetricName;
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.counter?.Dispose();
                    this.semaphore.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Returns the current value for the performance counter.
        /// </summary>
        protected virtual bool TryGetCounterValue(out float? counterValue)
        {
            counterValue = null;
            if (this.counter == null)
            {
                this.Initialize();
            }

            if (this.counter != null)
            {
                counterValue = this.counter.NextValue();
            }

            return counterValue != null;
        }

        private void Initialize()
        {
            if (DateTime.UtcNow >= this.nextCounterVerificationTime)
            {
                // We allow time for certain counters to become available before we check for them
                // again. We don't want to throttle the system at the same time as we want to start capturing
                // the counter values as soon as is possible.
                if (!PerformanceCounterCategory.Exists(this.Category))
                {
                    this.nextCounterVerificationTime = DateTime.UtcNow.AddMinutes(2);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(this.InstanceName) && !PerformanceCounterCategory.InstanceExists(this.InstanceName, this.Category))
                {
                    this.nextCounterVerificationTime = DateTime.UtcNow.AddMinutes(2);
                    return;
                }

                this.counter = string.IsNullOrWhiteSpace(this.InstanceName)
                    ? new PerformanceCounter(this.Category, this.Name, readOnly: true)
                    : new PerformanceCounter(this.Category, this.Name, this.InstanceName, readOnly: true);
            }
        }
    }
}
