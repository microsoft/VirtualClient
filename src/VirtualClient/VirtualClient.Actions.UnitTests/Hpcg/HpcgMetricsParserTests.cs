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
    public class HpcgMetricsParserTests
    {
        private string rawText;
        private HpcgMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Hpcg");
            }
        }

        [Test]
        public void HpcgParserVerifyMetrics()
        {
            string outputPath = Path.Combine(this.ExamplePath, "HpcgExample1.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HpcgMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "Total Gflop/s", 1.94783, "Gflop/s");
        }
    }
}