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
using VirtualClient.Common.Telemetry;

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class JsonMetricsParserTests
    {
        private JsonMetricsParser testParser;

        [Test]
        public void JsonMetricsParserVerifyMetricsForPassResults_Format1()
        {
            string resultsPath = MockFixture.GetDirectory(typeof(JsonMetricsParserTests), "Examples", "ScriptExecutor", "validJsonExample.json");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new JsonMetricsParser(rawText, new InMemoryLogger(), EventContext.None);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "metric1", 0);
            MetricAssert.Exists(metrics, "metric2", 1.45);
            MetricAssert.Exists(metrics, "metric3", 1279854282929.09);
        }

        [Test]
        public void JsonMetricsParserVerifyMetricsForPassResults_Format2()
        {
            string resultsPath = MockFixture.GetDirectory(typeof(JsonMetricsParserTests), "Examples", "ScriptExecutor", "validJsonExample_array.json");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new JsonMetricsParser(rawText, new InMemoryLogger(), EventContext.None);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(6, metrics.Count);
            MetricAssert.Exists(metrics, "metric1", 0);
            MetricAssert.Exists(metrics, "metric2", -1);
            MetricAssert.Exists(metrics, "metric3", 1.2);
            MetricAssert.Exists(metrics, "metric4", 1);
            MetricAssert.Exists(metrics, "metric5", 1.24);
            MetricAssert.Exists(metrics, "metric6", -5.8);
        }

        [Test]
        public void JsonMetricsParserThrowsIfTheJsonResultsHaveInvalidMetrics()
        {
            string resultsPath = MockFixture.GetDirectory(typeof(JsonMetricsParserTests), "Examples", "ScriptExecutor", "invalidJsonExample.json");
            string rawText = File.ReadAllText(resultsPath);
            this.testParser = new JsonMetricsParser(rawText, new InMemoryLogger(), EventContext.None);

            Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
        }

        [Test]
        public void JsonMetricsParserThrowsIfTheResultsAreNotProperlyFormattedKeyValuePairs()
        {
            this.testParser = new JsonMetricsParser("{ 'this': { 'is': 'json' }, 'but': [ 'not', 'properly', 'formatted' ] }", new InMemoryLogger(), EventContext.None);
            Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
        }

        [Test]
        public void JsonMetricsParserThrowsIfTheResultsAreNotJsonFormatted()
        {
            this.testParser = new JsonMetricsParser("<this><is/><not/><json/></this>", new InMemoryLogger(), EventContext.None);
            Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
        }
    }
}
