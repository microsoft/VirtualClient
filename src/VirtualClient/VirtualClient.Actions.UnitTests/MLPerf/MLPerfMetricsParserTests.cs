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

                Assert.AreEqual(3, metrics.Count);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Server-AccuracyMode", 1, "PASS/FAIL");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-SingleStream-AccuracyMode", 1, "PASS/FAIL");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT-custom_k_99_9_MaxP-Offline-AccuracyMode", 0, "PASS/FAIL");
            }
            else
            {
                this.testParser = new MLPerfMetricsParser(this.rawText, false);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(2, metrics.Count);
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-Server-PerformanceMode", 0, "VALID/INVALID");
                MetricAssert.Exists(metrics, "A100-PCIe-80GBx4_TRT_Triton-triton_k_99_9_MaxP-SingleStream-PerformanceMode", 1, "VALID/INVALID");
            }
        }
    }
}