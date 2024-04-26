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
    public class KafkaConsumerParserUnitTests
    {
        private string rawText;
        private KafkaConsumerMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Kafka", "KafkaConsumerResultExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new KafkaConsumerMetricsParser(this.rawText);
        }

        [Test]
        public void KafkaConsumerMetricsParserParsesTheExpectedMetricsFromResultsCorrectly()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(7, metrics.Count);

            MetricAssert.Exists(metrics, "Data_Consumed_In_Mb", 476.8396, MetricUnit.Megabytes);
            MetricAssert.Exists(metrics, "Mb_Per_Sec_Throughput", 70.3718, MetricUnit.MegabytesPerSecond);
            MetricAssert.Exists(metrics, "Data_Consumed_In_nMsg", 5000026, MetricUnit.Operations);
            MetricAssert.Exists(metrics, "nMsg_Per_Sec_Throughput", 737902.3022, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "Fetch_Time_In_MilliSec", 6073, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Fetch_Mb_Per_Sec", 78.5180, MetricUnit.MegabytesPerSecond);
            MetricAssert.Exists(metrics, "Fetch_NMsg_Per_Sec", 823320.5994, MetricUnit.OperationsPerSec);
        }

        [Test]
        public void KafkaConsumerMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            if (metrics.Count != 7)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.EndsWith("Data_Consumed_In_Mb")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Fetch_Time_In_MilliSec")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void KafkaConsumerMetricsParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Kafka", "KafkaConsumerResultExample.txt");
            this.rawText = File.ReadAllText(outputPath).Substring(0,10);
            this.testParser = new KafkaConsumerMetricsParser(this.rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse()); 
            Assert.IsTrue(exception.Message.StartsWith("Invalid/unpexpected format."));
        }
    }
}