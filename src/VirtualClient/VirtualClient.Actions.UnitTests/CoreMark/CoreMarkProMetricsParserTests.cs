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

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "CoreMark", "CoreMarkProExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CoreMarkProMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void CoreMarkProParserViewDataTable()
        {
            this.testParser.WorkloadResult.PrintDataTableFormatted();
            this.testParser.MarkResult.PrintDataTableFormatted();
        }

        [Test]
        public void CoreMarkProParserVerifyWorkloadMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(555.56, metrics.Where(m => m.Name == "MultiCore-cjpeg-rose7-preset").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-cjpeg-rose7-preset").FirstOrDefault().Unit);
            Assert.AreEqual(156.25, metrics.Where(m => m.Name == "SingleCore-cjpeg-rose7-preset").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-cjpeg-rose7-preset").FirstOrDefault().Unit);
            Assert.AreEqual(3.56, metrics.Where(m => m.Name == "Scaling-cjpeg-rose7-preset").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-cjpeg-rose7-preset").FirstOrDefault().Unit);

            Assert.AreEqual(4.87, metrics.Where(m => m.Name == "MultiCore-core").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-core").FirstOrDefault().Unit);
            Assert.AreEqual(1.30, metrics.Where(m => m.Name == "SingleCore-core").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-core").FirstOrDefault().Unit);
            Assert.AreEqual(3.75, metrics.Where(m => m.Name == "Scaling-core").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-core").FirstOrDefault().Unit);

            Assert.AreEqual(1428.57, metrics.Where(m => m.Name == "MultiCore-linear_alg-mid-100x100-sp").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-linear_alg-mid-100x100-sp").FirstOrDefault().Unit);
            Assert.AreEqual(409.84, metrics.Where(m => m.Name == "SingleCore-linear_alg-mid-100x100-sp").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-linear_alg-mid-100x100-sp").FirstOrDefault().Unit);
            Assert.AreEqual(3.49, metrics.Where(m => m.Name == "Scaling-linear_alg-mid-100x100-sp").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-linear_alg-mid-100x100-sp").FirstOrDefault().Unit);

            Assert.AreEqual(22.56, metrics.Where(m => m.Name == "MultiCore-loops-all-mid-10k-sp").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-loops-all-mid-10k-sp").FirstOrDefault().Unit);
            Assert.AreEqual(6.25, metrics.Where(m => m.Name == "SingleCore-loops-all-mid-10k-sp").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-loops-all-mid-10k-sp").FirstOrDefault().Unit);
            Assert.AreEqual(3.61, metrics.Where(m => m.Name == "Scaling-loops-all-mid-10k-sp").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-loops-all-mid-10k-sp").FirstOrDefault().Unit);

            Assert.AreEqual(33.22, metrics.Where(m => m.Name == "MultiCore-nnet_test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-nnet_test").FirstOrDefault().Unit);
            Assert.AreEqual(10.56, metrics.Where(m => m.Name == "SingleCore-nnet_test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-nnet_test").FirstOrDefault().Unit);
            Assert.AreEqual(3.15, metrics.Where(m => m.Name == "Scaling-nnet_test").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-nnet_test").FirstOrDefault().Unit);

            Assert.AreEqual(70.18, metrics.Where(m => m.Name == "MultiCore-parser-125k").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-parser-125k").FirstOrDefault().Unit);
            Assert.AreEqual(19.23, metrics.Where(m => m.Name == "SingleCore-parser-125k").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-parser-125k").FirstOrDefault().Unit);
            Assert.AreEqual(3.65, metrics.Where(m => m.Name == "Scaling-parser-125k").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-parser-125k").FirstOrDefault().Unit);

            Assert.AreEqual(1666.67, metrics.Where(m => m.Name == "MultiCore-radix2-big-64k").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-radix2-big-64k").FirstOrDefault().Unit);
            Assert.AreEqual(453.72, metrics.Where(m => m.Name == "SingleCore-radix2-big-64k").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-radix2-big-64k").FirstOrDefault().Unit);
            Assert.AreEqual(3.67, metrics.Where(m => m.Name == "Scaling-radix2-big-64k").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-radix2-big-64k").FirstOrDefault().Unit);

            Assert.AreEqual(588.24, metrics.Where(m => m.Name == "MultiCore-sha-test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-sha-test").FirstOrDefault().Unit);
            Assert.AreEqual(172.41, metrics.Where(m => m.Name == "SingleCore-sha-test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-sha-test").FirstOrDefault().Unit);
            Assert.AreEqual(3.41, metrics.Where(m => m.Name == "Scaling-sha-test").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-sha-test").FirstOrDefault().Unit);

            Assert.AreEqual(500, metrics.Where(m => m.Name == "MultiCore-zip-test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "MultiCore-zip-test").FirstOrDefault().Unit);
            Assert.AreEqual(142.86, metrics.Where(m => m.Name == "SingleCore-zip-test").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "SingleCore-zip-test").FirstOrDefault().Unit);
            Assert.AreEqual(3.50, metrics.Where(m => m.Name == "Scaling-zip-test").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-zip-test").FirstOrDefault().Unit);
        }

        [Test]
        public void CoreMarkProParserVerifyMarkResultMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            // 9 Workloads with 3 metrics each plus the result of 3 metrics. 9*3+3=30
            Assert.AreEqual(30, metrics.Count);

            Assert.AreEqual(19183.84, metrics.Where(m => m.Name == "MultiCore-CoreMark-PRO").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "MultiCore-CoreMark-PRO").FirstOrDefault().Unit);
            Assert.AreEqual(5439.59, metrics.Where(m => m.Name == "SingleCore-CoreMark-PRO").FirstOrDefault().Value);
            Assert.AreEqual("Score", metrics.Where(m => m.Name == "SingleCore-CoreMark-PRO").FirstOrDefault().Unit);
            Assert.AreEqual(3.53, metrics.Where(m => m.Name == "Scaling-CoreMark-PRO").FirstOrDefault().Value);
            Assert.AreEqual("scale", metrics.Where(m => m.Name == "Scaling-CoreMark-PRO").FirstOrDefault().Unit);
        }
    }
}