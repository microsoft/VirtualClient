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
    class Compressor7zipMetricsParserTests
    {
        private string rawText;
        private Compression7zipMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "Compressor7zip", "Compressor7zipResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new Compression7zipMetricsParser(this.rawText);
        }

        [Test]
        public void Compressor7zipResultsParserParsesTheExpectedSizesAndTimeFromResults()
        {
            this.testParser.Parse();
            this.testParser.SizeAndTime.PrintDataTableFormatted();
            Assert.AreEqual(4, this.testParser.SizeAndTime.Columns.Count);
            Assert.AreEqual(7, this.testParser.SizeAndTime.Rows.Count);
        }

        [Test]
        public void Compressor7zipResultsParserCreatesTheExpectedMetricsFromResults()
        {
            IList<Metric> metrics = this.testParser.Parse();
            MetricAssert.Exists(metrics, "Compression_Ratio", 26.03795395904138);
            MetricAssert.Exists(metrics, "Compression_Time", 53.223, "seconds");
        }

        [Test]
        public void Compressor7zipResultsParserThrowsWhenInvalidResultsAreProvided()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string incorrectCompressor7zipoutputPath = Path.Combine(workingDirectory, "Examples", "Compressor7zip", "Compressor7zipResultsInvalidExample.txt");
            this.rawText = File.ReadAllText(incorrectCompressor7zipoutputPath);
            this.testParser = new Compression7zipMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Compressor7zip results file has incorrect data for parsing", exception.Message);
        }
    }
}
