// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Redis Memtier benchmark output.
    /// </summary>
    public class MemtierMetricsParser : MetricsParser
    {
        private const string SplitAtSpace = @"\s{1,}";
        private const string ProcessResultSectionDelimiter = @"*{6}";

        // ============================================================================================================================================================
        // Type Ops/sec Hits/sec Misses/sec Avg.Latency     p50 Latency     p90 Latency     p95 Latency     p99 Latency   p99.9 Latency KB/sec
        // ------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Gets        43444.12     43444.12         0.00         2.61979         2.73500         3.88700         4.41500         7.42300        29.31100      3724.01
        // Sets         4827.17          ---          ---         2.64323         2.83100         3.93500         4.47900         7.45500        29.56700       329.45
        // Waits           0.00          ---          ---             ---             ---             ---             ---             ---             ---          ---
        // Totals      48271.29     43444.12         0.00         2.62213         2.75100         3.90300         4.41500         7.42300        29.31100      4053.46
        private static readonly Regex GetsExpression = new Regex(@"(?<=Gets).*(?=\n)", RegexOptions.IgnoreCase);
        private static readonly Regex SetsExpression = new Regex(@"(?<=Sets).*(?=\n)", RegexOptions.IgnoreCase);
        private static readonly Regex TotalsExpression = new Regex(@"(?<=Totals).*(?=\n)", RegexOptions.IgnoreCase);

        private bool perProcessMetric = false;
        private List<string> memtierCommandLines = new List<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MemtierMetricsParser"/> class.
        /// </summary>
        public MemtierMetricsParser(bool perProcessMetric, List<string> rawText, List<string> commandLines = null)
            : base(string.Join(ProcessResultSectionDelimiter, rawText))
        {
            this.perProcessMetric = perProcessMetric;
            this.memtierCommandLines = commandLines;
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.ThrowIfInvalidOutputFormat();
                List<Metric> aggregateMetrics = new List<Metric>();
                List<Metric> combinedMetrics = new List<Metric>();
                List<string> rawTextList = this.RawText.Split(ProcessResultSectionDelimiter).Select(s => s.Trim()).ToList();

                for (int i = 0; i < rawTextList.Count; i++)
                {
                    string rawText = rawTextList[i];
                    // Example Format:
                    //
                    // ALL STATS
                    // ============================================================================================================================================================
                    // Type         Ops/sec     Hits/sec   Misses/sec    Avg. Latency     p50 Latency     p90 Latency     p95 Latency     p99 Latency   p99.9 Latency       KB/sec
                    // ------------------------------------------------------------------------------------------------------------------------------------------------------------
                    // Sets         4827.17          ---          ---         2.64323         2.83100         3.93500         4.47900         7.45500        29.56700       329.45
                    // Gets        43444.12     43444.12         0.00         2.61979         2.73500         3.88700         4.41500         7.42300        29.31100      3724.01
                    // Waits           0.00          ---          ---             ---             ---             ---             ---             ---             ---          ---
                    // Totals      48271.29     43444.12         0.00         2.62213         2.75100         3.90300         4.41500         7.42300        29.31100      4053.46

                    List<Metric> perProcessMetrics = new List<Metric>();

                    var totals = MemtierMetricsParser.TotalsExpression.Matches(rawText);
                    MemtierMetricsParser.AddMetrics(perProcessMetrics, totals);

                    var getsResults = MemtierMetricsParser.GetsExpression.Matches(rawText);
                    MemtierMetricsParser.AddMetrics(perProcessMetrics, getsResults, "GET");

                    var setsResults = MemtierMetricsParser.SetsExpression.Matches(rawText);
                    MemtierMetricsParser.AddMetrics(perProcessMetrics, setsResults, "SET");

                    if (this.memtierCommandLines != null)
                    {
                        string memtiercommandLine = this.memtierCommandLines[i];
                        perProcessMetrics.AddMetadata(MemtierMetricsParser.GetMetadata(memtiercommandLine));
                    }

                    combinedMetrics.AddRange(perProcessMetrics);

                    if (this.perProcessMetric)
                    {
                        aggregateMetrics.AddRange(perProcessMetrics);
                    }
                }

                aggregateMetrics.AddRange(MemtierMetricsParser.AggregateMetrics(combinedMetrics));

                return aggregateMetrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse Memtier metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        private static void AddMetrics(List<Metric> metrics, MatchCollection matches, string metricNamePrefix = null)
        {
            if (matches?.Any() == true)
            {
                IDictionary<string, MetricAggregate> metricsBin = MemtierMetricsParser.CreateMetricsBin(metricNamePrefix);

                // We capture the Hits/sec and Misses/sec only for the totals. We don't
                // capture them for Gets or Sets operations because they are ONLY for
                // Gets and thus captured in the totals.
                if (!string.IsNullOrWhiteSpace(metricNamePrefix))
                {
                    metricsBin.Remove("Hits/sec");
                    metricsBin.Remove("Misses/sec");
                }

                foreach (Match match in matches)
                {
                    var values = Regex.Split(match.Value, MemtierMetricsParser.SplitAtSpace);

                    if (double.TryParse(values[1], out double opsPerSec))
                    {
                        metricsBin["Ops/sec"].Add(opsPerSec);
                    }

                    if (string.IsNullOrWhiteSpace(metricNamePrefix))
                    {
                        if (double.TryParse(values[2], out double hitsPerSec))
                        {
                            metricsBin["Hits/sec"].Add(hitsPerSec);
                        }

                        if (double.TryParse(values[3], out double missesPerSec))
                        {
                            metricsBin["Misses/sec"].Add(missesPerSec);
                        }
                    }

                    if (double.TryParse(values[4], out double avgLatency))
                    {
                        metricsBin["AvgLatency"].Add(avgLatency);
                    }

                    if (double.TryParse(values[5], out double p50Latency))
                    {
                        metricsBin["p50Latency"].Add(p50Latency);
                    }

                    if (double.TryParse(values[6], out double p90Latency))
                    {
                        metricsBin["p90Latency"].Add(p90Latency);
                    }

                    if (double.TryParse(values[7], out double p95Latency))
                    {
                        metricsBin["p95Latency"].Add(p95Latency);
                    }

                    if (double.TryParse(values[8], out double p99Latency))
                    {
                        metricsBin["p99Latency"].Add(p99Latency);
                    }

                    if (double.TryParse(values[9], out double p99_9Latency))
                    {
                        metricsBin["p99.9Latency"].Add(p99_9Latency);
                    }

                    if (double.TryParse(values[10], out double kbPerSec))
                    {
                        metricsBin["KB/sec"].Add(kbPerSec);
                    }
                }

                foreach (var entry in metricsBin)
                {
                    if (!entry.Value.Any())
                    {
                        entry.Value.Add(0);
                    }

                    metrics.Add(entry.Value.ToMetric());
                }
            }
        }

        private static IDictionary<string, MetricAggregate> CreateMetricsBin(string metricNamePrefix = null)
        {
            return new Dictionary<string, MetricAggregate>
            {
                // e.g.
                // Throughput-Req/sec
                // GET_Throughput-Req/sec
                // GET_Throughput-Req/sec
                // Latency-Avg
                // Latency-Avg (Gets)
                // Latency-Avg (Sets)
                {
                    "Ops/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Throughput" : $"{metricNamePrefix}_Throughput",
                        metricUnit: MetricUnit.RequestsPerSec,
                        relativity: MetricRelativity.HigherIsBetter,
                        description: "Total number of requests/operations per second during the period of time.")
                },
                {
                    "Hits/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Hits/sec" : $"{metricNamePrefix}_Hits/sec",
                        relativity: MetricRelativity.HigherIsBetter,
                        description: "Total number of cache hits per second during the period of time.")
                },
                {
                    "Misses/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Misses/sec" : $"{metricNamePrefix}_Misses/sec",
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Total number of cache misses per second during a period of time. This is an indication of data evictions due to reaching memory limits.")
                },
                {
                    "AvgLatency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-Avg" : $"{metricNamePrefix}_Latency-Avg",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Average latency for requests/operations during the period of time.")
                },
                {
                    "p50Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P50" : $"{metricNamePrefix}_Latency-P50",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 50% of all requests was at or under this value.")
                },
                {
                    "p90Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P90" : $"{metricNamePrefix}_Latency-P90",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 90% of all requests was at or under this value.")
                },
                {
                    "p95Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P95" : $"{metricNamePrefix}_Latency-P95",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 95% of all requests was at or under this value.")
                },
                {
                    "p99Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P99" : $"{metricNamePrefix}_Latency-P99",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 99% of all requests was at or under this value.")
                },
                {
                    "p99.9Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P99.9" : $"{metricNamePrefix}_Latency-P99.9",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 99.9% of all requests was at or under this value.")
                },
                {
                    "KB/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Bandwidth" : $"{metricNamePrefix}_Bandwidth",
                        metricUnit: MetricUnit.KilobytesPerSecond,
                        relativity: MetricRelativity.HigherIsBetter,
                        description: "Total amount of data transferred per second during the period of time.")
                }
            };
        }

        private static IDictionary<string, IConvertible> GetMetadata(string commandLine)
        {
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                metadata["commandline"] = commandLine;
            }

            Match protocol = Regex.Match(commandLine, @"--protocol\s*=*([a-z0-9_-]+)", RegexOptions.IgnoreCase);
            if (protocol.Success)
            {
                metadata["protocol"] = protocol.Groups[1].Value;
            }

            Match threads = Regex.Match(commandLine, @"--threads\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (threads.Success)
            {
                metadata["threads"] = int.Parse(threads.Groups[1].Value);
            }

            Match clients = Regex.Match(commandLine, @"--clients\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (clients.Success)
            {
                metadata["clientsPerThread"] = int.Parse(clients.Groups[1].Value);
            }

            Match ratio = Regex.Match(commandLine, @"--ratio\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (ratio.Success)
            {
                metadata["ratio"] = ratio.Groups[1].Value;
            }

            Match dataSize = Regex.Match(commandLine, @"--data-size\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (dataSize.Success)
            {
                metadata["dataSize"] = int.Parse(dataSize.Groups[1].Value);
            }

            Match pipeline = Regex.Match(commandLine, @"--pipeline\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (pipeline.Success)
            {
                metadata["pipeline"] = int.Parse(pipeline.Groups[1].Value);
            }

            Match keyMinimum = Regex.Match(commandLine, @"--key-minimum\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (keyMinimum.Success)
            {
                metadata["keyMinimum"] = int.Parse(keyMinimum.Groups[1].Value);
            }

            Match keyMaximum = Regex.Match(commandLine, @"--key-maximum\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (keyMaximum.Success)
            {
                metadata["keyMaximum"] = int.Parse(keyMaximum.Groups[1].Value);
            }

            Match keyPattern = Regex.Match(commandLine, @"--key-pattern\s*=*([a-z\:]+)", RegexOptions.IgnoreCase);
            if (keyPattern.Success)
            {
                metadata["keyPattern"] = keyPattern.Groups[1].Value;
            }

            return metadata;
        }

        private static List<Metric> AggregateMetrics(List<Metric> combinedMetrics)
        {
            Dictionary<string, List<double>> metricNameValueListDict = new Dictionary<string, List<double>>();
            Dictionary<string, List<Metric>> metricNameMetricsListDict = new Dictionary<string, List<Metric>>();

            foreach (var metric in combinedMetrics)
            {
                if (metricNameMetricsListDict.ContainsKey(metric.Name))
                {
                    metricNameValueListDict[metric.Name].Add(metric.Value);
                }
                else
                {
                    metricNameValueListDict.Add(metric.Name, new List<double>() { metric.Value });
                }

                if (metricNameMetricsListDict.ContainsKey(metric.Name))
                {
                    metricNameMetricsListDict[metric.Name].Add(metric);
                }
                else
                {
                    metricNameMetricsListDict.Add(metric.Name, new List<Metric>() { metric });
                }
            }

            List<Metric> newAggregateListOfMetrics = new List<Metric>();

            foreach (var metricKeyValuePair in metricNameValueListDict)
            {
                double avgValue = metricKeyValuePair.Value.Average();
                double minValue = metricKeyValuePair.Value.Min();
                double maxValue = metricKeyValuePair.Value.Max();
                double stdevValue = Math.Sqrt(metricKeyValuePair.Value.Select(x => Math.Pow(x - avgValue, 2)).Average());
                List<double> sortedValues = metricKeyValuePair.Value.OrderBy(x => x).ToList();
                double p80Value = sortedValues[(int)Math.Ceiling(sortedValues.Count * 0.8) - 1];

                newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Avg", avgValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Min", minValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Max", maxValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Stdev", stdevValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_P80", p80Value, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                if (metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit != MetricUnit.Milliseconds)
                {
                    // For throughput and bandwidth related metrics only.
                    double sumValue = metricKeyValuePair.Value.Sum();
                    newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Sum", sumValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                }
            }

            return newAggregateListOfMetrics;
        }

        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null || !this.RawText.Contains("Totals"))
            {
                throw new SchemaException($"Invalid results. Memtier output is not in the correct format for parsing.");
            }
        }
    }
}
