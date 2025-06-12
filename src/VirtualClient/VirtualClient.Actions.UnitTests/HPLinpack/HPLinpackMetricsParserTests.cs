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

        [Test]
        public void HPLParserVerifyArmResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string armoutputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLResultsArm.txt");
            this.rawText = File.ReadAllText(armoutputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(4, metrics.Count);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("N_W01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["N_W01R2R4"].Equals("8029"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("NB_W01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["NB_W01R2R4"].Equals("400"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("P_W01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["P_W01R2R4"].Equals("1"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("Q_W01R2R4"));
            Assert.IsTrue(metrics[0].Metadata["Q_W01R2R4"].Equals("2"));
            MetricAssert.Exists(metrics, "Time", 11.55);
            MetricAssert.Exists(metrics, "GFlops", 29.874);
            MetricAssert.Exists(metrics, "Time", 11.55);
            MetricAssert.Exists(metrics, "GFlops", 29.874);
        }

        [Test]
        public void HPLParserVerifyIntelResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string inteloutputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLResultsIntel.txt");
            this.rawText = File.ReadAllText(inteloutputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual(2, metrics.Count);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("N_W00R2L1"));
            Assert.IsTrue(metrics[0].Metadata["N_W00R2L1"].Equals("82081"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("NB_W00R2L1"));
            Assert.IsTrue(metrics[0].Metadata["NB_W00R2L1"].Equals("256"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("P_W00R2L1"));
            Assert.IsTrue(metrics[0].Metadata["P_W00R2L1"].Equals("1"));
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("Q_W00R2L1"));
            Assert.IsTrue(metrics[0].Metadata["Q_W00R2L1"].Equals("1"));
            MetricAssert.Exists(metrics, "Time", 551.89);
            MetricAssert.Exists(metrics, "GFlops", 668.032);
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

        [Test]
        public void HPLParserExtractsVersionCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string armoutputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLResultsArm.txt");
            this.rawText = File.ReadAllText(armoutputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();
            Assert.AreEqual("2.3", this.testParser.Version);
            
            // Test Intel output version extraction
            string inteloutputPath = Path.Combine(workingDirectory, "Examples", "HPLinpack", "HPLResultsIntel.txt");
            this.rawText = File.ReadAllText(inteloutputPath);
            this.testParser = new HPLinpackMetricsParser(this.rawText);
            
            metrics = this.testParser.Parse();
            Assert.AreEqual("2.3", this.testParser.Version);
        }
    }
}
