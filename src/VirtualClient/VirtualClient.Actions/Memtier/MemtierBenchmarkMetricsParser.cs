// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Redis Memtier benchmark output.
    /// </summary>
    public class MemtierBenchmarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// To match Totals line of the result.
        /// </summary>
        private const string GetTotalsLine = @"(?<=Totals).*(?=\n)";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SplitAtSpace = @"\s{1,}";

        private List<Metric> metrics = new List<Metric>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MemtierBenchmarkMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text which is output of the Memcached benchmark</param>
        public MemtierBenchmarkMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Total throughput of Redis Memtier for all copies of redis server.
        /// </summary>
        private double TotalThroughput { get; set; }

        /// <summary>
        /// p50 latency of Redis Memtier for all copies of redis server.
        /// </summary>
        private double P50Latency { get; set; }

        /// <summary>
        /// p90 latency of Redis Memtier for all copies of redis server.
        /// </summary>
        private double P90Latency { get; set; }

        /// <summary>
        /// p95 latency of Redis Memtier for all copies of redis server.
        /// </summary>
        private double P95Latency { get; set; }

        /// <summary>
        /// p99 latency of Redis Memtier for all copies of redis server.
        /// </summary>
        private double P99Latency { get; set; }

        /// <summary>
        /// p99.9 latency of Redis Memtier for all copies of redis server.
        /// </summary>
        private double P99_9Latency { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            var matches = Regex.Matches(this.RawText, GetTotalsLine);
            for (int i = 0; i < matches.Count; i++)
            {
                var st = Regex.Split(matches.ElementAt(i).Value, SplitAtSpace);
                this.TotalThroughput += Convert.ToDouble(st[1]);
                this.metrics.Add(new Metric($"Throughput_{i + 1}", Convert.ToDouble(st[1]), "req/sec", MetricRelativity.HigherIsBetter));
                this.P50Latency = Math.Max(this.P50Latency, Convert.ToDouble(st[5]));
                this.P90Latency = Math.Max(this.P90Latency, Convert.ToDouble(st[6]));
                this.P95Latency = Math.Max(this.P95Latency, Convert.ToDouble(st[7]));
                this.P99Latency = Math.Max(this.P99Latency, Convert.ToDouble(st[8]));
                this.P99_9Latency = Math.Max(this.P99_9Latency, Convert.ToDouble(st[9]));
            }

            this.metrics.Add(new Metric($"Throughput", this.TotalThroughput, "req/sec", MetricRelativity.HigherIsBetter));
            this.metrics.Add(new Metric($"P50lat", this.P50Latency, "msec", MetricRelativity.LowerIsBetter));
            this.metrics.Add(new Metric($"P90lat", this.P90Latency, "msec", MetricRelativity.LowerIsBetter));
            this.metrics.Add(new Metric($"P95lat", this.P95Latency, "msec", MetricRelativity.LowerIsBetter));
            this.metrics.Add(new Metric($"P99lat", this.P99Latency, "msec", MetricRelativity.LowerIsBetter));
            this.metrics.Add(new Metric($"P99_9lat", this.P99_9Latency, "msec", MetricRelativity.LowerIsBetter));

            return this.metrics;
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null || !this.RawText.Contains("Totals"))
            {
                throw new SchemaException($"The Redis Memtier output has incorrect format for parsing. The ouput is: '{this.RawText}'");
            }
        }
    }
}
