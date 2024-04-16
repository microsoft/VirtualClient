using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Actions.MLPerf;
using VirtualClient.Contracts;
using VirtualClient.Contracts.Parser;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MLPerfSummaryMetricsParserTests
    {
        private string rawText;
        private MLPerfSummaryMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "MLPerf");
            }
        }

        [Test]
        [TestCase("Example_bert_accuracy_summary2.json")]
        [TestCase("Example_bert_perf_harness_summary2.json")]
        public void MLPerfParserHandlesFilesThatHasNoValidValues(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
                this.testParser = new MLPerfSummaryMetricsParser(this.rawText, true);
            else
                this.testParser = new MLPerfSummaryMetricsParser(this.rawText, false);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(0, metrics.Count);
        }

        [Test]
        [TestCase("Example_bert_perf_summary.txt")]
        public void MLPerfParserVerifyMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
            {
                this.testParser = new MLPerfSummaryMetricsParser(this.rawText, true);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(12, metrics.Count);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Server-AccuracyMode", 1, "PASS/FAIL");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Server-ThresholdValue", 90.783);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Server-AccuracyValue", 91.873);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Server-Accuracy Threshold Ratio", 1.0120066532280274);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-SingleStream-AccuracyMode", 1, "PASS/FAIL");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-SingleStream-ThresholdValue", 90.783);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-SingleStream-AccuracyValue", 91.568);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-SingleStream-Accuracy Threshold Ratio", 1.0086469933798177);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Offline-AccuracyMode", 0, "PASS/FAIL");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Offline-ThresholdValue", 90.783);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Offline-AccuracyValue", 90.723);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Offline-Accuracy Threshold Ratio", 0.99933908330854893);
            }
            else
            {
                this.testParser = new MLPerfSummaryMetricsParser(this.rawText, false);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(12, metrics.Count);
                MetricAssert.Exists(metrics, "Min latency", 12780892533, "nanoseconds");
                MetricAssert.Exists(metrics, "Max latency", 696786809471, "nanoseconds");
                MetricAssert.Exists(metrics, "Mean latency", 5354648122, "nanoseconds");
                MetricAssert.Exists(metrics, "50.00 percentile latency", 459037897500, "nanoseconds");
                MetricAssert.Exists(metrics, "90.00 percentile latency", 665504372761, "nanoseconds");
                MetricAssert.Exists(metrics, "95.00 percentile latency", 683827241560, "nanoseconds");
                MetricAssert.Exists(metrics, "97.00 percentile latency", 689936998165, "nanoseconds");
                MetricAssert.Exists(metrics, "99.00 percentile latency", 694892889725, "nanoseconds");
                MetricAssert.Exists(metrics, "99.90 percentile latency", 696624066783, "nanoseconds");
                MetricAssert.Exists(metrics, "Warnings", 8, "count");
                MetricAssert.Exists(metrics, "Errors", 0, "count");
                MetricAssert.Exists(metrics, "Samples Per Second", 62136.7);
            }
        }
    }
}