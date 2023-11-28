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
        [TestCase("Example_bert_real_output.txt")]
        public void MLPerfTrainingMetricsParserParsesCorrectMetricsFromRawText(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(5, metrics.Count);
            MetricAssert.Exists(metrics, "eval_mlm_accuracy", 0.71472860574722286);
            MetricAssert.Exists(metrics, "e2e_time", 596.53150777816768, "s");
            MetricAssert.Exists(metrics, "training_sequences_per_second", 1855.6555448898107);
            MetricAssert.Exists(metrics, "final_loss", 0);
            MetricAssert.Exists(metrics, "raw_train_time", 577.98435058593748, "s");
        }

        [Test]
        [TestCase("Example_bert_incorrect_output.txt")]
        public void MLPerfTrainingMetricsParserThrowsOnIncorrectRawText(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The MlPerf Training output file has incorrect format for parsing", exception.Message);
        }
    }
}