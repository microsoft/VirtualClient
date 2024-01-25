// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;
using VirtualClient.Common.Contracts;
using VirtualClient.Actions;
using CRC.Toolkit.VirtualClient;

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class JsonMetricsParserTests
    {
        private JsonMetricsParser testParser;
        string workingDirectory;

        [SetUp]
        public void Setup()
        {
            this.workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [Test]
        public void JsonMetricsParserVerifyMetricsForPassResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "ScriptExecutor", "validExample.json");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new JsonMetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "metric1", 0);
            MetricAssert.Exists(metrics, "metric2", 1.45);
            MetricAssert.Exists(metrics, "metric3", 1279854282929.09);
        }

        [Test]
        public void JsonMetricsParserVerifyMetricsForFailedResults()
        {
            string resultsPath = Path.Combine(this.workingDirectory, "Examples", "ScriptExecutor", "invalidExample.json");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new JsonMetricsParser(rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "metric2", 1.45);
            MetricAssert.Exists(metrics, "metric3", 129.09);
        }
    }
}
