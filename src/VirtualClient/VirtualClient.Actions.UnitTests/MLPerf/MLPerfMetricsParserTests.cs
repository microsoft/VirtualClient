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
        [TestCase("Example_bert_accuracy_summary2.json")]
        [TestCase("Example_bert_perf_harness_summary2.json")]
        public void MLPerfParserHandlesFilesThatHasNoValidValues(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
                this.testParser = new MLPerfMetricsParser(this.rawText, true);
            else
                this.testParser = new MLPerfMetricsParser(this.rawText, false);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(0, metrics.Count);
        }

        [Test]
        [TestCase("Example_bert_accuracy_summary1.json")]
        [TestCase("Example_bert_perf_harness_summary1.json")]
        public void MLPerfParserVerifyMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (exampleFile.Contains("accuracy"))
            {
                this.testParser = new MLPerfMetricsParser(this.rawText, true);
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
                this.testParser = new MLPerfMetricsParser(this.rawText, false);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(4, metrics.Count);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-Server-PerformanceMode", 0, "VALID/INVALID");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-Server-result_scheduled_samples_per_sec", 4751.78);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-SingleStream-PerformanceMode", 1, "VALID/INVALID");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-SingleStream-result_90.00_percentile_latency_ns", 2202969);
            }
        }

        [Test]
        [TestCase("Example_bert_server_performance_summary.json")]
        public void MLPerfParserVerifyValidBertServerPerformanceMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            this.testParser = new MLPerfMetricsParser(this.rawText, false);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-PerformanceMode", 1, "VALID/INVALID");
            MetricAssert.Exists(metrics, "DGX-A100_A100-SXM4-40GBx8_TRT-custom_k_99_MaxP-Server-result_completed_samples_per_sec", 25405.6);
        }
    }
}