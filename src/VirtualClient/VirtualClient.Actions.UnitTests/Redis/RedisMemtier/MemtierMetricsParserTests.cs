using VirtualClient.Common.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Actions;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MemtierMetricsParserTests
    {
        private string rawText;
        private MemtierMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Redis\redis-memtier-results.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MemtierMetricsParser(this.rawText);
        }

        [Test]
        public void RedisParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

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
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectRedisoutputPath = Path.Combine(workingDirectory, @"Examples\Redis\RedisIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectRedisoutputPath);
            this.testParser = new MemtierMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Redis Memtier output has incorrect format for parsing", exception.Message);
        }
    }
}
