// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    [Category("Unit")]
    public class KafkaProducerParserUnitTests
    {
        private string rawText;
        private KafkaProducerMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Kafka\KafkaProducerResultExample.txt");
            this.rawText = File.ReadAllText(outputPath);
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
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Kafka\KafkaProducerResultExample.txt");
            this.rawText = File.ReadAllText(outputPath).Substring(0, 10);
            this.testParser = new KafkaProducerMetricsParser(this.rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            Assert.IsTrue(exception.Message.StartsWith("Invalid/unpexpected format."));
        }
    }
}