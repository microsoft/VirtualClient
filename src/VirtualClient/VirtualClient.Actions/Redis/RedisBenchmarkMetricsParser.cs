// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Redis benchmark output CSV-formatted results.
    /// </summary>
    public class RedisBenchmarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBenchmarkMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text which is output of the Redis benchmark</param>
        public RedisBenchmarkMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Parses the metrics for each of the scenarios covered by the Redis benchmark
        /// toolset (e.g. PING_INLINE, GET, SET).
        /// </summary>
        /// <returns></returns>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();

                // Example Format:
                // "test","rps","avg_latency_ms","min_latency_ms","p50_latency_ms","p95_latency_ms","p99_latency_ms","max_latency_ms"
                // "PING_INLINE","3154.38","15.758","0.416","15.935","16.335","27.935","53.599"
                // "PING_MBULK","3065.04","16.231","0.416","15.935","16.799","27.951","52.063"
                // "SET","3340.12","14.829","0.472","15.927","16.383","27.951","39.903"
                // "GET","3066.73","16.222","3.832","15.935","16.511","27.983","44.191"
                // "INCR","3127.64","15.891","0.448","15.935","16.671","30.799","44.031"
                // "LPUSH","3049.62","16.310","6.744","15.943","17.887","31.807","50.495"
                // "RPUSH","3158.06","15.736","0.488","15.935","16.215","26.639","44.031"
                // "LPOP","3048.32","16.316","0.584","15.935","18.047","31.823","56.639"
                // "RPOP","3148.52","15.794","0.440","15.935","16.239","27.935","52.095"
                // "SADD","3104.24","16.005","0.616","15.935","18.159","31.839","55.999"
                // "HSET","3056.42","16.273","0.712","15.935","16.543","31.759","47.967"
                // "SPOP","3130.28","15.874","0.384","15.943","16.479","31.743","50.751"
                // "ZADD","3042.94","16.345","0.632","15.935","17.407","31.903","44.031"
                // "ZPOPMIN","3157.16","15.738","0.392","15.935","16.247","27.871","59.263"
                // "LPUSH (needed to benchmark LRANGE)","3096.74","16.063","0.464","15.935","16.135","20.511","55.455"
                // "LRANGE_100 (first 100 elements)","3115.85","15.860","0.904","15.847","16.223","27.759","56.607"
                // "LRANGE_300 (first 300 elements)","3018.32","16.066","0.920","15.551","19.327","31.599","65.791"
                // "LRANGE_500 (first 450 elements)","3037.57","15.768","0.856","15.359","17.983","31.327","61.343"
                // "LRANGE_600 (first 600 elements)","3037.85","15.540","1.048","15.159","17.823","30.575","59.967"
                // "MSET (10 keys)","3040.53","16.313","0.488","15.927","27.967","35.903","59.903"

                List<Metric> metrics = new List<Metric>();
                this.AddMetricsFromCsv(metrics);

                if (metrics.Count <= 0)
                {
                    throw new SchemaException(
                        $"Invalid/unpexpected format. The Redis benchmark workload results were not in the expected CSV format or did not contain valid measurements.");
                }

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse Redis benchmark metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @$"""", string.Empty);
        }

        private void AddMetricsFromCsv(List<Metric> metrics)
        {
            IEnumerable<string> operationTypeMetricsLines = this.PreprocessedText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)?.Skip(1);

            if (operationTypeMetricsLines?.Any() == true)
            {
                Regex operationTypeExpression = new Regex(@"([a-z0-9_]+)", RegexOptions.IgnoreCase);

                foreach (string line in operationTypeMetricsLines)
                {
                    var values = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    string operationType = operationTypeExpression.Match(values[0]).Groups[1].Value;

                    if (operationType == "MSET")
                    {
                        string keys = Regex.Match(values[0], @"\d+", RegexOptions.IgnoreCase).Value;
                        operationType = $"{operationType}_{keys}";
                    }

                    // The metric values are read in the order at which they exist within the Redis 
                    // benchmark CSV output.
                    if (double.TryParse(values[1], out double reqPerSec))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Throughput",
                            reqPerSec,
                            MetricUnit.RequestsPerSec,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Total number of requests/operations per second during the period of time."));
                    }

                    if (double.TryParse(values[2], out double avgLatency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-Avg",
                            avgLatency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "Average latency for requests/operations during the period of time."));
                    }

                    if (double.TryParse(values[3], out double minLatency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-Min",
                            minLatency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "Minimum latency for requests/operations during the period of time."));
                    }

                    if (double.TryParse(values[4], out double p50Latency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-P50",
                            p50Latency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "The latency for 50% of all requests was at or under this value."));
                    }

                    if (double.TryParse(values[5], out double p95Latency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-P95",
                            p95Latency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "The latency for 95% of all requests was at or under this value."));
                    }

                    if (double.TryParse(values[6], out double p99Latency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-P99",
                            p99Latency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "The latency for 99% of all requests was at or under this value."));
                    }

                    if (double.TryParse(values[7], out double maxLatency))
                    {
                        metrics.Add(new Metric(
                            $"{operationType}_Latency-Max",
                            maxLatency,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "Maximum latency for requests/operations during the period of time."));
                    }
                }
            }
        }
    }
}
