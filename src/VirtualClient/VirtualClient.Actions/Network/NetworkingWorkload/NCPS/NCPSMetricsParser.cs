// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Numerics.Distributions;
    using MathNet.Numerics.Statistics;
    using VirtualClient.Contracts;

    /// <summary>
    /// NCPS toolset results parser.
    /// </summary>
    public class NCPSMetricsParser : MetricsParser
    {
        private static readonly Regex CpsExpression = new Regex(@"^###ENDCPS\s(?<Cps>\d+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex SynRttExpression = new Regex(@"^###SYNRTT,(?<SynRttRecord>.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex RetransmitsExpression = new Regex(@"^###REXMIT,(?<RexmitRecord>.*)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex RxGbpsExpression = new Regex(@"^###RXGBPS\s(?<RxGbps>[\d.]+)", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex TxGbpsExpression = new Regex(@"^###TXGBPS\s(?<TxGbps>[\d.]+)", RegexOptions.Compiled | RegexOptions.Multiline);

        private double confidenceLevel;
        private double warmupTimeInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSMetricsParser"/> class.
        /// </summary>
        public NCPSMetricsParser(string results, double confidenceLevel, double warmupTimeSeconds)
            : base(results)
        {
            this.confidenceLevel = confidenceLevel;
            this.warmupTimeInSeconds = warmupTimeSeconds;
        }

        /// <summary>
        /// Parses the NCPS results and returns a list of metrics from them.
        /// </summary>
        public override IList<Metric> Parse()
        {
            try
            {
                IList<Metric> metrics = new List<Metric>();
                
                // Parse RXGBPS and TXGBPS metrics
                MatchCollection matches = NCPSMetricsParser.RxGbpsExpression.Matches(this.RawText);
                NCPSMetricsParser.AddThroughputMetrics(metrics, matches, "RxGbps", "Receive throughput in Gbps");

                matches = NCPSMetricsParser.TxGbpsExpression.Matches(this.RawText);
                NCPSMetricsParser.AddThroughputMetrics(metrics, matches, "TxGbps", "Transmit throughput in Gbps");

                // Parse CPS metrics
                matches = NCPSMetricsParser.CpsExpression.Matches(this.RawText);
                NCPSMetricsParser.AddConnectionPerSecondMetrics(metrics, matches);

                // Parse SYN RTT metrics
                matches = NCPSMetricsParser.SynRttExpression.Matches(this.RawText);
                NCPSMetricsParser.AddSynRttMetrics(metrics, matches);

                // Parse retransmit metrics
                matches = NCPSMetricsParser.RetransmitsExpression.Matches(this.RawText);
                NCPSMetricsParser.AddRetransmitMetrics(metrics, matches);

                // Parse periodic output for statistical metrics
                KeyValuePair<List<double>, List<double>> connectionsPerSec = NCPSMetricsParser.GetTimestampsAndConnectionsPerSec(this.RawText, this.warmupTimeInSeconds);

                if (connectionsPerSec.Value.Count > 0)
                {
                    NCPSMetricsParser.AddStatisticalMetrics(metrics, connectionsPerSec.Key, connectionsPerSec.Value, this.confidenceLevel);
                }

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException(
                    $"Results parsing operation failed. The NCPS parser failed to parse the results of the NCPS workload.",
                    exc,
                    ErrorReason.WorkloadResultsParsingFailed);
            }
        }

        private static void AddThroughputMetrics(IList<Metric> metrics, MatchCollection matches, string metricName, string description)
        {
            if (matches?.Any() == true)
            {
                if (double.TryParse(matches[0].Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double throughput))
                {
                    metrics.Add(new Metric(metricName, throughput, "Gbps", MetricRelativity.HigherIsBetter, description: description));
                }
            }
        }

        private static void AddStatisticalMetrics(IList<Metric> metrics, List<double> timestamps, List<double> connectsPerSec, double confidenceLevel)
        {
            Normal normal = new Normal();
            double theta = ((confidenceLevel / 100.0) + 1.0) / 2;
            double mean = connectsPerSec.Mean();
            double sd = connectsPerSec.StandardDeviation();
            double inverserCDF = normal.InverseCumulativeDistribution(theta);
            double sem = sd / (double)Math.Sqrt(connectsPerSec.Count);
            double t = inverserCDF * sem;
            double lowerCI = mean - t;
            double upperCI = mean + t;

            metrics.Add(new Metric("ConnectsPerSec_Min", connectsPerSec.Min()));
            metrics.Add(new Metric("ConnectsPerSec_Max", connectsPerSec.Max()));
            metrics.Add(new Metric("ConnectsPerSec_Med", connectsPerSec.Median()));
            metrics.Add(new Metric("ConnectsPerSec_Avg", connectsPerSec.Average(), MetricUnit.TransactionsPerSec, MetricRelativity.HigherIsBetter, verbosity: 0));
            metrics.Add(new Metric("ConnectsPerSec_P25", connectsPerSec.Percentile(25)));
            metrics.Add(new Metric("ConnectsPerSec_P50", connectsPerSec.Percentile(50)));
            metrics.Add(new Metric("ConnectsPerSec_P75", connectsPerSec.Percentile(75)));
            metrics.Add(new Metric("ConnectsPerSec_P90", connectsPerSec.Percentile(90)));
            metrics.Add(new Metric("ConnectsPerSec_P99", connectsPerSec.Percentile(99)));
            metrics.Add(new Metric("ConnectsPerSec_P99_9", Statistics.QuantileCustom(connectsPerSec, 1d - 0.001d, QuantileDefinition.R3)));
            metrics.Add(new Metric("ConnectsPerSec_P99_99", Statistics.QuantileCustom(connectsPerSec, 1d - 0.0001d, QuantileDefinition.R3)));
            metrics.Add(new Metric("ConnectsPerSec_P99_999", Statistics.QuantileCustom(connectsPerSec, 1d - 0.00001d, QuantileDefinition.R3)));
            double median = Statistics.Median(connectsPerSec);
            double[] absoluteDeviations = connectsPerSec.Select(x => Math.Abs(x - median)).ToArray();
            metrics.Add(new Metric("ConnectsPerSec_Mad", Statistics.Median(absoluteDeviations), MetricUnit.TransactionsPerSec, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("ConnectsPerSec_StandardErrorMean", sem, MetricUnit.TransactionsPerSec, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("ConnectsPerSec_LowerCI", lowerCI, MetricUnit.TransactionsPerSec, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("ConnectsPerSec_UpperCI", upperCI, MetricUnit.TransactionsPerSec, MetricRelativity.LowerIsBetter, verbosity: 2));
        }

        /// <summary>
        /// Parses the NCPS results and returns a list of pairs of {Timestamp, Conn/s} for workloads duration (excluding warmupTime).
        /// </summary>
        private static KeyValuePair<List<double>, List<double>> GetTimestampsAndConnectionsPerSec(string content, double warmupTime)
        {
            bool appendResult = true;
            int valueIndex = 0;
            
            // The timestamps and connectionsPerSecond co-relate on the index.
            List<double> timestamps = new List<double>();
            List<double> connectionsPerSec = new List<double>();
            StringReader strReader = new StringReader(content);

            try
            {
                double excludeCount = warmupTime;

                while (true)
                {
                    string line = strReader.ReadLine();

                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    else if (appendResult)
                    {
                        string[] stringList = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                        if (stringList.Contains("Conn/s"))
                        {
                            valueIndex = Array.IndexOf(stringList, "Conn/s");
                            continue;
                        }
                        else if (stringList.Length == 0)
                        {
                            appendResult = false;
                        }
                        else
                        {
                            string formattedTimestamp = string.Format("{0:0.##}", stringList[0]);
                            string formattedConnectRate = string.Format("{0:0.#}", stringList[valueIndex]);
                            double timestamp = Convert.ToDouble(formattedTimestamp);
                            double connectRate = Convert.ToDouble(formattedConnectRate);

                            if (excludeCount < Math.Floor(timestamp))
                            {
                                timestamps.Add(Convert.ToDouble(formattedTimestamp));
                                connectionsPerSec.Add(Convert.ToDouble(formattedConnectRate));
                            }
                        }
                    }
                }
            }
            catch (ArgumentNullException exc)
            {
                throw new WorkloadResultsException(
                    "Results parsing operation failed. The NCPS parser failed to parse the results of the NCPS workload. Error getting values of Conn/s",
                    exc,
                    ErrorReason.WorkloadResultsNotFound);
            }

            return new KeyValuePair<List<double>, List<double>>(timestamps, connectionsPerSec);
        }

        private static void AddConnectionPerSecondMetrics(IList<Metric> metrics, MatchCollection matches)
        {
            if (matches?.Any() == true)
            {
                if (double.TryParse(matches[0].Groups[1].Value, out double cps))
                {
                    metrics.Add(new Metric("Cps", cps, MetricRelativity.HigherIsBetter, description: "Connections per second"));
                }
            }
        }

        private static void AddRetransmitMetrics(IList<Metric> metrics, MatchCollection matches)
        {
            if (matches?.Any() == true)
            {
                Match match = matches.First();
                string[] tokens = match.Groups[1].Value.Split(',');

                foreach (string token in tokens)
                {
                    string[] tokenPair = token.Split(':');
                    if (tokenPair?.Any() == true && tokenPair.Length == 2)
                    {
                        string metricName = tokenPair[0].Trim();
                        string metricValue = tokenPair[1].Trim();

                        switch (metricName.ToLowerInvariant())
                        {
                            case "rtconnpercentage":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double median))
                                {
                                    metrics.Add(new Metric("RexmitConnPercentage", median, MetricRelativity.LowerIsBetter, description: "Connection retransmits percentage."));
                                }

                                break;

                            case "rtperconn":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double mean))
                                {
                                    metrics.Add(new Metric("RexmitPerConn", mean, MetricRelativity.LowerIsBetter, description: "Retransmits per connection."));
                                }

                                break;
                        }
                    }
                }
            }
        }

        private static void AddSynRttMetrics(IList<Metric> metrics, MatchCollection matches)
        {
            if (matches?.Any() == true)
            {
                Match match = matches.First();
                string[] tokens = match.Groups[1].Value.Split(',');

                foreach (string token in tokens)
                {
                    string[] tokenPair = token.Split(':');
                    if (tokenPair?.Any() == true && tokenPair.Length == 2)
                    {
                        string metricName = tokenPair[0].Trim();
                        string metricValue = tokenPair[1].Trim();

                        switch (metricName.ToLowerInvariant())
                        {
                            case "median":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double median))
                                {
                                    metrics.Add(new Metric("SynRttMedian", median, MetricRelativity.LowerIsBetter, description: "Synchronous (SYN) median round-trip time."));
                                }

                                break;

                            case "mean":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double mean))
                                {
                                    metrics.Add(new Metric("SynRttMean", mean, MetricRelativity.LowerIsBetter, description: "Synchronous (SYN) mean round-trip time."));
                                }

                                break;

                            case "25":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p25))
                                {
                                    metrics.Add(new Metric("SynRttP25", p25, MetricRelativity.LowerIsBetter, description: "P25 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "75":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p75))
                                {
                                    metrics.Add(new Metric("SynRttP75", p75, MetricRelativity.LowerIsBetter, description: "P75 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "90":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p90))
                                {
                                    metrics.Add(new Metric("SynRttP90", p90, MetricRelativity.LowerIsBetter, description: "P90 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "95":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p95))
                                {
                                    metrics.Add(new Metric("SynRttP95", p95, MetricRelativity.LowerIsBetter, description: "P95 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "99":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p99))
                                {
                                    metrics.Add(new Metric("SynRttP99", p99, MetricRelativity.LowerIsBetter, description: "P99 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "99.9":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p99_9))
                                {
                                    metrics.Add(new Metric("SynRttP99_9", p99_9, MetricRelativity.LowerIsBetter, description: "P99.9 Synchronous (SYN) round-trip time."));
                                }

                                break;

                            case "99.99":
                                if (double.TryParse(metricValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double p99_99))
                                {
                                    metrics.Add(new Metric("SynRttP99_99", p99_99, MetricRelativity.LowerIsBetter, description: "P99.99 Synchronous (SYN) round-trip time."));
                                }

                                break;
                        }
                    }
                }
            }
        }
    }
}
