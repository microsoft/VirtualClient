// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    ///  Parser for Graph500 output document.
    /// </summary>
    public class Graph500MetricsParser : MetricsParser
    {
        private static readonly Regex ColumnRegex = new Regex(@$":", RegexOptions.ExplicitCapture);

        private static readonly string TraversedEdgesPerSecondUnit = "TEPS";
        private static readonly string TimeUnit = "seconds";

        /// <summary>
        /// Constructor for <see cref="Graph500MetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public Graph500MetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            List<string> lines = this.PreprocessedText.Split(Environment.NewLine).ToList();
            foreach (string line in lines)
            {
                if (line != string.Empty)
                {
                    string[] nameAndValue = Regex.Split(line, ColumnRegex.ToString(), ColumnRegex.Options);
                    string metricName = nameAndValue[0].Trim();
                    double metricValue;
                    double.TryParse(nameAndValue[1].Trim(), NumberStyles.Float, null, out metricValue);

                    if (line.Contains("time"))
                    {
                        metrics.Add(new Metric(metricName, metricValue, Graph500MetricsParser.TimeUnit));
                    }
                    else if (line.Contains("TEPS"))
                    {
                        metrics.Add(new Metric(metricName, metricValue, Graph500MetricsParser.TraversedEdgesPerSecondUnit));
                    }
                    else
                    {
                        metrics.Add(new Metric(metricName, metricValue));
                    }
                }
            }
            
            metrics.Where(m => m.Name == "NBFS").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "SCALE").FirstOrDefault().Relativity = MetricRelativity.Undefined;
            metrics.Where(m => m.Name == "bfs  firstquartile_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  firstquartile_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  firstquartile_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  harmonic_mean_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  harmonic_stddev_TEPS").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  max_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  max_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  max_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  mean_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  mean_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  median_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  median_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  median_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  min_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  min_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  min_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  stddev_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  stddev_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  thirdquartile_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "bfs  thirdquartile_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "bfs  thirdquartile_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "construction_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "edgefactor").FirstOrDefault().Relativity = MetricRelativity.Undefined;
            metrics.Where(m => m.Name == "firstquartile_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "graph_generation").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "max_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "mean_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "median_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "min_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "num_mpi_processes").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp firstquartile_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp firstquartile_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp firstquartile_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp harmonic_mean_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp harmonic_stddev_TEPS").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp max_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp max_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp max_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp mean_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp mean_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp median_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp median_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp median_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp min_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp min_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp min_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp stddev_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp stddev_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp thirdquartile_TEPS").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "sssp thirdquartile_time").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "sssp thirdquartile_validate").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "stddev_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            metrics.Where(m => m.Name == "thirdquartile_nedge").FirstOrDefault().Relativity = MetricRelativity.LowerIsBetter;
            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[\r\n|\n]+", $"{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, $"!", $"{string.Empty}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, $"-nan", $"{string.Empty}");
        }

        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null)
            {
                throw new SchemaException("Graph500 workload didn't generate results because of insufficient memory for running the workload");
            }
        }
    }
}
