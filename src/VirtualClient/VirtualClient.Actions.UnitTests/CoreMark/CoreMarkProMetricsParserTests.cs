using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Actions;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class CoreMarkProMetricsParserTests
    {
        private string rawText;
        private CoreMarkProMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "CoreMark");
            }
        }

        [Test]
        public void CoreMarkProParserVerifyWorkloadMetrics()
        {
            string outputPath = Path.Combine(this.ExamplePath, "CoreMarkProExample2.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CoreMarkProMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // 9 Workloads with 3 metrics each plus the result of 3 summary metrics. 9*3+3=30
            Assert.AreEqual(30, metrics.Count);

            MetricAssert.Exists(metrics, "MultiCore-cjpeg-rose7-preset", 555.56, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-cjpeg-rose7-preset", 156.25, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-cjpeg-rose7-preset", 3.56, "scale");

            MetricAssert.Exists(metrics, "MultiCore-core", 4.87, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-core", 1.30, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-core", 3.75, "scale");

            MetricAssert.Exists(metrics, "MultiCore-linear_alg-mid-100x100-sp", 1428.57, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-linear_alg-mid-100x100-sp", 409.84, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-linear_alg-mid-100x100-sp", 3.49, "scale");

            MetricAssert.Exists(metrics, "MultiCore-loops-all-mid-10k-sp", 22.56, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-loops-all-mid-10k-sp", 6.25, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-loops-all-mid-10k-sp", 3.61, "scale");

            MetricAssert.Exists(metrics, "MultiCore-nnet_test", 33.22, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-nnet_test", 10.56, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-nnet_test", 3.15, "scale");

            MetricAssert.Exists(metrics, "MultiCore-parser-125k", 70.18, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-parser-125k", 19.23, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-parser-125k", 3.65, "scale");

            MetricAssert.Exists(metrics, "MultiCore-radix2-big-64k", 1666.67, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-radix2-big-64k", 453.72, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-radix2-big-64k", 3.67, "scale");

            MetricAssert.Exists(metrics, "MultiCore-sha-test", 588.24, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-sha-test", 172.41, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-sha-test", 3.41, "scale");

            MetricAssert.Exists(metrics, "MultiCore-zip-test", 500, "iterations/sec");
            MetricAssert.Exists(metrics, "SingleCore-zip-test", 142.86, "iterations/sec");
            MetricAssert.Exists(metrics, "Scaling-zip-test", 3.50, "scale");

            MetricAssert.Exists(metrics, "MultiCore-CoreMark-PRO", 19183.84, "Score");
            MetricAssert.Exists(metrics, "SingleCore-CoreMark-PRO", 5439.59, "Score");
            MetricAssert.Exists(metrics, "Scaling-CoreMark-PRO", 3.53, "scale");
        }
    }
}