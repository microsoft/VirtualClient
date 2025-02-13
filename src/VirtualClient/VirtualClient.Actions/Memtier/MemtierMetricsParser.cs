// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Numerics.Statistics;
    using VirtualClient;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Redis Memtier benchmark output.
    /// </summary>
    public class MemtierMetricsParser : MetricsParser
    {
        private const string SplitAtSpace = @"\s{1,}";

        // ============================================================================================================================================================
        // Type Ops/sec Hits/sec Misses/sec Avg.Latency     p50 Latency     p90 Latency     p95 Latency     p99 Latency   p99.9 Latency KB/sec
        // ------------------------------------------------------------------------------------------------------------------------------------------------------------
        // Gets        43444.12     43444.12         0.00         2.61979         2.73500         3.88700         4.41500         7.42300        29.31100      3724.01
        // Sets         4827.17          ---          ---         2.64323         2.83100         3.93500         4.47900         7.45500        29.56700       329.45
        // Waits           0.00          ---          ---             ---             ---             ---             ---             ---             ---          ---
        // Totals      48271.29     43444.12         0.00         2.62213         2.75100         3.90300         4.41500         7.42300        29.31100      4053.46
        
        private static readonly Regex BandwidthExpression = new Regex(@"Bandwidth|Throughput", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GetsExpression = new Regex(@"(?<=Gets).*(?=\n)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GetLatencyP80Expression = new Regex(@"(?<=GET)\s+([0-9\.]+)\s+.*80[\.0]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SetLatencyP80Expression = new Regex(@"(?<=SET)\s+([0-9\.]+)\s+.*80[\.0]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SetsExpression = new Regex(@"(?<=Sets).*(?=\n)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TotalsExpression = new Regex(@"(?<=Totals).*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="MemtierMetricsParser"/> class.
        /// </summary>
        public MemtierMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Assembles the metrics into a set of aggregates (e.g. Avg, Min, Max, Stdev).
        /// </summary>
        /// <param name="metrics">The set of metrics to aggregate.</param>
        /// <returns>
        /// A set of metrics that is an aggregate of the raw metrics provided.
        /// </returns>
        public static IList<Metric> Aggregate(IEnumerable<Metric> metrics)
        {
            // Setup the metrics so that we can calculate the aggregates (e.g. Avg, Min, Max)
            IDictionary<string, MetricAggregate> aggregations = new Dictionary<string, MetricAggregate>(StringComparer.OrdinalIgnoreCase);
            foreach (var metric in metrics)
            {
                MetricAggregate aggregate;
                if (!aggregations.TryGetValue(metric.Name, out aggregate))
                {
                    aggregate = new MetricAggregate(metric.Name, metric.Unit, metric.Relativity, description: metric.Description);
                    aggregations.Add(metric.Name, aggregate);
                }

                aggregate.Add(metric.Value);
            }

            // Create the set of metric aggregates.
            //
            // e.g.
            // GET_Bandwidth -> GET-Bandwidth AVG
            //               -> GET-Bandwidth MIN
            //               -> GET-Bandwidth MAX
            //               -> GET-Bandwidth STDDEV
            //               -> GET-Bandwidth P80
            //               -> GET-Bandwidth TOTAL
            List<Metric> metricAggregates = new List<Metric>();
            foreach (MetricAggregate aggregate in aggregations.Values)
            {
                metricAggregates.Add(new Metric($"{aggregate.Name} Avg", aggregate.Average(), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                metricAggregates.Add(new Metric($"{aggregate.Name} Min", aggregate.Min(), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                metricAggregates.Add(new Metric($"{aggregate.Name} Max", aggregate.Max(), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                metricAggregates.Add(new Metric($"{aggregate.Name} Stddev", aggregate.StandardDeviation(), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));

                if (MemtierMetricsParser.BandwidthExpression.IsMatch(aggregate.Name))
                {
                    // e.g.
                    // GET-Bandwidth P80
                    // GET-Bandwidth TOTAL
                    metricAggregates.Add(new Metric($"{aggregate.Name} P20", aggregate.Percentile(20), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                    metricAggregates.Add(new Metric($"{aggregate.Name} P50", aggregate.Percentile(50), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                    metricAggregates.Add(new Metric($"{aggregate.Name} P80", aggregate.Percentile(80), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                    metricAggregates.Add(new Metric($"{aggregate.Name} Total", aggregate.Sum(), aggregate.Unit, aggregate.Relativity, description: aggregate.Description));
                }
            }

            return metricAggregates;
        }

        /// <summary>
        /// Parses metadata properties out of the command line that can be used to associate with
        /// a set of related metrics.
        /// </summary>
        /// <param name="commandLine">The Memtier command line from which to parse the metadata properties.</param>
        /// <param name="cpuAffinity">The relative CPU number for which the target server (e.g. Redis, Memcached) has affinity.</param>
        /// <returns>A set of metadata properties that can be included with the metrics.</returns>
        public static IDictionary<string, IConvertible> ParseMetadata(string commandLine, string cpuAffinity = null)
        {
            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            if (cpuAffinity != null)
            {
                metadata["cpuAffinity"] = cpuAffinity;
            }

            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                metadata["commandline"] = commandLine;
            }

            Match protocol = Regex.Match(commandLine, @"--protocol\s*=*([a-z0-9_-]+)", RegexOptions.IgnoreCase);
            if (protocol.Success)
            {
                metadata["protocol"] = protocol.Groups[1].Value;
            }

            Match port = Regex.Match(commandLine, @"--port\s*=*([0-9]+)", RegexOptions.IgnoreCase);
            if (port.Success)
            {
                metadata["port"] = port.Groups[1].Value;
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

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.ThrowIfInvalidOutputFormat();

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

                List<Metric> metrics = new List<Metric>();

                var totals = MemtierMetricsParser.TotalsExpression.Matches(this.RawText);
                MemtierMetricsParser.AddMetrics(metrics, totals);

                var getsResults = MemtierMetricsParser.GetsExpression.Matches(this.RawText);
                MemtierMetricsParser.AddMetrics(metrics, getsResults, "GET");

                var setsResults = MemtierMetricsParser.SetsExpression.Matches(this.RawText);
                MemtierMetricsParser.AddMetrics(metrics, setsResults, "SET");

                MemtierMetricsParser.AddAdditionalLatencyPercentileMetrics(this.RawText, metrics);

                return metrics;
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
                        metricNamePrefix == null ? "Throughput" : $"{metricNamePrefix}-Throughput",
                        metricUnit: MetricUnit.RequestsPerSec,
                        relativity: MetricRelativity.HigherIsBetter,
                        verbosity: 0,
                        description: "Total number of requests/operations per second during the period of time.")
                },
                {
                    "Hits/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Hits/sec" : $"{metricNamePrefix}-Hits/sec",
                        relativity: MetricRelativity.HigherIsBetter,
                        description: "Total number of cache hits per second during the period of time.")
                },
                {
                    "Misses/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Misses/sec" : $"{metricNamePrefix}-Misses/sec",
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Total number of cache misses per second during a period of time. This is an indication of data evictions due to reaching memory limits.")
                },
                {
                    "AvgLatency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-Avg" : $"{metricNamePrefix}-Latency-Avg",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Average latency for requests/operations during the period of time.")
                },
                {
                    "p50Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P50" : $"{metricNamePrefix}-Latency-P50",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        verbosity: 0,
                        description: "The latency for 50% of all requests was at or under this value.")
                },
                {
                    "p90Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P90" : $"{metricNamePrefix}-Latency-P90",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 90% of all requests was at or under this value.")
                },
                {
                    "p95Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P95" : $"{metricNamePrefix}-Latency-P95",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 95% of all requests was at or under this value.")
                },
                {
                    "p99Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P99" : $"{metricNamePrefix}-Latency-P99",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        verbosity: 0,
                        description: "The latency for 99% of all requests was at or under this value.")
                },
                {
                    "p99.9Latency",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Latency-P99.9" : $"{metricNamePrefix}-Latency-P99.9",
                        metricUnit: MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 99.9% of all requests was at or under this value.")
                },
                {
                    "KB/sec",
                    new MetricAggregate(
                        metricNamePrefix == null ? "Bandwidth" : $"{metricNamePrefix}-Bandwidth",
                        metricUnit: MetricUnit.KilobytesPerSecond,
                        relativity: MetricRelativity.HigherIsBetter,
                        verbosity: 0,
                        description: "Total amount of data transferred per second during the period of time.")
                }
            };
        }

        private static void AddAdditionalLatencyPercentileMetrics(string output, List<Metric> metrics)
        {
            double p80LatencyOnGet = 0.0;
            double p80LatencyOnSet = 0.0;

            Match getLatencyP80 = MemtierMetricsParser.GetLatencyP80Expression.Match(output);

            if (getLatencyP80.Success)
            {
                p80LatencyOnGet = double.Parse(getLatencyP80.Groups[1].Value);

                metrics.Add(new Metric(
                    "GET-Latency-P80",
                    p80LatencyOnGet,
                    MetricUnit.Milliseconds,
                    MetricRelativity.LowerIsBetter,
                    description: "The latency for 80% of all requests was at or under this value."));
            }

            Match setLatencyP80 = MemtierMetricsParser.SetLatencyP80Expression.Match(output);
            if (setLatencyP80.Success)
            {
                p80LatencyOnSet = double.Parse(setLatencyP80.Groups[1].Value);

                metrics.Add(new Metric(
                    "SET-Latency-P80",
                    p80LatencyOnSet,
                    MetricUnit.Milliseconds,
                    MetricRelativity.LowerIsBetter,
                    description: "The latency for 80% of all requests was at or under this value."));
            }

            metrics.Add(new Metric(
                "Latency-P80",
                (p80LatencyOnGet + p80LatencyOnSet) / 2,
                MetricUnit.Milliseconds,
                MetricRelativity.LowerIsBetter,
                description: "The latency for 80% of all requests was at or under this value."));
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
