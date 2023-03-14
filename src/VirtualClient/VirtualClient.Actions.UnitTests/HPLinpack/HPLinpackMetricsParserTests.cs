// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class HPLinpackMetricsParserTests
    {
        private string rawText;
        private HPLinpackMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\HPLinpack\HPLResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);
        }

        [Test]
        public void HPLParserVerifyResults()
        {
            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(4, metrics.Count);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("N_WR01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["N_WR01R2R4"].Equals("8029"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("NB_WR01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["NB_WR01R2R4"].Equals("400"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("P_WR01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["P_WR01R2R4"].Equals("1"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("Q_WR01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["Q_WR01R2R4"].Equals("2"));
            MetricAssert.Exists(metrics, "Time", 11.55);
            MetricAssert.Exists(metrics, "GFlops", 29.874);
            MetricAssert.Exists(metrics, "Time", 11.55);
            MetricAssert.Exists(metrics, "GFlops", 29.874);
        }

        [Test]
        [TestCase(@"Examples\HPLinpack\HPLIncorrectResults.txt", @"The HPLinpack output file has incorrect format for parsing")]
        public void HPLParserThrowIfInvalidOutput(string IncorrectHPLoutputPath, string exceptionMessage)
        {
            this.rawText = File.ReadAllText(IncorrectHPLoutputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains(exceptionMessage, exception.Message);
        }
    }
}
