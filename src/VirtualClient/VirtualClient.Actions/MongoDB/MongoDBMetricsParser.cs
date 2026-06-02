namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MongoDB benchmark output.
    /// </summary>
    public class MongoDBMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex MongoDBSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Label for current running Scenario.
        /// </summary>
        private string scenario;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text which is output of the MongoDB workload</param>
        /// <param name="scenario">Scenario name</param>
        public MongoDBMetricsParser(string scenario, string rawText)
            : base(rawText)
        {
            this.scenario = scenario;
        }

        /// <summary>
        /// Logic to parse and read metrics
        /// </summary>
        public override IList<Metric> Parse()
        {
            try
            {
                // Validate input before processing
                if (string.IsNullOrWhiteSpace(this.RawText))
                {
                    throw new WorkloadException(
                        "The raw text provided for parsing is null or empty.",
                        ErrorReason.WorkloadResultsParsingFailed);
                }

                this.Preprocess();
                Dictionary<string, Tuple<string, double>> metricsMap = GetMetricsMap(this.Sections["Metrics"]);
                
                // Validate that we got some metrics
                if (metricsMap == null || metricsMap.Count == 0)
                {
                    throw new WorkloadException(
                        "No valid metrics could be parsed from the workload output.",
                        ErrorReason.WorkloadResultsParsingFailed);
                }

                List<Metric> metrics = new List<Metric>();
                
                foreach (var entry in metricsMap)
                {
                    var metricRelativity = MetricRelativity.Undefined;
                    if (entry.Key.Contains("ERROR") || entry.Key.Contains("FAILED") || entry.Key.Contains("NOT_FOUND") || entry.Key.Contains("TIMEOUT"))
                    {
                        metricRelativity = MetricRelativity.LowerIsBetter;
                    }
                    else if (entry.Key.Contains("Throughput"))
                    {
                        metricRelativity = MetricRelativity.HigherIsBetter;
                    }
                    else if (entry.Key.Contains("Count"))
                    {
                        metricRelativity = MetricRelativity.HigherIsBetter;
                    }
                    else if (entry.Key.Contains("Latency"))
                    {
                        metricRelativity = MetricRelativity.LowerIsBetter;
                    }
                    else if (entry.Key.Contains("Operations"))
                    {
                        metricRelativity = MetricRelativity.HigherIsBetter;
                    }
                    else if (entry.Key.Contains("Time"))
                    {
                        metricRelativity = MetricRelativity.LowerIsBetter;
                    }

                    metrics.Add(new Metric(name: entry.Key, value: entry.Value.Item2, unit: entry.Value.Item1, relativity: metricRelativity));
                }

                return metrics;
            }
            catch (WorkloadException)
            {
                // Re-throw WorkloadException as-is
                throw;
            }
            catch (Exception exc)
            {
                // Catch any other parsing errors (FormatException, ArgumentException, etc.)
                throw new WorkloadException(
                    "Failed to parse MongoDB workload output. The output format may be invalid or corrupted.",
                    exc,
                    ErrorReason.WorkloadResultsParsingFailed);
            }
        }

        /// <summary>
        /// Logic for preprocessing raw metrics
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

            // Holds all possible output formats of ycsb (potentially change if upgrading ycsb version/ouput format changes)
            this.Sections["Metrics"] = this.Sections["Metrics"]
                // OVERALL
                .Replace("[OVERALL], RunTime(ms)", "OVERALL-RunTime(ms)")
                .Replace("[OVERALL], Throughput(ops/sec)", "OVERALL-Throughput(ops/sec)")
                // JVM GC / TOTAL GC metrics (common YCSB + GC exporters)
                .Replace("[TOTAL_GCS_G1_Young_Generation], Count", "TOTAL_GCS_G1_Young_Generation-Count")
                .Replace("[TOTAL_GC_TIME_G1_Young_Generation], Time(ms)", "TOTAL_GC_TIME_G1_Young_Generation-Time(ms)")
                .Replace("[TOTAL_GC_TIME_%_G1_Young_Generation], Time(%)", "TOTAL_GC_TIME_%_G1_Young_Generation-Time(%)")
                .Replace("[TOTAL_GCS_G1_Concurrent_GC], Count", "TOTAL_GCS_G1_Concurrent_GC-Count")
                .Replace("[TOTAL_GC_TIME_G1_Concurrent_GC], Time(ms)", "TOTAL_GC_TIME_G1_Concurrent_GC-Time(ms)")
                .Replace("[TOTAL_GC_TIME_%_G1_Concurrent_GC], Time(%)", "TOTAL_GC_TIME_%_G1_Concurrent_GC-Time(%)")
                .Replace("[TOTAL_GCS_G1_Old_Generation], Count", "TOTAL_GCS_G1_Old_Generation-Count")
                .Replace("[TOTAL_GC_TIME_G1_Old_Generation], Time(ms)", "TOTAL_GC_TIME_G1_Old_Generation-Time(ms)")
                .Replace("[TOTAL_GC_TIME_%_G1_Old_Generation], Time(%)", "TOTAL_GC_TIME_%_G1_Old_Generation-Time(%)")
                .Replace("[TOTAL_GCs], Count", "TOTAL_GCs-Count")
                .Replace("[TOTAL_GC_TIME], Time(ms)", "TOTAL_GC_TIME-Time(ms)")
                .Replace("[TOTAL_GC_TIME_%], Time(%)", "TOTAL_GC_TIME_%-Time(%)")
                // LOAD / INIT / CLEANUP
                .Replace("[LOAD], Operations", "LOAD-Operations")
                .Replace("[LOAD], AverageLatency(us)", "LOAD-AverageLatency(us)")
                .Replace("[LOAD], MinLatency(us)", "LOAD-MinLatency(us)")
                .Replace("[LOAD], MaxLatency(us)", "LOAD-MaxLatency(us)")
                .Replace("[LOAD], 95thPercentileLatency(us)", "LOAD-95thPercentileLatency(us)")
                .Replace("[LOAD], 99thPercentileLatency(us)", "LOAD-99thPercentileLatency(us)")
                .Replace("[CLEANUP], Operations", "CLEANUP-Operations")
                .Replace("[CLEANUP], AverageLatency(us)", "CLEANUP-AverageLatency(us)")
                .Replace("[CLEANUP], MinLatency(us)", "CLEANUP-MinLatency(us)")
                .Replace("[CLEANUP], MaxLatency(us)", "CLEANUP-MaxLatency(us)")
                .Replace("[CLEANUP], 95thPercentileLatency(us)", "CLEANUP-95thPercentileLatency(us)")
                .Replace("[CLEANUP], 99thPercentileLatency(us)", "CLEANUP-99thPercentileLatency(us)")
                // READ
                .Replace("[READ], Operations", "READ-Operations")
                .Replace("[READ], AverageLatency(us)", "READ-AverageLatency(us)")
                .Replace("[READ], MinLatency(us)", "READ-MinLatency(us)")
                .Replace("[READ], MaxLatency(us)", "READ-MaxLatency(us)")
                .Replace("[READ], 95thPercentileLatency(us)", "READ-95thPercentileLatency(us)")
                .Replace("[READ], 99thPercentileLatency(us)", "READ-99thPercentileLatency(us)")
                .Replace("[READ], Return=OK", "READ-Return-OK-Count")
                .Replace("[READ], Return=ERROR", "READ-Return-ERROR-Count")
                .Replace("[READ], Return=NOT_FOUND", "READ-Return-NOT_FOUND-Count")
                .Replace("[READ], Return=FAILED", "READ-Return-FAILED-Count")
                .Replace("[READ], Return=TIMEOUT", "READ-Return-TIMEOUT-Count")
                // READ-FAILED (some clients emit this block)
                .Replace("[READ-FAILED], Operations", "READ-FAILED-Operations")
                .Replace("[READ-FAILED], AverageLatency(us)", "READ-FAILED-AverageLatency(us)")
                .Replace("[READ-FAILED], MinLatency(us)", "READ-FAILED-MinLatency(us)")
                .Replace("[READ-FAILED], MaxLatency(us)", "READ-FAILED-MaxLatency(us)")
                .Replace("[READ-FAILED], 95thPercentileLatency(us)", "READ-FAILED-95thPercentileLatency(us)")
                .Replace("[READ-FAILED], 99thPercentileLatency(us)", "READ-FAILED-99thPercentileLatency(us)")
                // UPDATE
                .Replace("[UPDATE], Operations", "UPDATE-Operations")
                .Replace("[UPDATE], AverageLatency(us)", "UPDATE-AverageLatency(us)")
                .Replace("[UPDATE], MinLatency(us)", "UPDATE-MinLatency(us)")
                .Replace("[UPDATE], MaxLatency(us)", "UPDATE-MaxLatency(us)")
                .Replace("[UPDATE], 95thPercentileLatency(us)", "UPDATE-95thPercentileLatency(us)")
                .Replace("[UPDATE], 99thPercentileLatency(us)", "UPDATE-99thPercentileLatency(us)")
                .Replace("[UPDATE], Return=OK", "UPDATE-Return-OK-Count")
                .Replace("[UPDATE], Return=ERROR", "UPDATE-Return-ERROR-Count")
                .Replace("[UPDATE], Return=NOT_FOUND", "UPDATE-Return-NOT_FOUND-Count")
                .Replace("[UPDATE], Return=FAILED", "UPDATE-Return-FAILED-Count")
                .Replace("[UPDATE], Return=TIMEOUT", "UPDATE-Return-TIMEOUT-Count")
                // UPDATE-FAILED
                .Replace("[UPDATE-FAILED], Operations", "UPDATE-FAILED-Operations")
                .Replace("[UPDATE-FAILED], AverageLatency(us)", "UPDATE-FAILED-AverageLatency(us)")
                .Replace("[UPDATE-FAILED], MinLatency(us)", "UPDATE-FAILED-MinLatency(us)")
                .Replace("[UPDATE-FAILED], MaxLatency(us)", "UPDATE-FAILED-MaxLatency(us)")
                .Replace("[UPDATE-FAILED], 95thPercentileLatency(us)", "UPDATE-FAILED-95thPercentileLatency(us)")
                .Replace("[UPDATE-FAILED], 99thPercentileLatency(us)", "UPDATE-FAILED-99thPercentileLatency(us)")
                // INSERT
                .Replace("[INSERT], Operations", "INSERT-Operations")
                .Replace("[INSERT], AverageLatency(us)", "INSERT-AverageLatency(us)")
                .Replace("[INSERT], MinLatency(us)", "INSERT-MinLatency(us)")
                .Replace("[INSERT], MaxLatency(us)", "INSERT-MaxLatency(us)")
                .Replace("[INSERT], 95thPercentileLatency(us)", "INSERT-95thPercentileLatency(us)")
                .Replace("[INSERT], 99thPercentileLatency(us)", "INSERT-99thPercentileLatency(us)")
                .Replace("[INSERT], Return=OK", "INSERT-Return-OK-Count")
                .Replace("[INSERT], Return=ERROR", "INSERT-Return-ERROR-Count")
                .Replace("[INSERT], Return=FAILED", "INSERT-Return-FAILED-Count")
                .Replace("[INSERT], Return=TIMEOUT", "INSERT-Return-TIMEOUT-Count")
                // INSERT-FAILED
                .Replace("[INSERT-FAILED], Operations", "INSERT-FAILED-Operations")
                .Replace("[INSERT-FAILED], AverageLatency(us)", "INSERT-FAILED-AverageLatency(us)")
                .Replace("[INSERT-FAILED], MinLatency(us)", "INSERT-FAILED-MinLatency(us)")
                .Replace("[INSERT-FAILED], MaxLatency(us)", "INSERT-FAILED-MaxLatency(us)")
                .Replace("[INSERT-FAILED], 95thPercentileLatency(us)", "INSERT-FAILED-95thPercentileLatency(us)")
                .Replace("[INSERT-FAILED], 99thPercentileLatency(us)", "INSERT-FAILED-99thPercentileLatency(us)")
                // DELETE
                .Replace("[DELETE], Operations", "DELETE-Operations")
                .Replace("[DELETE], AverageLatency(us)", "DELETE-AverageLatency(us)")
                .Replace("[DELETE], MinLatency(us)", "DELETE-MinLatency(us)")
                .Replace("[DELETE], MaxLatency(us)", "DELETE-MaxLatency(us)")
                .Replace("[DELETE], 95thPercentileLatency(us)", "DELETE-95thPercentileLatency(us)")
                .Replace("[DELETE], 99thPercentileLatency(us)", "DELETE-99thPercentileLatency(us)")
                .Replace("[DELETE], Return=OK", "DELETE-Return-OK-Count")
                .Replace("[DELETE], Return=ERROR", "DELETE-Return-ERROR-Count")
                .Replace("[DELETE], Return=NOT_FOUND", "DELETE-Return-NOT_FOUND-Count")
                .Replace("[DELETE], Return=FAILED", "DELETE-Return-FAILED-Count")
                // DELETE-FAILED
                .Replace("[DELETE-FAILED], Operations", "DELETE-FAILED-Operations")
                .Replace("[DELETE-FAILED], AverageLatency(us)", "DELETE-FAILED-AverageLatency(us)")
                .Replace("[DELETE-FAILED], MinLatency(us)", "DELETE-FAILED-MinLatency(us)")
                .Replace("[DELETE-FAILED], MaxLatency(us)", "DELETE-FAILED-MaxLatency(us)")
                .Replace("[DELETE-FAILED], 95thPercentileLatency(us)", "DELETE-FAILED-95thPercentileLatency(us)")
                .Replace("[DELETE-FAILED], 99thPercentileLatency(us)", "DELETE-FAILED-99thPercentileLatency(us)")
                // SCAN
                .Replace("[SCAN], Operations", "SCAN-Operations")
                .Replace("[SCAN], AverageLatency(us)", "SCAN-AverageLatency(us)")
                .Replace("[SCAN], MinLatency(us)", "SCAN-MinLatency(us)")
                .Replace("[SCAN], MaxLatency(us)", "SCAN-MaxLatency(us)")
                .Replace("[SCAN], 95thPercentileLatency(us)", "SCAN-95thPercentileLatency(us)")
                .Replace("[SCAN], 99thPercentileLatency(us)", "SCAN-99thPercentileLatency(us)")
                .Replace("[SCAN], Return=OK", "SCAN-Return-OK-Count")
                .Replace("[SCAN], Return=ERROR", "SCAN-Return-ERROR-Count")
                // SCAN-FAILED
                .Replace("[SCAN-FAILED], Operations", "SCAN-FAILED-Operations")
                .Replace("[SCAN-FAILED], AverageLatency(us)", "SCAN-FAILED-AverageLatency(us)")
                .Replace("[SCAN-FAILED], MinLatency(us)", "SCAN-FAILED-MinLatency(us)")
                .Replace("[SCAN-FAILED], MaxLatency(us)", "SCAN-FAILED-MaxLatency(us)")
                .Replace("[SCAN-FAILED], 95thPercentileLatency(us)", "SCAN-FAILED-95thPercentileLatency(us)")
                .Replace("[SCAN-FAILED], 99thPercentileLatency(us)", "SCAN-FAILED-99thPercentileLatency(us)")
                // READ-MODIFY-WRITE (RMW)
                .Replace("[READ-MODIFY-WRITE], Operations", "READ-MODIFY-WRITE-Operations")
                .Replace("[READ-MODIFY-WRITE], AverageLatency(us)", "READ-MODIFY-WRITE-AverageLatency(us)")
                .Replace("[READ-MODIFY-WRITE], MinLatency(us)", "READ-MODIFY-WRITE-MinLatency(us)")
                .Replace("[READ-MODIFY-WRITE], MaxLatency(us)", "READ-MODIFY-WRITE-MaxLatency(us)")
                .Replace("[READ-MODIFY-WRITE], 95thPercentileLatency(us)", "READ-MODIFY-WRITE-95thPercentileLatency(us)")
                .Replace("[READ-MODIFY-WRITE], 99thPercentileLatency(us)", "READ-MODIFY-WRITE-99thPercentileLatency(us)")
                .Replace("[READ-MODIFY-WRITE], Return=OK", "READ-MODIFY-WRITE-Return-OK-Count")
                .Replace("[READ-MODIFY-WRITE], Return=ERROR", "READ-MODIFY-WRITE-Return-ERROR-Count")
                // RMW-FAILED
                .Replace("[READ-MODIFY-WRITE-FAILED], Operations", "READ-MODIFY-WRITE-FAILED-Operations")
                .Replace("[READ-MODIFY-WRITE-FAILED], AverageLatency(us)", "READ-MODIFY-WRITE-FAILED-AverageLatency(us)")
                .Replace("[READ-MODIFY-WRITE-FAILED], MinLatency(us)", "READ-MODIFY-WRITE-FAILED-MinLatency(us)")
                .Replace("[READ-MODIFY-WRITE-FAILED], MaxLatency(us)", "READ-MODIFY-WRITE-FAILED-MaxLatency(us)")
                .Replace("[READ-MODIFY-WRITE-FAILED], 95thPercentileLatency(us)", "READ-MODIFY-WRITE-FAILED-95thPercentileLatency(us)")
                .Replace("[READ-MODIFY-WRITE-FAILED], 99thPercentileLatency(us)", "READ-MODIFY-WRITE-FAILED-99thPercentileLatency(us)")
                // RETURN CODES GENERAL (some clients format these without operation prefix)
                .Replace("[Return=OK], Operations", "Return-OK-Count")
                .Replace("[Return=ERROR], Operations", "Return-ERROR-Count")
                .Replace("[Return=NOT_FOUND], Operations", "Return-NOT_FOUND-Count")
                .Replace("[Return=FAILED], Operations", "Return-FAILED-Count")
                .Replace("[Return=TIMEOUT], Operations", "Return-TIMEOUT-Count");
        }

        /// <summary>
        /// Parses the metrics from string format to (string (name), Tuple (data)) format
        /// </summary>
        /// <param name="metrics">Preprocessed metrics</param>
        /// <returns></returns>
        private static Dictionary<string, Tuple<string, double>> GetMetricsMap(string metrics)
        {
            char seperator = '\n';
            string[] results = metrics.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            Dictionary<string, Tuple<string, double>> metricMap = new Dictionary<string, Tuple<string, double>>();
            
            foreach (string item in results)
            {
                try
                {
                    string[] metricData = item.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    if (metricData.Length > 0)
                    {
                        string[] metricNameList = metricData[0].Split('(', StringSplitOptions.RemoveEmptyEntries);

                        if (metricNameList.Length > 0)
                        {
                            string metricName = metricNameList[0].Trim();
                            
                            // Skip empty metric names
                            if (string.IsNullOrWhiteSpace(metricName))
                            {
                                continue;
                            }

                            double metricValue = 0.0;
                            string metricUnit = string.Empty;
                            
                            if (metricData.Length > 1 && !string.IsNullOrWhiteSpace(metricData[1]))
                            {
                                // Try to parse the value, skip if invalid
                                if (!double.TryParse(metricData[1].Trim(), out metricValue))
                                {
                                    continue;
                                }
                            }

                            if (metricNameList.Length > 1 && !string.IsNullOrWhiteSpace(metricNameList[1]))
                            {
                                string[] unitParts = metricNameList[1].Split(')', StringSplitOptions.RemoveEmptyEntries);
                                if (unitParts.Length > 0)
                                {
                                    metricUnit = unitParts[0].Trim();
                                }
                            }

                            Tuple<string, double> metricTuple = new Tuple<string, double>(metricUnit, metricValue);

                            if (!metricMap.ContainsKey(metricName))
                            {
                                metricMap.Add(metricName, metricTuple);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip invalid lines and continue parsing
                    continue;
                }
            }

            return metricMap;
        }
    }
}
