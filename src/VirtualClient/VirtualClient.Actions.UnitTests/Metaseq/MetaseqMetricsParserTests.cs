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
    public class MetaseqMetricsParserTests
    {
        private string rawText;
        private MetaseqMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Metaseq");
            }
        }

        [Test]
        [TestCase("Example_bert_accuracy_summary2.json")]
        [TestCase("Example_bert_perf_harness_summary2.json")]
        public void MetaseqParserHandlesFilesThatHasNoValidValues(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
                this.testParser = new MetaseqMetricsParser(this.rawText, true);
            else
                this.testParser = new MetaseqMetricsParser(this.rawText, false);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(0, metrics.Count);
        }

        [Test]
        [TestCase("Example_bert_accuracy_summary1.json")]
        [TestCase("Example_bert_perf_harness_summary1.json")]
        public void MetaseqParserVerifyMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
            {
                this.testParser = new MetaseqMetricsParser(this.rawText, true);
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
                this.testParser = new MetaseqMetricsParser(this.rawText, false);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(4, metrics.Count);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-Server-PerformanceMode", 0, "VALID/INVALID");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-Server-result_scheduled_samples_per_sec", 4751.78);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-SingleStream-PerformanceMode", 1, "VALID/INVALID");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-SingleStream-result_90.00_percentile_latency_ns", 2202969);
            }
        }
    }
}