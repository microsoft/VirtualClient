// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using VirtualClient.Contracts;
    using NUnit.Framework;
    using VirtualClient;

    [TestFixture]
    [Category("Unit")]
    internal class StreamResultsParserUnitTests
    {
        private string rawText;
        private StreamMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Stream");
            }
        }

        [Test]
        public void StreamResultsParserVerifyMetrics()
        {
            string outputPath = Path.Combine(this.ExamplePath, "StreamExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StreamMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "Best Rate Copy", 18514.5, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Scale", 18333.8, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Add", 23043.7, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Triad", 23314, "MBps");
        }

        [Test]
        public void StreamResultParserThrowsOnInvalidOutputFormat()
        {
            string invalidOutputPath = Path.Combine(this.ExamplePath, "StreamInvalidExample.txt");
            string rawText = File.ReadAllText(invalidOutputPath);
            this.testParser = new StreamMetricsParser(rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("incorrect format/data for parsing", exception.Message);
        }

        [Test]
        public void StreamResultParserThrowsOnInvalidMetricsCount()
        {
            string invalidMetricsCountOutputPath = Path.Combine(this.ExamplePath, "StreamInvalidMetricCountExample.txt");
            string rawText = File.ReadAllText(invalidMetricsCountOutputPath);
            this.testParser = new StreamMetricsParser(rawText);

            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("incorrect format/data for parsing", exception.Message);
        }

        [Test]
        public void StreamResultsParserVerifyMetricsForWindowsFormat()
        {
            string outputPath = Path.Combine(this.ExamplePath, "StreamExampleWindows.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StreamMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "Best Rate Copy", 42890.7, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Scale", 42678.9, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Add", 44234.6, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Triad", 44512.3, "MBps");
        }
    }
}