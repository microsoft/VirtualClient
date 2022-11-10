// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Memcached Memtier benchmark output document.
    /// </summary>
    public class MemcachedMemtierMetricsParser : MetricsParser
    {
        /// <summary>
        /// To match Totals line of the result.
        /// </summary>
        private const string GetTotalsLine = @"(?<=Totals).*(?=\n)";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedMemtierMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text which is output of the Memcached benchmark</param>
        public MemcachedMemtierMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Total throughput of Memcached Memtier run.
        /// </summary>
        private double TotalThroughput { get; set; }

        /// <summary>
        /// p50 latency of Memcached Memtier run.
        /// </summary>
        private double P50Latency { get; set; }

        /// <summary>
        /// p90 latency of Memcached Memtier run.
        /// </summary>
        private double P90Latency { get; set; }

        /// <summary>
        /// p95 latency of Memcached Memtier run.
        /// </summary>
        private double P95Latency { get; set; }

        /// <summary>
        /// p99 latency of Memcached Memtier run.
        /// </summary>
        private double P99Latency { get; set; }

        /// <summary>
        /// p99.9 latency of Memcached Memtier run.
        /// </summary>
        private double P99_9Latency { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            List<Metric> metrics = new List<Metric>();
            var matches = Regex.Matches(this.RawText, GetTotalsLine);
            for (int i = 0; i < matches.Count; i++)
            {
                var st = Regex.Split(matches.ElementAt(i).Value, SpaceDelimiter);
                this.TotalThroughput += Convert.ToDouble(st[1]);
                metrics.Add(new Metric($"throughput_{i + 1}", Convert.ToDouble(st[1]), MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter));
                this.P50Latency = Math.Max(this.P50Latency, Convert.ToDouble(st[5]));
                this.P90Latency = Math.Max(this.P90Latency, Convert.ToDouble(st[6]));
                this.P95Latency = Math.Max(this.P95Latency, Convert.ToDouble(st[7]));
                this.P99Latency = Math.Max(this.P99Latency, Convert.ToDouble(st[8]));
                this.P99_9Latency = Math.Max(this.P99_9Latency, Convert.ToDouble(st[9]));
            }

            metrics.Add(new Metric($"throughput", Math.Round(this.TotalThroughput, 5), MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter));
            metrics.Add(new Metric($"p50lat", this.P50Latency, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
            metrics.Add(new Metric($"p90lat", this.P90Latency, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
            metrics.Add(new Metric($"p95lat", this.P95Latency, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
            metrics.Add(new Metric($"p99lat", this.P99Latency, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
            metrics.Add(new Metric($"p99.9lat", this.P99_9Latency, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null || !this.RawText.Contains("Totals"))
            {
                throw new SchemaException("Invalid Memcached Memtier results format.");
            }
        }
    }
}
