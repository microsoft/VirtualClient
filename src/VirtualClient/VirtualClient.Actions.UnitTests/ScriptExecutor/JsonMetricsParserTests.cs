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
        public void JsonMetricsParserVerifyMetricsForPassResults_ArrayFormat()
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
        public void JsonMetricsParserThrowsIfTheJsonArrayResultsHaveInvalidMetrics()
        {
            string rawText = "[\r\n\t{\r\n\t\t\"Name\": \"metric3\",\r\n\t\t\"Value\": \"a1\",\r\n\t\t\"Unit\": \"unit3\",\r\n\t\t\"MetaData\": {\r\n\t\t\t\"metadata1\": \"m5\",\r\n\t\t\t\"metadata2\": \"m6\"\r\n\t\t}\r\n\t},\r\n\t{\r\n\t\t\"Name\": \"metric4\",\r\n\t\t\"Value\": 1.0,\r\n\t\t\"MetaData\": {\r\n\t\t\t\"metadata1\": \"m7\"\r\n\t\t}\r\n\t}\r\n]";

            this.testParser = new JsonMetricsParser(rawText, new InMemoryLogger(), EventContext.None);
            Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
        }

        [Test]
        public void JsonMetricsParserThrowsIfTheJsonArrayResultsHaveMissingMetricName()
        {
            string rawText = "[\r\n\t{\r\n\t\t\"Value\": 0,\r\n\t\t\"Unit\": \"unit3\",\r\n\t\t\"MetaData\": {\r\n\t\t\t\"metadata1\": \"m5\",\r\n\t\t\t\"metadata2\": \"m6\"\r\n\t\t}\r\n\t},\r\n\t{\r\n\t\t\"Name\": \"metric4\",\r\n\t\t\"Value\": 1.0,\r\n\t\t\"MetaData\": {\r\n\t\t\t\"metadata1\": \"m7\"\r\n\t\t}\r\n\t}\r\n]";

            this.testParser = new JsonMetricsParser(rawText, new InMemoryLogger(), EventContext.None);
            Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
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
