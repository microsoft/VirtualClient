using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Common.Contracts;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MemcachedMemtierMetricsParserTests
    {
        private string rawText;
        private MemcachedMemtierMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Memcached");
            }
        }

        [Test]
        public void MemcachedMemtierParserParsesTheExpectedMetricsCorrectly()
        {
            string outputPath = Path.Combine(this.ExamplePath, "MemcachedExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MemcachedMemtierMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

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
            string outputPath = Path.Combine(this.ExamplePath, "MemcachedInvalidExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MemcachedMemtierMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("Invalid Memcached Memtier results format.", exception.Message);
        }
    }
}
