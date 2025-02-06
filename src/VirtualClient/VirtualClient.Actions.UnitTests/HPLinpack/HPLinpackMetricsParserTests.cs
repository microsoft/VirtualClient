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
            string outputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLResults.txt");
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
        public void HPLParserVerifyAMDResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPL-AMDResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText, isAMD: true);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(2, metrics.Count);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("N_WRC06R8C30c"));
            Assert.IsTrue(metrics[0].Metadata["N_WRC06R8C30c"].Equals("86880"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("NB_WRC06R8C30c"));
            Assert.IsTrue(metrics[0].Metadata["NB_WRC06R8C30c"].Equals("240"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("P_WRC06R8C30c"));
            Assert.IsTrue(metrics[0].Metadata["P_WRC06R8C30c"].Equals("1"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("Q_WRC06R8C30c"));
            Assert.IsTrue(metrics[0].Metadata["Q_WRC06R8C30c"].Equals("1"));
            MetricAssert.Exists(metrics, "Time", 1206.03);
            MetricAssert.Exists(metrics, "GFlops", 362.51);
        }

        [Test]
        public void HPLParserThrowIfInvalidOutput()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLIncorrectResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The HPLinpack output file has incorrect format for parsing", exception.Message);
        }
    }
}
