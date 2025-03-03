// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for SQLOLTPSysbench output document
    /// </summary>
    public class SysbenchMetricsParser : MetricsParser
    {
        private const string TransactionsPerSecond = "transactions/sec";
        private const string QueriesPerSecond = "queries/sec";
        private const string IgnoredErrorsPerSecond = "ignored errors/sec";
        private const string ReconnectsPerSecond = "reconnects/sec";
        private const string Second = "seconds";
        private const string MilliSecond = "milliseconds";
        private Dictionary<string, IConvertible> metricMetadata = new Dictionary<string, IConvertible>();

        /// <summary>
        /// Constructor for <see cref="SysbenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SysbenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var match = Regex.Match(this.RawText, @"sysbench\s+([\d\.]+-[\w\d]+)");
            string sysbenchversion = match.Success ? match.Groups[1].Value : string.Empty;
            this.metricMetadata["sysbench_version"] = sysbenchversion;
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();

            // Create list of Metrics Info
            List<MetricInfo> metricInfoList = new List<MetricInfo>()
            {
                new MetricInfo("# read queries", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# write queries", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# other queries", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# transactions", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("transactions/sec", TransactionsPerSecond, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# queries", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("queries/sec", QueriesPerSecond, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# ignored errors", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("ignored errors/sec", IgnoredErrorsPerSecond, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("# reconnects", string.Empty, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("reconnects/sec", ReconnectsPerSecond, MetricRelativity.HigherIsBetter, metadata: this.metricMetadata),
                new MetricInfo("elapsed time", Second, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
                new MetricInfo("latency min", MilliSecond, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
                new MetricInfo("latency avg", MilliSecond, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
                new MetricInfo("latency max", MilliSecond, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
                new MetricInfo("latency p95", MilliSecond, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
                new MetricInfo("latency sum", MilliSecond, MetricRelativity.LowerIsBetter, metadata: this.metricMetadata),
            };

            if (!string.IsNullOrEmpty(this.PreprocessedText))
            {
                // Get all ints and decimals
                MatchCollection mc = Regex.Matches(this.PreprocessedText, "-?\\d+(\\.\\d+)?");

                // list of indices to skip in MatchCollection MC (Total Queries, Events/s, Total Number of Events, Thread Fairness averages and stddevs, and 95)
                List<int> dropIndices = new List<int>()
                {
                    3,
                    12,
                    14,
                    18,
                    21,
                    22,
                    23,
                    24
                };

                int mcIndex = 0;
                int metricInfoIndex = 0;

                while (metricInfoIndex < 17)
                {
                    if (!dropIndices.Contains(mcIndex))
                    {
                        MetricInfo metricInfo = metricInfoList[metricInfoIndex];
                        Match m = mc[mcIndex];
                        metrics.Add(new Metric($"{metricInfo.Name}", Convert.ToDouble(m.Value), metricInfo.Unit, metricInfo.Relativity, metadata: metricInfo.Metadata));
                        metricInfoIndex++;
                    }

                    mcIndex++;
                }
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            const string MetricsStart = "SQL statistics:";
            // Split Standard Output to only consider SQL statistics
            Match match = Regex.Match(this.RawText, MetricsStart);
            this.PreprocessedText = match.Success ? Regex.Split(this.RawText, MetricsStart)[1] : string.Empty;
        }

        // helper class that contains Metric Name, Unit, and Relativity
        private class MetricInfo
        {
            public MetricInfo(string name, string unit, MetricRelativity relativity, Dictionary<string, IConvertible> metadata)
            {
                this.Name = name;
                this.Unit = unit;
                this.Relativity = relativity;
                this.Metadata = metadata;
            }

            public string Name { get; set; }

            public string Unit { get; set; }

            public MetricRelativity Relativity { get; set; }

            public Dictionary<string, IConvertible> Metadata { get; set; }
        }
    }
}
