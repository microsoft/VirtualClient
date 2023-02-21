using VirtualClient.Common.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Contracts;
using Microsoft.Identity.Client;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class HPLParserUnitTests
    {
        private string rawText;
        private HPLMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\HPL\HPLResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HPLMetricsParser(this.rawText);
        }

        [Test]
        public void HPLParserVerifyResults()
        {
            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(6, metrics.Count);
            MetricAssert.Exists(metrics, "N", 8029);
            MetricAssert.Exists(metrics, "NB", 400);
            MetricAssert.Exists(metrics, "P", 1);
            MetricAssert.Exists(metrics, "Q", 2);
            MetricAssert.Exists(metrics, "Time", 11.55);
            MetricAssert.Exists(metrics, "Gflops", 29.874);
        }

        [Test]
        [TestCase(@"Examples\HPL\HPLIncorrectResults.txt", @"The Linpack output file has incorrect format for parsing")]
        public void HPLParserThrowIfInvalidOutput(string IncorrectHPLoutputPath, string exceptionMessage)
        {
            this.rawText = File.ReadAllText(IncorrectHPLoutputPath);
            this.testParser = new HPLMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains(exceptionMessage, exception.Message);
        }
    }
}
