// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class KafkaConsumerParserUnitTests
    {
        private string rawText;
        private KafkaConsumerMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            this.rawText = MockFixture.ReadTestExamplesFile("Kafka", "KafkaConsumerResultExample.txt");
            this.testParser = new KafkaConsumerMetricsParser(this.rawText);
        }

        [Test]
        public void KafkaConsumerMetricsParserParsesTheExpectedMetricsFromResultsCorrectly()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(7, metrics.Count);

            MetricAssert.Exists(metrics, "data_consumed_in_megabyte", 476.8396, MetricUnit.Megabytes);
            MetricAssert.Exists(metrics, "megabyte_per_sec_throughput", 70.3718, MetricUnit.MegabytesPerSecond);
            MetricAssert.Exists(metrics, "data_consumed_in_nMsg", 5000026, MetricUnit.Operations);
            MetricAssert.Exists(metrics, "nMsg_per_sec_throughput", 737902.3022, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "fetch_time_in_millisec", 6073, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "fetch_megabyte_per_sec", 78.5180, MetricUnit.MegabytesPerSecond);
            MetricAssert.Exists(metrics, "fetch_nMsg_per_sec", 823320.5994, MetricUnit.OperationsPerSec);
        }

        [Test]
        public void KafkaConsumerMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            if (metrics.Count != 7)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.EndsWith("data_consumed_in_megabyte")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("fetch_time_in_millisec")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void KafkaConsumerMetricsParserThrowIfInvalidOutputFormat()
        {
            this.rawText = MockFixture.ReadTestExamplesFile("Kafka", "KafkaConsumerResultExample.txt").Substring(0, 10);
            this.testParser = new KafkaConsumerMetricsParser(this.rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            Assert.IsTrue(exception.Message.StartsWith("Invalid/unpexpected format."));
        }
    }
}