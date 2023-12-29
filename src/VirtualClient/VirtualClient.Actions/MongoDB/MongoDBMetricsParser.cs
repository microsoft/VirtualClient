namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MongoDB benchmark output.
    /// </summary>
    public class MongoDBMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex MongoDBSectionDelimiter = new (@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text which is output of the MongoDB workload</param>
        public MongoDBMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Logic to parse and read metrics8
        /// </summary>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                Dictionary<string, Tuple<string, double>> metricsMap = GetMetricsMap(this.Sections["Metrics"]);
                List<Metric> metrics = new List<Metric>();
                var metricRelativity = MetricRelativity.Undefined;

                foreach (var entry in metricsMap)
                {
                    if (entry.Key == "OVERALL-Throughput")
                    {
                        metricRelativity = MetricRelativity.HigherIsBetter;
                    }
                    else if (entry.Key.Contains("AverageLatency"))
                    {
                        metricRelativity = MetricRelativity.LowerIsBetter;
                    }
                    else if (entry.Key.Contains("95thPercentileLatency"))
                    {
                        metricRelativity = MetricRelativity.LowerIsBetter;
                    }

                    metrics.Add(new Metric(entry.Key, entry.Value.Item2, entry.Value.Item1, metricRelativity));

                }

                return metrics;
            }
            catch (JsonException exc)
            {
                throw new WorkloadResultsException("Workload results parsing failure. The example workload results are not valid or are not formatted in a valid JSON.", exc, ErrorReason.WorkloadResultsParsingFailed);
            }
        }

        /// <summary>
        /// Logic for preprocessing
        /// </summary>
        protected override void Preprocess()
        {
            RegexOptions options = RegexOptions.None;
            var regex = new Regex("[ ]{2,}", options);
            this.PreprocessedText = regex.Replace(this.RawText, " ");
            string pattern = @"(?=\[OVERALL\])"; // Pattern to find the position before the word "[OVERALL]"
            string newSection = $"{Environment.NewLine}Metrics{Environment.NewLine}";

            Regex rgx = new Regex(pattern);
            this.PreprocessedText = rgx.Replace(this.PreprocessedText, newSection, 1);

            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, MongoDBSectionDelimiter);
            if (!this.Sections.ContainsKey("Metrics") || string.IsNullOrWhiteSpace(this.Sections["Metrics"]))
            {
                throw new WorkloadException(
                        $"Benchmarking metrics are not present", ErrorReason.WorkloadResultsParsingFailed);
            }

            this.Sections["Metrics"] = this.Sections["Metrics"]
                .Replace("[OVERALL], RunTime", "OVERALL-RunTime")
                .Replace("[OVERALL], Throughput", "OVERALL-Throughput")
                .Replace("[CLEANUP], Operations", "CLEANUP-Operations")
                .Replace("[CLEANUP], AverageLatency", "CLEANUP-AverageLatency")
                .Replace("[CLEANUP], MinLatency", "CLEANUP-MinLatency")
                .Replace("[CLEANUP], MaxLatency", "CLEANUP-MaxLatency")
                .Replace("[CLEANUP], 95thPercentileLatency", "CLEANUP-95thPercentileLatency")
                .Replace("[CLEANUP], 99thPercentileLatency", "CLEANUP-99thPercentileLatency")
                .Replace("[INSERT], Operations", "INSERT-Operations")
                .Replace("[INSERT], AverageLatency", "INSERT-AverageLatency")
                .Replace("[INSERT], MinLatency", "INSERT-MinLatency")
                .Replace("[INSERT], MaxLatency", "INSERT-MaxLatency")
                .Replace("[INSERT], 95thPercentileLatency", "INSERT-95thPercentileLatency")
                .Replace("[INSERT], 99thPercentileLatency", "INSERT-99thPercentileLatency")
                .Replace("[INSERT], Return=OK", "INSERT-Count")
                .Replace("[INSERT], Return=ERROR", "INSERT-Error-Count")
                .Replace("[READ], Operations", "SELECT-Operations")
                .Replace("[READ], AverageLatency", "SELECT-AverageLatency")
                .Replace("[READ], MinLatency", "SELECT-MinLatency")
                .Replace("[READ], MaxLatency", "SELECT-MaxLatency")
                .Replace("[READ], 95thPercentileLatency", "SELECT-95thPercentileLatency")
                .Replace("[READ], 99thPercentileLatency", "SELECT-99thPercentileLatency")
                .Replace("[READ], Return=OK", "SELECT-Count")
                .Replace("[READ], Return=NOT_FOUND", "READ-NotFound-Count")
                .Replace("[UPDATE], Operations", "UPDATE-Operations")
                .Replace("[UPDATE], AverageLatency", "UPDATE-AverageLatency")
                .Replace("[UPDATE], MinLatency", "UPDATE-MinLatency")
                .Replace("[UPDATE], MaxLatency", "UPDATE-MaxLatency")
                .Replace("[UPDATE], 95thPercentileLatency", "UPDATE-95thPercentileLatency")
                .Replace("[UPDATE], 99thPercentileLatency", "UPDATE-99thPercentileLatency")
                .Replace("[UPDATE], Return=OK", "UPDATE-Count")
                .Replace("[UPDATE], Return=NOT_FOUND", "UPDATE-NotFound-Count");
        }

        private static Dictionary<string, Tuple<string, double>> GetMetricsMap(string metrics)
        {
            char seperator = '\n';
            string[] results = metrics.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, Tuple<string, double>> metricMap = new Dictionary<string, Tuple<string, double>>();
            foreach (string item in results)
            {
                string[] metricData = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (metricData.Length > 0)
                {
                    string[] metricNameList = metricData[0].Split('(', StringSplitOptions.RemoveEmptyEntries);
                    if (metricNameList.Length > 0)
                    {
                        string metricName = metricNameList[0];
                        double metricValue = 0.0;
                        string metricUnit = string.Empty;
                        if (metricData.Length > 1 && metricData[1] != null)
                        {
                            metricValue = Convert.ToDouble(metricData[1]);
                        }

                        if (metricNameList.Length > 1 && metricNameList[1] != null)
                        {
                            metricUnit = metricNameList[1].Split(')', StringSplitOptions.RemoveEmptyEntries)[0];
                        }

                        Tuple<string, double> metricTuple = new Tuple<string, double>(metricUnit, metricValue);
                        metricMap.Add(metricName, metricTuple);
                    }
                }

            }

            return metricMap;
        }
    }
}
