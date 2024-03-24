using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Extreme.Statistics;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class SockPerfMetricsParserTests
    {
        private string rawText;
        private SockPerfMetricsParser testParser;
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
        }

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "SockPerf");
            }
        }

        [Test]
        public void SockPerfParserVerifyMetricsExample1()
        {
            string outputPath = Path.Combine(this.ExamplePath, "SockPerfClientExample1.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SockPerfMetricsParser(this.rawText, 99);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(16, metrics.Count);
            MetricAssert.Exists(metrics, "Latency-Avg", 334.643949799325, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Stdev", 821.40770401562077, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Max", 15005.133, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.999", 14797.537, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.99", 13462.533, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.9", 10218.522, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99", 4370.4978666667239, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P90", 319.62000000000018, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P75", 205.8723333333333, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P50", 163.613, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P25", 139.027, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Min", 88.373, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Mad", 29.199999999999989, "microseconds");
            MetricAssert.Exists(metrics, "Latency-StandardErrorMean", 2.1478620278824772, "microseconds");
            MetricAssert.Exists(metrics, "Latency-LowerCI", 329.11142384791594);
            MetricAssert.Exists(metrics, "Latency-UpperCI", 340.17647575071521);
        }

        [Test]
        public void SockPerfParserVerifyMetricsExample2()
        {
            string outputPath = Path.Combine(this.ExamplePath, "SockPerfClientExample2.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SockPerfMetricsParser(this.rawText, 99);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(16, metrics.Count);
            MetricAssert.Exists(metrics, "Latency-Avg", 101.9265298163604, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Stdev", 25.484942000428859, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Max", 531.956, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.999", 531.956, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.99", 300, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99.9", 192.845, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P99", 180.762526666667, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P90", 123.09120000000001, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P75", 111.838, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P50", 104.528, "microseconds");
            MetricAssert.Exists(metrics, "Latency-P25", 94.464333333333329, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Min", 48.651, "microseconds");
            MetricAssert.Exists(metrics, "Latency-Mad", 8.381, "microseconds");
            MetricAssert.Exists(metrics, "Latency-StandardErrorMean", 0.20825730020697319, "microseconds");
            MetricAssert.Exists(metrics, "Latency-LowerCI", 101.39009455980919);
            MetricAssert.Exists(metrics, "Latency-UpperCI", 102.46296507291139);
        }
    }
}