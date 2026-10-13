// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.ApacheBench
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ApacheBenchMetricsParserTests : MockFixture
    {
        private string rawText;
        private ApacheBenchMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            this.rawText = MockFixture.ReadFile(
                MockFixture.TestExamplesDirectory,
                "ApacheBench",
                "ApacheBenchResultsExample.txt");
            this.testParser = new ApacheBenchMetricsParser(this.rawText);
        }

        [Test]
        public void ApacheBenchMetricsParserParsesAsExpected()
        {
            this.testParser.Parse();
            Assert.IsNotNull(this.testParser.Sections["Metrics"]);
        }

        [Test]
        public void ApacheBenchMetricsParserParsesInputAsExpected()
        {
            IList<Metric> metrics = this.testParser.Parse();
            string metricsInput = this.testParser.Sections["Metrics"];
            Assert.IsNotNull(metricsInput);

            MetricAssert.Exists(metrics, "Total requests", 100);
            MetricAssert.Exists(metrics, "Total time", 0.578);
            MetricAssert.Exists(metrics, "Total failed requests", 0);
            MetricAssert.Exists(metrics, "Requests", 172.97);
            MetricAssert.Exists(metrics, "Total time per request", 5.781);
            MetricAssert.Exists(metrics, "Total data transferred", 65006);
            MetricAssert.Exists(metrics, "Data transfer rate", 109.81);
        }

        [Test]
        public void ApacheBenchResultsParserReturnsEmptyResultWhenInvalidResultsAreProvided()
        {
            this.rawText = MockFixture.ReadFile(
                MockFixture.TestExamplesDirectory,
                "ApacheBench",
                "ApacheBenchResultsInvalidExample.txt");
            this.testParser = new ApacheBenchMetricsParser(this.rawText);
            Assert.Throws<WorkloadException>(() => this.testParser.Parse());
        }
    }
}
