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
        public void MLPerfTrainingParserVerifyRealResults_1(string exampleFile)
        {
            string outputPath = Path.Combine(this.ExamplePath, exampleFile);
            this.rawText = File.ReadAllText(outputPath);

            if (rawText.Contains("time_ms"))
            {
                this.testParser = new MLPerfTrainingMetricsParser(this.rawText);
                IList<Metric> metrics = this.testParser.Parse();

                Assert.AreEqual(5, metrics.Count);
                MetricAssert.Exists(metrics, "Accuracy", 0.71505482494831085, "%");
                MetricAssert.Exists(metrics, "e2e_time", 595.89423966407776, "s");
                MetricAssert.Exists(metrics, "training_sequences_per_second", 1855.5367415584262, "");
                MetricAssert.Exists(metrics, "final_loss", 0, "");
                MetricAssert.Exists(metrics, "raw_train_time", 577.73040866851807, "s");
            }
            else
            {
                // Do nothing as there are no valid metrics
            }
        }
    }
}