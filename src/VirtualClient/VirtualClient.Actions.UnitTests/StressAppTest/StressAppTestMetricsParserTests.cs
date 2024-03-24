// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;
using VirtualClient.Common.Contracts;
using VirtualClient.Actions;

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class StressAppTestMetricsParserTests
    {
        private StressAppTestMetricsParser testParser;
        string workingDirectory;

        [SetUp]
        public void Setup()
        {
            this.workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [Test]
        public void StressAppTestParserVerifyMetricsForPassResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "StressAppTest", "stressAppTestLog_pass.txt");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new StressAppTestMetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "hardwareErrorCount", 0);
        }

        [Test]
        public void StressAppTestParserVerifyMetricsForFailedResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "StressAppTest", "StressAppTestLog_forcedErrors.txt");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new StressAppTestMetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "hardwareErrorCount", 15);
        }
    }
}
