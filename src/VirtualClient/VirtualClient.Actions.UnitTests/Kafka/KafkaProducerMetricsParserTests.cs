// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.IO;
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
        public void KafkaParserConsumerResult()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(8, metrics.Count);
        }
    }
}