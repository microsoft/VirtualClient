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
        public void MLPerfParserVerifyValidPerformanceMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, false);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "PerformanceMode", 1, "VALID/INVALID");
            MetricAssert.Exists(metrics, "result_completed_samples_per_sec", 25405.6);
        }

        [Test]
        [TestCase("Example_performance_summary2.json")]
        public void MLPerfParserVerifyInvalidPerformanceMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, false);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "PerformanceMode", 0, "VALID/INVALID");
            MetricAssert.Exists(metrics, "result_90.00_percentile_latency_ns", 1924537);
        }

        [Test]
        [TestCase("Example_accuracy_summary1.json")]
        public void MLPerfParserVerifyPassedPerformanceMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "AccuracyMode", 1, "PASS/FAIL");
            MetricAssert.Exists(metrics, "ThresholdValue", 89.96526);
            MetricAssert.Exists(metrics, "AccuracyValue", 90.2147015680108);
            MetricAssert.Exists(metrics, "Accuracy Threshold Ratio", 1.00277264321818);
        }

        [Test]
        [TestCase("Example_accuracy_summary2.json")]
        public void MLPerfParserVerifyFailedPerformanceMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "AccuracyMode", 0, "PASS/FAIL");
            MetricAssert.Exists(metrics, "ThresholdValue", 1.0);
            MetricAssert.Exists(metrics, "AccuracyValue", 1.5);
            MetricAssert.Exists(metrics, "Accuracy Threshold Ratio", 1.5);
        }
    }
}