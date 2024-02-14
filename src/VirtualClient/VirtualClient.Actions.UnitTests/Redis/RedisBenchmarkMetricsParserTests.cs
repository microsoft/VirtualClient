// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class RedisBenchmarkMetricsParserTests
    {
        [Test]
        public void RedisBenchmarkMetricsParserParsesTheExpectedMetricsFromResultsCorrectly_1()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(RedisBenchmarkMetricsParserTests), "Examples", @"Redis\RedisBenchmarkResults.txt"));
            var parser = new RedisBenchmarkMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(140, metrics.Count);
            MetricAssert.Exists(metrics, "PING_INLINE_Throughput", 3154.38, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-Avg", 15.758, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-Min", 0.416, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-P95", 16.335, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-P99", 27.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_INLINE_Latency-Max", 53.599, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "PING_MBULK_Throughput", 3065.04, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-Avg", 16.231, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-Min", 0.416, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-P95", 16.799, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-P99", 27.951, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "PING_MBULK_Latency-Max", 52.063, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput", 3340.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 14.829, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-Min", 0.472, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 15.927, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 16.383, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 27.951, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-Max", 39.903, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 3066.73, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 16.222, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Min", 3.832, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 16.511, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 27.983, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Max", 44.191, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "INCR_Throughput", 3127.64, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "INCR_Latency-Avg", 15.891, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "INCR_Latency-Min", 0.448, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "INCR_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "INCR_Latency-P95", 16.671, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "INCR_Latency-P99", 30.799, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "INCR_Latency-Max", 44.031, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LPUSH_Throughput", 3049.62, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Avg", 16.310, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Min", 6.744, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P50", 15.943, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P95", 17.887, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P99", 31.807, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Max", 50.495, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "RPUSH_Throughput", 3158.06, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "RPUSH_Latency-Avg", 15.736, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPUSH_Latency-Min", 0.488, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPUSH_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPUSH_Latency-P95", 16.215, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPUSH_Latency-P99", 26.639, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPUSH_Latency-Max", 44.031, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LPOP_Throughput", 3048.32, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LPOP_Latency-Avg", 16.316, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPOP_Latency-Min", 0.584, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPOP_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPOP_Latency-P95", 18.047, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPOP_Latency-P99", 31.823, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPOP_Latency-Max", 56.639, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "RPOP_Throughput", 3148.52, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "RPOP_Latency-Avg", 15.794, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPOP_Latency-Min", 0.440, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPOP_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPOP_Latency-P95", 16.239, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPOP_Latency-P99", 27.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "RPOP_Latency-Max", 52.095, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SADD_Throughput", 3104.24, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SADD_Latency-Avg", 16.005, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SADD_Latency-Min", 0.616, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SADD_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SADD_Latency-P95", 18.159, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SADD_Latency-P99", 31.839, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SADD_Latency-Max", 55.999, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "HSET_Throughput", 3056.42, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "HSET_Latency-Avg", 16.273, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "HSET_Latency-Min", 0.712, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "HSET_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "HSET_Latency-P95", 16.543, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "HSET_Latency-P99", 31.759, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "HSET_Latency-Max", 47.967, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SPOP_Throughput", 3130.28, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SPOP_Latency-Avg", 15.874, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SPOP_Latency-Min", 0.384, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SPOP_Latency-P50", 15.943, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SPOP_Latency-P95", 16.479, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SPOP_Latency-P99", 31.743, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SPOP_Latency-Max", 50.751, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "ZADD_Throughput", 3042.94, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "ZADD_Latency-Avg", 16.345, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZADD_Latency-Min", 0.632, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZADD_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZADD_Latency-P95", 17.407, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZADD_Latency-P99", 31.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZADD_Latency-Max", 44.031, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "ZPOPMIN_Throughput", 3157.16, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-Avg", 15.738, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-Min", 0.392, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-P95", 16.247, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-P99", 27.871, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "ZPOPMIN_Latency-Max", 59.263, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LPUSH_Throughput", 3096.74, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Avg", 16.063, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Min", 0.464, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P50", 15.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P95", 16.135, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-P99", 20.511, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LPUSH_Latency-Max", 55.455, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LRANGE_100_Throughput", 3115.85, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-Avg", 15.860, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-Min", 0.904, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-P50", 15.847, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-P95", 16.223, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-P99", 27.759, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_100_Latency-Max", 56.607, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LRANGE_300_Throughput", 3018.32, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-Avg", 16.066, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-Min", 0.920, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-P50", 15.551, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-P95", 19.327, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-P99", 31.599, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_300_Latency-Max", 65.791, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LRANGE_500_Throughput", 3037.57, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-Avg", 15.768, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-Min", 0.856, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-P50", 15.359, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-P95", 17.983, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-P99", 31.327, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_500_Latency-Max", 61.343, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "LRANGE_600_Throughput", 3037.85, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-Avg", 15.540, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-Min", 1.048, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-P50", 15.159, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-P95", 17.823, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-P99", 30.575, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "LRANGE_600_Latency-Max", 59.967, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "MSET_10_Throughput", 3040.53, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "MSET_10_Latency-Avg", 16.313, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "MSET_10_Latency-Min", 0.488, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "MSET_10_Latency-P50", 15.927, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "MSET_10_Latency-P95", 27.967, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "MSET_10_Latency-P99", 35.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "MSET_10_Latency-Max", 59.903, MetricUnit.Milliseconds);
        }

        [Test]
        public void RedisBenchmarkMetricsParserParsesTheExpectedMetricsFromResultsCorrectly_2()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(RedisBenchmarkMetricsParserTests), "Examples", @"Redis\RedisBenchmarkResults_2.txt"));
            var parser = new RedisBenchmarkMetricsParser(results);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(14, metrics.Count);

            MetricAssert.Exists(metrics, "SET_Throughput", 46446.82, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 34.325, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-Min", 0.504, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 31.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 58.527, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 67.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-Max", 98.687, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 46939.54, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 33.969, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Min", 7.792, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 31.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 55.967, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 67.199, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Max", 82.431, MetricUnit.Milliseconds);
        }

        [Test]
        public void RedisBenchmarkMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(RedisBenchmarkMetricsParserTests), "Examples", @"Redis\RedisBenchmarkResults.txt"));
            var parser = new RedisBenchmarkMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            if (metrics.Count != 26)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.EndsWith("Throughput")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Latency")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void RedisBenchmarkMetricsParserThrowIfInvalidOutputFormat_1()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(RedisBenchmarkMetricsParserTests), "Examples", @"Redis\RedisBenchmarkResults.txt")).Substring(0, 100); // invalid results
            var parser = new RedisBenchmarkMetricsParser(results);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }

        [Test]
        public void RedisBenchmarkMetricsParserThrowIfInvalidOutputFormat_2()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(RedisBenchmarkMetricsParserTests), "Examples", @"Redis\RedisBenchmarkResults.txt"));
            var parser = new RedisBenchmarkMetricsParser(string.Join(Environment.NewLine, results.Split(Environment.NewLine).Take(1))); // headers exist but no measurements within.

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
            Assert.IsNotNull(exception.InnerException);
            Assert.IsInstanceOf<SchemaException>(exception.InnerException);
        }
    }
}
