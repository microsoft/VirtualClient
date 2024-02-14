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
    class GzipMetricsParserTests
    {
        private string rawText;
        private GzipMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Gzip\GzipResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new GzipMetricsParser(this.rawText);
        }

        [Test]
        public void GzipResultsParserParsesTheExpectedSizesAndTimeFromResults()
        {
            this.testParser.Parse();
            this.testParser.ReductionRatio.PrintDataTableFormatted();
            Assert.AreEqual(4, this.testParser.ReductionRatio.Columns.Count);
            Assert.AreEqual(12, this.testParser.ReductionRatio.Rows.Count);
        }

        [Test]
        public void GzipResultsParserCreatesTheExpectedMetricsFromResults()
        {
            IList<Metric> metrics = this.testParser.Parse();
            MetricAssert.Exists(metrics, "ReductionRatio", 72);
            MetricAssert.Exists(metrics, "ReductionRatio", 28.8);
            MetricAssert.Exists(metrics, "ReductionRatio", 49.7);
            MetricAssert.Exists(metrics, "ReductionRatio", 26.5);
            MetricAssert.Exists(metrics, "ReductionRatio", 70.6);
            MetricAssert.Exists(metrics, "ReductionRatio", 90.5);
            MetricAssert.Exists(metrics, "ReductionRatio", 63.0);
            MetricAssert.Exists(metrics, "ReductionRatio", 87.1);
            MetricAssert.Exists(metrics, "ReductionRatio", 62.0);
            MetricAssert.Exists(metrics, "ReductionRatio", 62.9);
            MetricAssert.Exists(metrics, "ReductionRatio", 62.8);
            MetricAssert.Exists(metrics, "ReductionRatio", 74.7);
        }

        [Test]
        public void GzipResultsParserThrowsWhenInvalidResultsAreProvided()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectGzipoutputPath = Path.Combine(workingDirectory, @"Examples\Gzip\GzipResultsInvalidExample.txt");
            this.rawText = File.ReadAllText(IncorrectGzipoutputPath);
            this.testParser = new GzipMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Gzip results file has incorrect format for parsing", exception.Message);
        }
    }
}
