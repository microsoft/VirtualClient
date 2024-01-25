// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Apache bench metrics output document.
    /// </summary>
    public class ApacheBenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex ApacheBenchSectionDelimiter = new (@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);
        private static readonly char[] Separator = new[] { '\r', '\n' };

        /// <summary>
        /// Constructor for <see cref="ApacheBenchMetricsParser"/>
        /// </summary>
        /// <param name="results">Raw text to parse.</param>
        public ApacheBenchMetricsParser(string results)
            : base(results)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var metrics = Enumerable.Empty<Metric>().ToList();

            try
            {
                this.Preprocess();
                Dictionary<string, string> metricsMap = ApacheBenchMetricsParser.GetMetricsMap(this.Sections["Metrics"]);

                metrics = new List<Metric>()
                {
                    new ("Concurrency Level", Convert.ToDouble(metricsMap["Concurrency Level"]), "number", MetricRelativity.Undefined),
                    new ("Total requests", Convert.ToDouble(metricsMap["Total requests"]), "number", MetricRelativity.Undefined),
                    new ("Total time", Convert.ToDouble(metricsMap["Total time"]), MetricUnit.Seconds, MetricRelativity.LowerIsBetter),
                    new ("Total failed requests", Convert.ToDouble(metricsMap["Total failed requests"]), "number", MetricRelativity.LowerIsBetter),
                    new ("Requests", Convert.ToDouble(metricsMap["Requests"]), "number/sec", MetricRelativity.HigherIsBetter),
                    new ("Total time per request", Convert.ToDouble(metricsMap["Total time per request"]), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter),
                    new ("Total data transferred", Convert.ToDouble(metricsMap["Total data transferred"]), MetricUnit.Bytes, MetricRelativity.LowerIsBetter),
                    new ("Data transfer rate", Convert.ToDouble(metricsMap["Data transfer rate"]), MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter)
                };
            }
            catch (JsonException exc)
            {
                throw new WorkloadResultsException(
                    "Workload results parsing failure. The example workload results are not valid or are not formatted in a valid JSON structure.",
                    exc,
                    ErrorReason.WorkloadResultsParsingFailed);
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            RegexOptions options = RegexOptions.None;
            var regex = new Regex("[ ]{2,}", options);
            this.PreprocessedText = regex.Replace(this.RawText, " ");
            this.PreprocessedText = this.PreprocessedText.Replace("Concurrency Level", "Metrics\r\nConcurrency Level");
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, ApacheBenchSectionDelimiter);

            if (!this.Sections.ContainsKey("Metrics") || string.IsNullOrWhiteSpace(this.Sections["Metrics"]))
            {
                throw new WorkloadException($"Benchmarking metrics are not present", ErrorReason.WorkloadResultsParsingFailed);
            }

            this.Sections["Metrics"] = this.Sections["Metrics"]
                .Replace("Complete requests", "Total requests")
                .Replace("Time taken for tests", "Total time")
                .Replace("Failed requests", "Total failed requests")
                .Replace("Requests per second", "Requests")
                .Replace("Time per request", "Total time per request")
                .Replace("Total transferred", "Total data transferred")
                .Replace("Transfer rate", "Data transfer rate");
        }

        private static Dictionary<string, string> GetMetricsMap(string rawText)
        {
            var metricsMap = new Dictionary<string, string>();
            string[] lines = rawText.Split(ApacheBenchMetricsParser.Separator, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts = line.Split(':');

                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim().Split(" ")[0];
                    metricsMap[key] = value;
                }
            }

            return metricsMap;
        }
    }
}
