// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using NUnit.Framework;
    using VirtualClient.Actions.Properties;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DiskSpdResultsExtensionsTests
    {
        private XmlDocument mockResults;
        private string mockDiskSpdResults;

        [SetUp]
        public void SetupTest()
        {
            this.mockResults = new XmlDocument();
            this.mockResults.LoadXml(TestResources.Results_DiskSpd_Xml);
            this.mockDiskSpdResults = TestResources.Results_DiskSpd_Text;
        }

        [Test]
        public void AddCpuUtilizationMetricsExtensionAddsTheExpectedCpuUtilizationInformationToTheResults()
        {
            IList<Metric> results = new List<Metric>();
            results.AddCpuUtilizationMetrics(this.mockDiskSpdResults);

            // Based on exact values in the example XML loaded above.
            Assert.IsNotEmpty(results);
            Assert.IsTrue(results.Count == 4);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "avg % cpu usage", 0.82);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "avg % cpu usage(user mode)", 0.10);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "avg % cpu usage(kernel mode)", 0.72);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "avg % cpu idle", 99.18);
        }

        [Test]
        public void AddDiskLatencyMetricsExtensionTheExpectedLatencyInformationToTheResults()
        {
            IList<Metric> results = new List<Metric>();
            results.AddDiskIOMetrics(this.mockDiskSpdResults);

            // Based on exact values in the example XML loaded above.
            Assert.IsNotEmpty(results);
            Assert.IsTrue(results.Count == 21);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total bytes", 146169856);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total IO operations", 35686);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total throughput", 4.64, MetricUnit.MebibytesPerSecond);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total iops", 1189.03);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "avg. latency", 0.840, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "iops stdev", 129.18);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "latency stdev", 1.051, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read total bytes", 102645760);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read IO operations", 25060);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read throughput", 3.26, MetricUnit.MebibytesPerSecond);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read iops", 834.98);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read avg. latency", 0.272, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read iops stdev", 94.24);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency stdev", 0.185, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write total bytes", 43524096);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write IO operations", 10626);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write throughput", 1.38, MetricUnit.MebibytesPerSecond);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write iops", 354.05);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write avg. latency", 2.179, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write iops stdev", 38.48);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency stdev", 1.037, MetricUnit.Milliseconds);
        }

        [Test]
        public void AddDiskIOPercentileMetricsExtensionTheExpectedLatencyPercentilesInformationToTheResults()
        {
            IList<Metric> results = new List<Metric>();
            results.AddDiskIOPercentileMetrics(this.mockDiskSpdResults);

            // Based on exact values in the example XML loaded above.
            Assert.IsNotEmpty(results);
            Assert.IsTrue(results.Count == 15);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency/operation(P50)", 0.232, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency/operation(P50)", 1.833, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total latency/operation(P50)", 0.297, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency/operation(P75)", 0.314, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency/operation(P75)", 2.308, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total latency/operation(P75)", 1.538, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency/operation(P90)", 0.458, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency/operation(P90)", 3.378, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total latency/operation(P90)", 2.065, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency/operation(P95)", 0.588, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency/operation(P95)", 4.200, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total latency/operation(P95)", 2.752, MetricUnit.Milliseconds);

            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "read latency/operation(P99)", 0.877, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "write latency/operation(P99)", 6.107, MetricUnit.Milliseconds);
            DiskSpdResultsExtensionsTests.AssertMetricExists(results, "total latency/operation(P99)", 4.713, MetricUnit.Milliseconds);
        }

        private static void AssertMetricExists(IList<Metric> results, string expectedMetric, double expectedMetricValue, string expectedMetricUnit = null)
        {
            Assert.IsTrue(
                results.FirstOrDefault(m => m.Name == expectedMetric) != null,
                "The metric does not exist in the set.");

            Assert.AreEqual(
                expectedMetricValue,
                results.FirstOrDefault(m => m.Name == expectedMetric).Value,
                $"The metric value does not match expected.");

            if (expectedMetricUnit != null)
            {
                Assert.AreEqual(
                    expectedMetricUnit,
                    results.FirstOrDefault(m => m.Name == expectedMetric).Unit,
                    $"The metric unit does not match expected.");
            }
        }
    }
}