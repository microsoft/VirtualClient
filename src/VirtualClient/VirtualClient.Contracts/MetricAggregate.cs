// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Concurrent;
    using System.Linq;
    using MathNet.Numerics.Statistics;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides features for capturing Windows performance counter values over a period
    /// of time.
    /// </summary>
    public class MetricAggregate : ConcurrentBag<double?>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregate"/> class.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="aggregateType">The type of aggregation to apply to the metric values/samples.</param>
        /// <param name="description">A description of the metric.</param>
        public MetricAggregate(string metricName, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
        {
            metricName.ThrowIfNullOrWhiteSpace(nameof(metricName));

            this.Name = metricName;
            this.Description = description;
            this.AggregateType = aggregateType;
            this.Relativity = MetricRelativity.Undefined;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregate"/> class.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="relativity">The relativity of the metric in comparison with other samples of itself (e.g. higher/lower is better).</param>
        /// <param name="aggregateType">The type of aggregation to apply to the metric values/samples.</param>
        /// <param name="description">A description of the metric.</param>
        public MetricAggregate(string metricName, MetricRelativity relativity, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
            : this(metricName, aggregateType, description)
        {
            this.Relativity = relativity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregate"/> class.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="metricUnit">The unit of measurement (e.g. KB/sec).</param>
        /// <param name="aggregateType">The type of aggregation to apply to the metric values/samples.</param>
        /// <param name="description">A description of the metric.</param>
        public MetricAggregate(string metricName, string metricUnit, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
            : this(metricName, aggregateType, description)
        {
            this.Unit = metricUnit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricAggregate"/> class.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="metricUnit">The unit of measurement (e.g. KB/sec).</param>
        /// <param name="relativity">The relativity of the metric in comparison with other samples of itself (e.g. higher/lower is better).</param>
        /// <param name="aggregateType">The type of aggregation to apply to the metric values/samples.</param>
        /// <param name="description">A description of the metric.</param>
        public MetricAggregate(string metricName, string metricUnit, MetricRelativity relativity, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
            : this(metricName, aggregateType, description)
        {
            this.Unit = metricUnit;
            this.Relativity = relativity;
        }

        /// <summary>
        /// Description of the metric.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The name of the metric.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The unit of measurement of the metric.
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// The capture strategy to use over time while capturing performance values.
        /// </summary>
        public MetricAggregateType AggregateType { get; set; }

        /// <summary>
        /// Defines how the metric value relates to the preferred outcome 
        /// (e.g. higher/lower is better).
        /// </summary>
        public MetricRelativity Relativity { get; set; }

        /// <summary>
        /// Creates a metric from the aggregate of values/samples.
        /// </summary>
        public Metric ToMetric()
        {
            return this.ToMetric(this.AggregateType);
        }

        /// <summary>
        /// Creates a metric from the aggregate of values/samples.
        /// </summary>
        public Metric ToMetric(MetricAggregateType aggregateType)
        {
            if (!this.Any())
            {
                return Metric.None;
            }

            double? value = 0;
            switch (aggregateType)
            {
                case MetricAggregateType.Average:
                    double? sum = this.Sum();
                    value = sum / this.Count;
                    break;

                case MetricAggregateType.Max:
                    value = this.Max();
                    break;

                case MetricAggregateType.Min:
                    value = this.Min();
                    break;

                case MetricAggregateType.Median:
                    value = Statistics.Median(this);
                    break;

                case MetricAggregateType.Raw:
                    value = this.First(); // a ConcurrentBag adds latest items first.
                    break;

                case MetricAggregateType.Sum:
                    value = this.Sum();
                    break;

                default:
                    throw new MonitorException(
                        $"Unsupported metric aggregate type '{this.AggregateType}' provided.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
            }

            return new Metric(this.Name, value, unit: this.Unit, relativity: this.Relativity, description: this.Description);
        }

        /// <summary>
        /// Returns a string representation of the performance counter.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
