// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemtierMetricsParserTests
    {
        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromMemtierResultsCorrectly_1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_1.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(26, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth", 4053.46, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec", 43444.12);
            MetricAssert.Exists(metrics, "Misses/sec", 0);
            MetricAssert.Exists(metrics, "Latency-Avg", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 2.75100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 3.90300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 29.31100, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 2.73500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90", 3.88700, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9", 29.31100, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 2.64323, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 2.83100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90", 3.93500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 4.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 7.45500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9", 29.56700, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromMemtierResultsCorrectly_2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_2.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(26, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput", 48041.840000000004, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth", 4034.1975, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec", 43237.6225);
            MetricAssert.Exists(metrics, "Misses/sec", 0);
            MetricAssert.Exists(metrics, "Latency-Avg", 2.636375, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 2.763, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 3.931, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 29.278999999999996, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 43237.6225, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth", 3706.31, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 2.6338675, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 2.759, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90", 3.919, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 7.4149999999999991, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9", 29.214999999999996, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput", 4804.2175000000007, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth", 327.885, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 2.6589324999999997, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90", 3.959, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 4.495, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 7.551, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9", 29.598999999999997, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromRedisResultsCorrectly_1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_1.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(26, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput", 355987.03, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth", 25860.83, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec", 320388.28);
            MetricAssert.Exists(metrics, "Misses/sec", 0);
            MetricAssert.Exists(metrics, "Latency-Avg", 0.34301, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 320388.28, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth", 23118.30, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 0.34300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput", 35598.74, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth", 2742.53, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 0.34304, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9", 1.54300, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_1.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            if (metrics.Count != 26)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.EndsWith("Throughput") || m.Name.EndsWith("Bandwidth")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Latency")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void MemtierMetricsParserThrowIfInvalidResultsAreProvided()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedInvalidResults_1.txt"));
            var parser = new MemtierMetricsParser(results);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }
    }
}
