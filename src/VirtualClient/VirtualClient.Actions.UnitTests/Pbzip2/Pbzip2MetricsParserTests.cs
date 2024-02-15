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
    class Pbzip2MetricsParserTests
    {
        private string rawText;
        private Pbzip2MetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Pbzip2\Pbzip2ResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Pbzip2ResultsParserParsesTheExpectedSizesAndTimeFromResults(bool compression)
        {
            this.testParser = new Pbzip2MetricsParser(this.rawText, compression);
            this.testParser.Parse();
            this.testParser.SizeAndTime.PrintDataTableFormatted();
            Assert.AreEqual(4, this.testParser.SizeAndTime.Columns.Count);
            Assert.AreEqual(25, this.testParser.SizeAndTime.Rows.Count);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Pbzip2ResultsParserCreatesTheExpectedMetricsFromResults(bool compression)
        {
            this.testParser = new Pbzip2MetricsParser(this.rawText, compression);
            IList<Metric> metrics = this.testParser.Parse();
            if (compression)
            {
                MetricAssert.Exists(metrics, "Compressed size and Original size ratio", 25.746507313581135);
            }
            else
            {
                MetricAssert.Exists(metrics, "Decompressed size and Original size ratio", 25.746507313581135);
            }

            MetricAssert.Exists(metrics, "CompressionTime", 2.722705, "seconds");
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void Pbzip2ResultsParserThrowsWhenInvalidResultsAreProvided(bool compression)
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectPbzip2outputPath = Path.Combine(workingDirectory, @"Examples\Pbzip2\Pbzip2ResultsInvalidExample.txt");
            this.rawText = File.ReadAllText(IncorrectPbzip2outputPath);
            this.testParser = new Pbzip2MetricsParser(this.rawText, compression);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The Pbzip2 results file has incorrect format for parsing", exception.Message);
        }
    }
}
