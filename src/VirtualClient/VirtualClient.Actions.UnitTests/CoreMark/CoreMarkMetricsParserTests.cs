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
    public class CoreMarkMetricsParserTests
    {
        private string rawText;
        private CoreMarkMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "CoreMark", "CoreMarkExampleSingleThread.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CoreMarkMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void CoreMarkParserViewDataTable()
        {
            this.testParser.CoreMarkResult.PrintDataTableFormatted();
        }

        [Test]
        public void CoreMarkParserVerifyMetricsSingleThread()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "CoreMark", "CoreMarkExampleSingleThread.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CoreMarkMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(5, metrics.Count);
            Assert.AreEqual(666, metrics.Where(m => m.Name == "CoreMark Size").FirstOrDefault().Value);
            Assert.AreEqual("bytes", metrics.Where(m => m.Name == "CoreMark Size").FirstOrDefault().Unit);
            Assert.AreEqual(14634, metrics.Where(m => m.Name == "Total ticks").FirstOrDefault().Value);
            Assert.AreEqual("ticks", metrics.Where(m => m.Name == "Total ticks").FirstOrDefault().Unit);
            Assert.AreEqual(14.634, metrics.Where(m => m.Name == "Total time (secs)").FirstOrDefault().Value);
            Assert.AreEqual("secs", metrics.Where(m => m.Name == "Total time (secs)").FirstOrDefault().Unit);
            Assert.AreEqual(20500.205002, metrics.Where(m => m.Name == "Iterations/Sec").FirstOrDefault().Value);
            Assert.AreEqual("iterations/sec", metrics.Where(m => m.Name == "Iterations/Sec").FirstOrDefault().Unit);
            Assert.AreEqual(300000, metrics.Where(m => m.Name == "Iterations").FirstOrDefault().Value);
            Assert.AreEqual("iterations", metrics.Where(m => m.Name == "Iterations").FirstOrDefault().Unit);
        }

        [Test]
        public void CoreMarkParserVerifyMetricsMultiThread()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "CoreMark", "CoreMarkExampleMultiThread.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CoreMarkMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(6, metrics.Count);

            MetricAssert.Exists(metrics, "CoreMark Size", 666, "bytes");
            MetricAssert.Exists(metrics, "Total ticks", 28457, "ticks");
            MetricAssert.Exists(metrics, "Total time (secs)", 28.457000, "secs");
            MetricAssert.Exists(metrics, "Iterations/Sec", 42168.886390, "iterations/sec");
            MetricAssert.Exists(metrics, "Iterations", 1200000, "iterations");
            MetricAssert.Exists(metrics, "Parallel PThreads", 4, "threads");
        }
    }
}