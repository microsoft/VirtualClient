using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MLPerfMetricsParserTests
    {
        private string rawText;
        private MLPerfMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "MLPerf");
            }
        }

        [Test]
        [TestCase("Example_performance_summary1.json")]
        public void MLPerfParserVerifyValidMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, false);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "PerformanceMode_p99", 1, "VALID/INVALID");
            MetricAssert.Exists(metrics, "samples_per_second_p99", 25405.6);
        }

        [Test]
        [TestCase("Example_performance_summary2.json")]
        public void MLPerfParserVerifyInvalidMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, false);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "PerformanceMode_p99", 0, "VALID/INVALID");
            MetricAssert.Exists(metrics, "latency_ns_p99", 1924537);
        }

        [Test]
        [TestCase("Example_accuracy_summary1.json")]
        public void MLPerfParserVerifyPassedMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "AccuracyMode_p99", 1, "PASS/FAIL");
            MetricAssert.Exists(metrics, "ThresholdValue_p99", 89.96526);
            MetricAssert.Exists(metrics, "AccuracyValue_p99", 90.2147015680108);
            MetricAssert.Exists(metrics, "AccuracyThresholdRatio_p99", 1.00277264321818);
        }

        [Test]
        [TestCase("Example_accuracy_summary2.json")]
        public void MLPerfParserVerifyFailedMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "AccuracyMode_p99", 0, "PASS/FAIL");
            MetricAssert.Exists(metrics, "ThresholdValue_p99", 1.0);
            MetricAssert.Exists(metrics, "AccuracyValue_p99", 1.5);
            MetricAssert.Exists(metrics, "AccuracyThresholdRatio_p99", 1.5);
        }
    }
}