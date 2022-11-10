using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;
using VirtualClient.Common.Contracts;
using VirtualClient.Actions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class Prime95MetricsParserTests
    { 
        private Prime95MetricsParser testParser;
        string workingDirectory;

        [SetUp]
        public void Setup()
        {
            this.workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);            
        }

        [Test]
        public void Prime95ParserVerifyMetricsForPassResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "Prime95", "prime95_results_example_pass.txt");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new Prime95MetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "passTestCount", 200);
            MetricAssert.Exists(metrics, "failTestCount", 0);
        }

        [Test]
        public void Prime95ParserVerifyMetricsForFailedResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "Prime95", "prime95_results_example_fail.txt");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new Prime95MetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "passTestCount", 7);
            MetricAssert.Exists(metrics, "failTestCount", 3);
        }
    }
}
