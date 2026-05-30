// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VirtualClient.Contracts;
using NUnit.Framework;
using VirtualClient;

namespace VirtualClient.Actions
{

    [TestFixture]
    [Category("Unit")]
    internal class DotNetRuntimeMetricsParserUnitTests
    {
        private string rawText;
        private DotNetRuntimeMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples", "DotNetRuntimeResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new DotNetRuntimeMetricsParser(this.rawText);
        }

        [Test]
        public void DotNetRuntimeParserVerifyThroughputResult()
        {
            this.testParser.Parse();
            this.testParser.ThroughputResult.PrintDataTableFormatted();
            Assert.AreEqual(4, this.testParser.ThroughputResult.Columns.Count);
        }

        [Test]
        public void DotNetRuntimeParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();
            MetricAssert.Exists(metrics, "throughput", 11284.51, "bops");
        }

        [Test]
        public void DotNetRuntimeParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectDotNetoutputPath =Path.Combine(workingDirectory, @"Examples", "IncorrectDotNetRuntimeResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectDotNetoutputPath);
            this.testParser = new DotNetRuntimeMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The DotNetRuntime output file has incorrect format for parsing", exception.Message);
        }
    }
}