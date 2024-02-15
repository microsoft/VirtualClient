// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    ///  Parser for NAS Parallel Benchmarks results.
    /// </summary>
    public class NASParallelBenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="NASParallelBenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public NASParallelBenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            Regex timeRegex = new Regex($@"Time\s+in\s+seconds\s+=\s+{TextParsingExtensions.DoubleTypeRegex}", RegexOptions.Multiline);
            this.AddMetric(metrics, timeRegex, "ExecutionTime", "Seconds", MetricRelativity.LowerIsBetter);

            Regex mopsTotalRegex = new Regex($@"Mop/s\s+total\s+=\s+{TextParsingExtensions.DoubleTypeRegex}", RegexOptions.Multiline);
            this.AddMetric(metrics, mopsTotalRegex, @"Mop/s total", @"Mop/s", MetricRelativity.HigherIsBetter);

            Regex mopsProcessRegex = new Regex($@"Mop/s/process\s+=\s+{TextParsingExtensions.DoubleTypeRegex}", RegexOptions.Multiline);
            this.AddMetric(metrics, mopsProcessRegex, @"Mop/s/process", @"Mop/s", MetricRelativity.HigherIsBetter);

            Regex mopsThreadRegex = new Regex($@"Mop/s/thread\s+=\s+{TextParsingExtensions.DoubleTypeRegex}", RegexOptions.Multiline);
            this.AddMetric(metrics, mopsThreadRegex, @"Mop/s/thread", @"Mop/s", MetricRelativity.HigherIsBetter);

            return metrics;
        }

        private void AddMetric(List<Metric> metrics, Regex metricRegex, string metricName, string metricUnit = "", MetricRelativity relativity = MetricRelativity.Undefined)
        {
            Match match = Regex.Match(this.PreprocessedText, metricRegex.ToString(), metricRegex.Options);
            if (match.Success)
            {
                metrics.Add(new Metric(metricName, Convert.ToDouble(match.Groups[1].Value), metricUnit, relativity));
            }
        }
    }
}
