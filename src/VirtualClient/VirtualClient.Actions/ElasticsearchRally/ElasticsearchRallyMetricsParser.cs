// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient;
    using VirtualClient.Contracts;

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
        /// Allows the user to describe different levels of priority/verbosity to a set of metrics that can 
        /// be used for queries/filtering. Lower values indicate higher priority. For example, metrics considered 
        /// to be the most critical for decision making would be set with verbosity = 1 (Critical).
        /// </summary>
        public int MetricsVerbosity
        {
            get
            {
                if (this.Metadata.TryGetValue(nameof(this.MetricsVerbosity), out IConvertible verbosityValue) && int.TryParse(verbosityValue.ToString(), out int verbosity))
                {
                    return verbosity;
                }

                return 1;
            }
        }

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
            return Read(this.ReportLines, this.Metadata, this.RallyCollectAllMetrics, this.MetricsVerbosity);
        }

        private static IList<Metric> Read(
            string[] reportLines,
            IDictionary<string, IConvertible> metadata,
            bool collectAllMetrics,
            int metricsVerbosity)
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

                MetricRelativity relativity = MetricRelativity.Undefined;

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
                    metadata: metadata);

                if (!CheckVerbosity(metric, metricsVerbosity) && !collectAllMetrics)
                {
                    continue;
                }

                metrics.Add(metric);
            }

            return metrics;
        }

        /// <summary>
        /// Verbosity levels define a convention for organizing metrics by importance:
        /// - 1 (Standard/Critical): Most important metrics for decision making - bandwidth, throughput, IOPS, key latency percentiles (p50, p99)
        /// - 2 (Detailed): Additional detailed metrics - supplementary percentiles (p70, p90, p95, p99.9)
        /// - 3 (Reserved): Reserved for future expansion
        /// - 4 (Reserved): Reserved for future expansion
        /// - 5 (Verbose): All diagnostic/internal metrics - histogram buckets, standard deviations, byte counts, I/O counts
        /// https://github.com/microsoft/VirtualClient/blob/f1d5410ac2c1cd1acfa6a0901af79cbef1abe9df/src/VirtualClient/VirtualClient.Contracts/Metric.cs#L163
        /// </summary>
        /// <param name="metric"></param>
        /// <param name="metricsVerbosity"></param>
        /// <returns></returns>
        private static bool CheckVerbosity(Metric metric, int metricsVerbosity)
        {
            if (metric.Name.EndsWith("P100") ||
                metric.Name.EndsWith("Mean") || metric.Name.EndsWith("Median") || (metric.Name.Contains("latency") || metric.Name.Contains("throughput")))
            {
                metric.Verbosity = 1; // Critical
            }
            else if (metric.Name.EndsWith("P50") || metric.Name.EndsWith("P90") || metric.Name.EndsWith("P99"))
            {
                metric.Verbosity = 2; // Detailed
            }
            else
            {
                metric.Verbosity = 5; // Verbose
            }
            
            return metric.Verbosity <= metricsVerbosity;
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
            if (SwapPrefix(metricName, "median", "Median", out string newMetricName))
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
            public const string P50 = "50th";
            public const string P90 = "90th";
            public const string P99 = "99th";
            public const string P100 = "100th";
            public const string Rate = "rate";
            public const string Throughput = "throughput";
        }
    }
}
