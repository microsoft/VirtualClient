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
    public class KafkaProducerParserUnitTests
    {
        private string rawText;
        private KafkaProducerMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            this.rawText = MockFixture.ReadTestExamplesFile("Kafka", "KafkaProducerResultExample.txt");
            this.testParser = new KafkaProducerMetricsParser(this.rawText);
        }

        [Test]
        public void KafkaProducerMetricsParserParsesTheExpectedMetricsFromResultsCorrectly()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(8, metrics.Count);

            MetricAssert.Exists(metrics, "Total_Records_Sent", 5000000, MetricUnit.Operations);
            MetricAssert.Exists(metrics, "Records_Per_Sec", 175358.608354, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "Latency-Avg", 1333.88, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Max", 3687.00, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 1514, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 3147, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 3480, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 3657, MetricUnit.Milliseconds);
        }

        [Test]
        public void KafkaProducerMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            if (metrics.Count != 8)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.EndsWith("Records_Per_Sec")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Latency-Avg")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void KafkaProducerMetricsParserThrowIfInvalidOutputFormat()
        {
            this.rawText = MockFixture.ReadTestExamplesFile("Kafka", "KafkaProducerResultExample.txt").Substring(0, 10);
            this.testParser = new KafkaProducerMetricsParser(this.rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            Assert.IsTrue(exception.Message.StartsWith("Invalid/unpexpected format."));
        }
    }
}