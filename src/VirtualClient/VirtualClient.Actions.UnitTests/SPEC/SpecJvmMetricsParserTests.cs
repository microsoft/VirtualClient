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
    public class SpecJvmMetricsParserTests
    {
        private string rawText;
        private SpecJvmMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "SPECjvm");
            }
        }

        [Test]
        public void SpecJvmParserHandlesMetricsThatAreNotFormattedAsNumericValues()
        {
            string outputPath = Path.Combine(this.ExamplePath, "SPECjvm2008.012.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecJvmMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // The example itself contains non-numeric values.
            // If the test doesn't throw, it means the parser handles non-numeric values.
        }

        [Test]
        public void SpecJvmParserVerifyMetricsFpRate()
        {
            string outputPath = Path.Combine(this.ExamplePath, "SPECjvm2008.012.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecJvmMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(9, metrics.Count);
            MetricAssert.Exists(metrics, "compress", 123.71, "ops/m");
            MetricAssert.Exists(metrics, "crypto", 228.47, "ops/m");
            MetricAssert.Exists(metrics, "derby", 288.43, "ops/m");
            MetricAssert.Exists(metrics, "mpegaudio", 86.42, "ops/m");
            MetricAssert.Exists(metrics, "scimark.large", 49.62, "ops/m");
            MetricAssert.Exists(metrics, "scimark.small", 197.48, "ops/m");
            MetricAssert.Exists(metrics, "serial", 90.27, "ops/m");
            MetricAssert.Exists(metrics, "sunflow", 48.02, "ops/m");
            MetricAssert.Exists(metrics, "Noncompliant composite result", 115.78, "ops/m");
        }
    }
}