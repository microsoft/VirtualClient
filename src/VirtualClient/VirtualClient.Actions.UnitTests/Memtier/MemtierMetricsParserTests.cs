// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemtierMetricsParserTests
    {
        [Test]
        public void RedisParserVerifyMetrics()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Redis\redis-memtier-results.txt"));
            var parser = new MemtierBenchmarkMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(7, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput", 355987.03, "req/sec");
            MetricAssert.Exists(metrics, "Throughput_1", 355987.03, "req/sec");
            MetricAssert.Exists(metrics, "P50lat", 0.33500, "msec");
            MetricAssert.Exists(metrics, "P90lat", 0.47900, "msec");
            MetricAssert.Exists(metrics, "P95lat", 0.55900, "msec");
            MetricAssert.Exists(metrics, "P99lat", 0.83900, "msec");
            MetricAssert.Exists(metrics, "P99_9lat", 1.54300, "msec");
        }

        [Test]
        public void RedisMemtierDetailedParserThrowIfInvalidOutputFormat()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Redis\RedisIncorrectResultsExample.txt"));
            var parser = new MemtierBenchmarkMetricsParser(results);
            SchemaException exception = Assert.Throws<SchemaException>(() => parser.Parse());
        }


        [Test]
        public void MemcachedMemtierParserParsesTheExpectedMetricsCorrectly()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memcached\MemcachedExample.txt"));
            var parser = new MemtierBenchmarkMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(10, metrics.Count);
            MetricAssert.Exists(metrics, "throughput_1", 48271.29, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "throughput_2", 47909.78, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "throughput_3", 47062.77, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "throughput_4", 48923.52, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "throughput", 192167.36, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "p50lat", 2.815, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "p90lat", 4.047, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "p95lat", 4.543, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "p99lat", 7.999, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "p99.9lat", 35.071, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemcachedMemtierParserThrowIfInvalidResultsAreProvided()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memcached\MemcachedExample.txt"));
            var parser = new MemtierBenchmarkMetricsParser(results);

            SchemaException exception = Assert.Throws<SchemaException>(() => parser.Parse());
            StringAssert.Contains("Invalid Memcached Memtier results format.", exception.Message);
        }
    }
}
