using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualClient.Contracts;

namespace VirtualClient.Actions.MLPerf
{
    /// <summary>
    /// 
    /// </summary>
    public class MLPerfSummaryMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="MLPerfMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="accuracyMode">Mode for which output file needs to be parsed.</param>
        public MLPerfSummaryMetricsParser(string rawText, bool accuracyMode)
            : base(rawText)
        {
            this.AccuracyMode = accuracyMode;
        }

        private List<Metric> Metrics { get; set; }

        private bool AccuracyMode { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var metrics = new List<Metric>();

            foreach (string line in this.RawText.Split('\n'))
            {
                string latencyMetricName = Regex.Match(line, @"[^.!?\.]*[\.]*[^.!?\.]*[\s]+latency").Value;
                if (!string.IsNullOrEmpty(latencyMetricName))
                {
                    double latencyMetricValue = double.Parse(Regex.Match(line.Split(':')[1], @"\d+(\.\d+)?")?.Value);

                    metrics.Add(new Metric(latencyMetricName, latencyMetricValue, "nanoseconds"));
                }

                string samplesPerSecondMetricName = Regex.Match(line, @"Samples\s*per\s*second").Value.Trim();
                if (!string.IsNullOrEmpty(samplesPerSecondMetricName))
                {
                    double samplesPerSecondValue = double.Parse(Regex.Match(line.Split(':')[1], @"\d+(\.\d+)?")?.Value);

                    metrics.Add(new Metric(samplesPerSecondMetricName, samplesPerSecondValue));
                }

                string totalWarningsLine = Regex.Match(line, "warnings encountered").Value.Trim();
                if (!string.IsNullOrEmpty(totalWarningsLine))
                {
                    double totalWarnings = double.Parse(string.IsNullOrEmpty(Regex.Match(line, @"\d+(\.\d+)?").Value) ? "0" : Regex.Match(line, @"\d+(\.\d+)?").Value);
                    metrics.Add(new Metric("Warnings", totalWarnings, "count"));
                }

                string totalErrorsLine = Regex.Match(line, "errors encountered").Value.Trim();
                if (!string.IsNullOrEmpty(totalErrorsLine))
                {
                    double totalErrors = double.Parse(string.IsNullOrEmpty(Regex.Match(line, @"\d+(\.\d+)?").Value) ? "0" : Regex.Match(line, @"\d+(\.\d+)?").Value);
                    metrics.Add(new Metric("Errors", totalErrors, "count"));
                }
            }

            return metrics;
        }
    }
}
