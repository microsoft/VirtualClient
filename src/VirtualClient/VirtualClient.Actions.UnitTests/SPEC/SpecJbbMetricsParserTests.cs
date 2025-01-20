using System.Collections.Generic;
using System.IO;
using System.Reflection;
using global::VirtualClient.Contracts;
using NUnit.Framework;
using VirtualClient;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class SpecJbbMetricsParserTests
    {
        private string rawText;
        private SpecJbbMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "SPECjbb", "specjbb2015-C-20220301-00002", "report-00001", "logs");
            }
        }

        [Test]
        public void SpecJbbParserVerifyMetricsFpRate()
        {
            string outputPath = Path.Combine(this.ExamplePath, "specjbb2015-C-20220301-00002-reporter.out");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecJbbMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "hbIR (max attempted)", 4222, "jOPS");
            MetricAssert.Exists(metrics, "hbIR (settled)", 4123, "jOPS");
            MetricAssert.Exists(metrics, "max-jOPS", 4188, "jOPS");
            MetricAssert.Exists(metrics, "critical-jOPS", 1666, "jOPS");
        }


        [Test]
        public void SpecJbbParserVerifyNanMetricsFpRate()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SPECjbb", "specjbbNanOutput1.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecJbbMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "hbIR (max attempted)", 304872, "jOPS");
            MetricAssert.Exists(metrics, "hbIR (settled)", null, "jOPS");
            MetricAssert.Exists(metrics, "max-jOPS", 234751, "jOPS");
            MetricAssert.Exists(metrics, "critical-jOPS", null , "jOPS");
        }
    }
}