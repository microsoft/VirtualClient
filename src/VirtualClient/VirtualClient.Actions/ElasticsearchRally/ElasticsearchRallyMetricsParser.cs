// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;

    /// <summary>
    /// Elasticsearch Rally metrics parser that parses the raw text output from Elasticsearch Rally and converts it into a list of <see cref="Metric"/> objects.
    /// </summary>
    public class ElasticsearchRallyMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="ElasticsearchRallyMetricsParser"/>
        /// </summary>
        /// <param name="reportContents">Text to be parsed.</param>
        /// <param name="metadata">Metadata associated with the metrics.</param>
        /// <param name="rallyCollectAllMetrics">Indicates whether to collect all metrics.</param>
        public ElasticsearchRallyMetricsParser(string reportContents, Dictionary<string, IConvertible> metadata, bool rallyCollectAllMetrics)
            : base(reportContents)
        {
            if (string.IsNullOrEmpty(reportContents))
            {
                throw new ArgumentNullException(nameof(reportContents), "Report contents cannot be null.");
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            this.ReportLines = reportContents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in metadata)
            {
                this.Metadata[item.Key] = item.Value;
            }

            this.RallyCollectAllMetrics = rallyCollectAllMetrics;
        }

        /// <summary>
        /// Indicates whether to collect all metrics.
        /// </summary>
        public bool RallyCollectAllMetrics { get; private set; }

        /// <summary>
        /// The lines of the report to be parsed.
        /// </summary>
        public string[] ReportLines { get; }

        /// <summary>
        /// Parses the raw text output from Elasticsearch Rally and converts it into a list of <see cref="Metric"/> objects.
        /// </summary>
        /// <returns>A list of <see cref="Metric"/> objects.</returns>
        public override IList<Metric> Parse()
        {
            return Read(this.ReportLines, this.Metadata, this.RallyCollectAllMetrics);
        }

        private static IList<Metric> Read(
            string[] reportLines,
            IDictionary<string, IConvertible> metadata,
            bool collectAllMetrics)
        {
            if (reportLines == null)
            {
                throw new ArgumentNullException(nameof(reportLines), "Report lines cannot be null.");
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata), "Metadata cannot be null.");
            }

            IList<Metric> metrics = new List<Metric>();

            foreach (string line in reportLines)
            {
                // Metric,Task,Value,Unit
                string[] cols = line.Split(',');

                if (cols.Length != 4 || !double.TryParse(cols[2], out double value))
                {
                    continue;
                }

                string metricName = cols[0].ToLower();
                string taskName = cols[1].ToLower();

                // only relevant metrics will be collected
                if (!collectAllMetrics &&
                    !IsRelevantMetric(metricName, taskName))
                {
                    continue;
                }

                string unit = cols[3];
                bool isTimeUnit = unit == "s" || unit == "ms" || unit == "min";

                if (unit.Length == 0 && metricName.Contains($" {MetricNames.Count}"))
                {
                    unit = MetricNames.Count;
                }

                int verbosity = 1; // 0: Critical, 1: Standard, 2: Informational.
                MetricRelativity relativity = MetricRelativity.Undefined;

                if (
                    metricName.StartsWith(MetricNames.Median) ||
                    metricName.StartsWith(MetricNames.P100))
                {
                    verbosity = 0;
                }

                if (metricName.EndsWith(MetricNames.Throughput))
                {
                    relativity = MetricRelativity.HigherIsBetter;
                }
                else if (
                    metricName.EndsWith(MetricNames.Latency) ||
                    metricName.EndsWith(MetricNames.ServiceTime) ||
                    metricName.EndsWith(MetricNames.Rate))
                {
                    relativity = MetricRelativity.LowerIsBetter;
                }

                if (relativity == MetricRelativity.Undefined)
                {
                    if (isTimeUnit)
                    {
                        relativity = MetricRelativity.LowerIsBetter;
                    }
                }

                metricName = TransformMetricName(metricName);

                if (taskName.Length > 0)
                {
                    metricName = $"{taskName} {metricName}";
                }

                metricName = metricName.Replace(' ', '-');

                Metric metric = new Metric(
                    name: metricName,
                    value: value,
                    unit: unit,
                    relativity: relativity,
                    verbosity: verbosity,
                    metadata: metadata);

                metrics.Add(metric);
            }

            return metrics;
        }

        private static bool SwapPrefix(string metricName, string oldPrefix, string newPrefix, out string newMetricName)
        {
            if (metricName.StartsWith(oldPrefix))
            {
                newMetricName = $"{metricName.Substring(oldPrefix.Length).Trim()} {newPrefix}";
                return true;
            }

            newMetricName = metricName;
            return false;
        }

        private static bool SwapPercentileFormat(string metricName, out string newMetricName)
        {
            // "100th percentile latency" => "latency P100"

            var match = System.Text.RegularExpressions.Regex.Match(metricName, @"^(\d+)th (percentile) (.+)");
            if (match.Success)
            {
                newMetricName = $"{match.Groups[3].Value} P{match.Groups[1].Value}";
                return true;
            }

            newMetricName = metricName;
            return false;
        }

        private static string TransformMetricName(string metricName)
        {
            if (SwapPrefix(metricName, "median", "P50", out string newMetricName))
            {
                return newMetricName;
            }
            else if (SwapPrefix(metricName, "mean", "Mean", out newMetricName))
            {
                return newMetricName;
            }
            else if (SwapPercentileFormat(metricName, out newMetricName))
            {
                return newMetricName;
            }

            return metricName;
        }

        private static bool IsRelevantMetric(string metricName, string taskName)
        {
            if (string.IsNullOrEmpty(taskName))
            {
                // summary metrics
                return
                    new string[]
                    {
                        "median cumulative indexing time across primary shards",
                        "median cumulative merge time across primary shards",
                        "median cumulative refresh time across primary shards",
                        "median cumulative flush time across primary shards",
                        "total young gen gc time",
                        "dataset size",
                        "translog size",
                        "segment count",
                    }.Contains(metricName);
            }
            else
            {
                // task metrics
                return
                    new string[]
                    {
                        "mean throughput",
                        "median throughput",
                        "50th percentile latency",
                        "90th percentile latency",
                        "99th percentile latency",
                        "100th percentile latency",
                        "50th percentile service time",
                        "90th percentile service time",
                        "99th percentile service time",
                        "100th percentile service time",
                        "error rate",
                    }.Contains(metricName);
            }
        }

        private struct MetricNames
        {
            public const string Count = "count";
            public const string Latency = "latency";
            public const string Median = "median";
            public const string ServiceTime = "service time";
            public const string P100 = "100th";
            public const string Rate = "rate";
            public const string Throughput = "throughput";
        }
    }
}
