using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VirtualClient.Common.Contracts;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    /// <summary>
    /// Parser for Redis benchmark output document.
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
        /// Results for Linear equations single-precision .
        /// </summary>
        public DataTable RedisResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            this.Preprocess();
            string[] metricNames =
            {
                "Requests/Sec", "Average_Latency", "Min_Latency", "P50_Latency", "P95_Latency", "P99_Latency", "Max_Latency"
            };

            List<Metric> metrics = new List<Metric>();
            List<string> lines = this.PreprocessedText.Split(Environment.NewLine).ToList();
            foreach (string line in lines)
            {
                if (line != string.Empty && !line.Contains("ms"))
                {
                    var values = line.Split(',');
                    string metricGroupName = values[0];

                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[0], double.Parse(values[1]), "requests/second", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[1], double.Parse(values[2]), "milliSeconds", MetricRelativity.LowerIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[2], double.Parse(values[3]), "milliSeconds", MetricRelativity.LowerIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[3], double.Parse(values[4]), "milliSeconds", MetricRelativity.LowerIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[4], double.Parse(values[5]), "milliSeconds", MetricRelativity.LowerIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[5], double.Parse(values[6]), "milliSeconds", MetricRelativity.LowerIsBetter));
                    metrics.Add(new Metric(metricGroupName + "_" + metricNames[6], double.Parse(values[7]), "milliSeconds", MetricRelativity.LowerIsBetter));

                }
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @$"""", string.Empty);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            string[] metricNames =
            {
                "PING_INLINE", "PING_MBULK", "SET", "GET", "INCR", "LPUSH", "RPUSH", "LPOP", "RPOP", "SADD",
                "HSET", "SPOP", "ZADD", "ZPOPMIN", "LPUSH (needed to benchmark LRANGE)", "LRANGE_100 (first 100 elements)",
                "LRANGE_300 (first 300 elements)", "LRANGE_500 (first 450 elements)", "LRANGE_600 (first 600 elements)", "MSET (10 keys)"
            };

            if (this.RawText == string.Empty || this.RawText == null || !metricNames.All(substring => this.RawText.Contains(substring, StringComparison.CurrentCultureIgnoreCase)))
            {
                throw new SchemaException("The Redis output file has incorrect format for parsing");
            }
        }
    }
}
