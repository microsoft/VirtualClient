﻿// Copyright (c) Microsoft Corporation.
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
    public class SysbenchOLTPMetricsParser : MetricsParser
    {
        private const string TransactionsPerSecond = "transactions/sec";
        private const string QueriesPerSecond = "queries/sec";
        private const string IgnoredErrorsPerSecond = "ignored errors/sec";
        private const string ReconnectsPerSecond = "reconnects/sec";
        private const string Second = "seconds";
        private const string MilliSecond = "milliseconds";

        /// <summary>
        /// Constructor for <see cref="SysbenchOLTPMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SysbenchOLTPMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();

            // Create list of Metrics Info
            List<MetricInfo> metricInfoList = new List<MetricInfo>()
            {
                new MetricInfo("# read queries", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("# write queries", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("# other queries", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("# transactions", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("transactions/sec", TransactionsPerSecond, MetricRelativity.HigherIsBetter),
                new MetricInfo("# queries", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("queries/sec", QueriesPerSecond, MetricRelativity.HigherIsBetter),
                new MetricInfo("# ignored errors", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("ignored errors/sec", IgnoredErrorsPerSecond, MetricRelativity.HigherIsBetter),
                new MetricInfo("# reconnects", string.Empty, MetricRelativity.HigherIsBetter),
                new MetricInfo("reconnects/sec", ReconnectsPerSecond, MetricRelativity.HigherIsBetter),
                new MetricInfo("elapsed time", Second, MetricRelativity.LowerIsBetter),
                new MetricInfo("latency min", MilliSecond, MetricRelativity.LowerIsBetter),
                new MetricInfo("latency avg", MilliSecond, MetricRelativity.LowerIsBetter),
                new MetricInfo("latency max", MilliSecond, MetricRelativity.LowerIsBetter),
                new MetricInfo("latency p95", MilliSecond, MetricRelativity.LowerIsBetter),
                new MetricInfo("latency sum", MilliSecond, MetricRelativity.LowerIsBetter),
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
                        metrics.Add(new Metric($"{metricInfo.Name}", Convert.ToDouble(m.Value), metricInfo.Unit, metricInfo.Relativity));
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
            public MetricInfo(string name, string unit, MetricRelativity relativity)
            {
                this.Name = name;
                this.Unit = unit;
                this.Relativity = relativity;
            }

            public string Name { get; set; }

            public string Unit { get; set; }

            public MetricRelativity Relativity { get; set; }
        }
    }
}
