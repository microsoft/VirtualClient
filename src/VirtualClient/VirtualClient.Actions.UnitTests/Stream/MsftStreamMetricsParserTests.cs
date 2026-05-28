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
    internal class StreamMsftResultsParserUnitTests
    {
        private string rawText;
        private StreamMsftMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Stream");
            }
        }

        [Test]
        public void StreamMsftResultsParserVerifyMetricsScenarioBandwidth()
        {
            // Read: 144    141    140    113    110    114
            // Copy: 277    272    265    117    115    120
            // Scale: 275    270    268    118    117    119
            // Add: 419    412    407    115    114    116
            // Triad: 415    411    405    116    114    116
            // Write: 128    126    124    127    125    129

            string outputPath = Path.Combine(this.ExamplePath, "StreamMsftExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StreamMsftMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(36, metrics.Count);
            MetricAssert.Exists(metrics, "Best Rate Read", 144, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Scale", 275, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Add", 419, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Triad", 415, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Write", 128, "MBps");
            MetricAssert.Exists(metrics, "Best Rate Copy", 277, "MBps");

            MetricAssert.Exists(metrics, "Avg Rate Read", 141, "MBps");
            MetricAssert.Exists(metrics, "Avg Rate Scale", 270, "MBps");
            MetricAssert.Exists(metrics, "Avg Rate Add", 412, "MBps");
            MetricAssert.Exists(metrics, "Avg Rate Triad", 411, "MBps");
            MetricAssert.Exists(metrics, "Avg Rate Write", 126, "MBps");
            MetricAssert.Exists(metrics, "Avg Rate Copy", 272, "MBps");

            MetricAssert.Exists(metrics, "Min Rate Read", 140, "MBps");
            MetricAssert.Exists(metrics, "Min Rate Scale", 268, "MBps");
            MetricAssert.Exists(metrics, "Min Rate Add", 407, "MBps");
            MetricAssert.Exists(metrics, "Min Rate Triad", 405, "MBps");
            MetricAssert.Exists(metrics, "Min Rate Write", 124, "MBps");
            MetricAssert.Exists(metrics, "Min Rate Copy", 265, "MBps");

       
            MetricAssert.Exists(metrics, "Avg Latency Read", 113, "ns");
            MetricAssert.Exists(metrics, "Avg Latency Scale", 118, "ns");
            MetricAssert.Exists(metrics, "Avg Latency Add", 115, "ns");
            MetricAssert.Exists(metrics, "Avg Latency Triad", 116, "ns");
            MetricAssert.Exists(metrics, "Avg Latency Write", 127, "ns");
            MetricAssert.Exists(metrics, "Avg Latency Copy", 117, "ns");

            MetricAssert.Exists(metrics, "Min Latency Read", 110, "ns");
            MetricAssert.Exists(metrics, "Min Latency Scale", 117, "ns");
            MetricAssert.Exists(metrics, "Min Latency Add", 114, "ns");
            MetricAssert.Exists(metrics, "Min Latency Triad", 114, "ns");
            MetricAssert.Exists(metrics, "Min Latency Write", 125, "ns");
            MetricAssert.Exists(metrics, "Min Latency Copy", 115, "ns");

            MetricAssert.Exists(metrics, "Max Latency Read", 114, "ns");
            MetricAssert.Exists(metrics, "Max Latency Scale", 119, "ns");
            MetricAssert.Exists(metrics, "Max Latency Add", 116, "ns");
            MetricAssert.Exists(metrics, "Max Latency Triad", 116, "ns");
            MetricAssert.Exists(metrics, "Max Latency Write", 129, "ns");
            MetricAssert.Exists(metrics, "Max Latency Copy", 120, "ns");
        }

        [Test]
        public void StreamResultsParserVerifyMetricsScenarioLatency()
        {

            // LATENCY 108.97    108.00    111.43
            string outputPath = Path.Combine(this.ExamplePath, "StreamMsftLatencyExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StreamMsftMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();
            
            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "Avg Latency ns", 108.97, "ns");
            MetricAssert.Exists(metrics, "Min Latency ns", 108.00, "ns");
            MetricAssert.Exists(metrics, "Max Latency ns", 111.43, "ns");
            
        }

        [Test]
        public void StreamResultParserThrowsOnInvalidOutputFormat()
        {
            string InvalidOutputPath = Path.Combine(this.ExamplePath, "StreamInvalidExample.txt");
            string rawText = File.ReadAllText(InvalidOutputPath);
            this.testParser = new StreamMsftMetricsParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Stream results has incorrect format/data for parsing", exception.Message);
        }

        [Test]
        public void StreamResultParserThrowsOnInvalidMetricsCount()
        {
            string InvalidMetricsCountOutputPath = Path.Combine(this.ExamplePath, "StreamInvalidMetricCountExample.txt");
            string rawText = File.ReadAllText(InvalidMetricsCountOutputPath);
            this.testParser = new StreamMsftMetricsParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains($"The Stream results has incorrect format/data for parsing. Output is having 0 metrics.", exception.Message);
        }
    }
}