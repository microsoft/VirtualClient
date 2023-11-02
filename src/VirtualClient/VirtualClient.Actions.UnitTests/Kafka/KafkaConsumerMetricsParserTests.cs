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
    public class KafkaConsumerParserUnitTests
    {
        private string rawText;
        private KafkaConsumerMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Kafka\KafkaConsumerResultExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new KafkaConsumerMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void KafkaParserConsumerResult()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(6, metrics.Count);
        }
    }
}