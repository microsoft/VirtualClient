using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MLPerfTrainingMetricsParserTests
    {
        private string rawText;
        private MLPerfTrainingMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "MLPerfTraining");
            }
        }

        [Test]
        [TestCase("Example_bert_time_ms.txt")]
        public void MLPerfTrainingParserVerifySingleLineMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (rawText.Contains("time_ms"))
            {
                this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(1, metrics.Count);
                MetricAssert.Exists(metrics, "time_ms", 0, "ms");
            }
            else
            {
                // Do nothing as there are no valid metrics
            }
        }

        [Test]
        [TestCase("Example_bert_time_ms_multiple.txt")]
        public void MLPerfTrainingParserVerifyMultiLineMetrics(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (rawText.Contains("time_ms"))
            {
                this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(1, metrics.Count);
            }
            else
            {
                // Do nothing as there are no valid metrics
            }
        }

        [Test]
        [TestCase("combined.log")]
        public void MLPerfTrainingParserVerifyRealResults_1(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (rawText.Contains("time_ms"))
            {
                this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(5, metrics.Count);
            }
            else
            {
                // Do nothing as there are no valid metrics
            }
        }
    }
}