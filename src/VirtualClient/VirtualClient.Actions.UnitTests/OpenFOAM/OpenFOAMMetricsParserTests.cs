using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Common.Contracts;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class OpenFOAMMetricsParserTests
    {
        private string rawText;
        private OpenFOAMMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "OpenFOAM", "OpenFOAMResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new OpenFOAMMetricsParser(this.rawText);
        }

        [Test]
        public void OpenFOAMResultsParserParsesTheExpectedExecutionTimesFromResults()
        {
            this.testParser.Parse();
            Assert.AreEqual(4, this.testParser.ExecutionTimes.Columns.Count);
            Assert.AreEqual(313, this.testParser.ExecutionTimes.Rows.Count);
        }

        [Test]
        public void OpenFOAMResultsParserCreatesTheExpectedMetricsFromResults()
        {
            IList<Metric> metrics = this.testParser.Parse();
            MetricAssert.Exists(metrics, "Iterations/min", 1708.8262056414922, "itrs/min");
        }

        [Test]
        public void OpenFOAMResultsParserThrowsWhenInvalidResultsAreProvided()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string incorrectOpenFOAMoutputPath = Path.Combine(workingDirectory, "Examples", "OpenFOAM", "OpenFOAMResultsInvalidExample.txt");

            this.rawText = File.ReadAllText(incorrectOpenFOAMoutputPath);
            this.testParser = new OpenFOAMMetricsParser(this.rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
            StringAssert.Contains("Failed to parse OpenFOAM metrics from results", exception.Message);
        }
    }
}
