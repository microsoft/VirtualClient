// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class NASParallelBenchResultsParserUnitTests
    {
        private string rawText;
        private NASParallelBenchMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "NASParallelBench");
            }
        }

        [Test]
        public void NASParallelBenchMetricsParserVerifyMetricsForMPI()
        {
            string outputPath = Path.Combine(this.ExamplePath, "NASParallelBenchMPIExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new NASParallelBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "ExecutionTime", 0.05, "Seconds");
            MetricAssert.Exists(metrics, "Mop/s total", 3873.74, "Mop/s");
            MetricAssert.Exists(metrics, "Mop/s/process", 1936.87, "Mop/s");
        }

        [Test]
        public void NASParallelBenchMetricsParserVerifyMetricsForOMP()
        {
            string outputPath = Path.Combine(this.ExamplePath, "NASParallelBenchOMPExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new NASParallelBenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "ExecutionTime", 0.03, "Seconds");
            MetricAssert.Exists(metrics, "Mop/s total", 7973.99, "Mop/s");
            MetricAssert.Exists(metrics, "Mop/s/thread", 1993.50, "Mop/s");
        }
    }
}
