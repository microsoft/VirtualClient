// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class MetricExtensionsTests
    {
        private List<Metric> metrics;

        [SetUp]
        public void SetupTest()
        {
            this.metrics = new List<Metric>
            {
                new Metric("read_bandwidth", 1234),
                new Metric("read_iops", 21.234),
                new Metric("read_completionlatency_max", 12345867),
                new Metric("read_completionlatency_p50", 123456),
                new Metric("read_completionlatency_p70", 142347),
                new Metric("read_completionlatency_p99", 254567),
                new Metric("read_completionlatency_p99_99", 274567),
                new Metric("read_submissionlatency_mean", 11.92736453738),

                new Metric("write_bandwidth", 748),
                new Metric("write_iops", 11.456),
                new Metric("write_completionlatency_max", 24367876),
                new Metric("write_completionlatency_p50", 1562344),
                new Metric("write_completionlatency_p70", 2987657),
                new Metric("write_completionlatency_p99", 3167543),
                new Metric("write_completionlatency_p99_99", 3267543),
                new Metric("write_submissionlatency_mean", 15.35467863),
            };
        }

        [Test]
        public void FilterByExtensionValidatesRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => (null as List<Metric>).FilterBy(new List<string>()));
        }

        [Test]
        public void FilterByExtensionHandlesEmptyMetricSets()
        {
            Assert.DoesNotThrow(() => (new List<Metric>()).FilterBy(new List<string>()));
        }

        [Test]
        public void FilterByExtensionHandlesEmptyFilterSets()
        {
            Assert.DoesNotThrow(() => this.metrics.FilterBy(new List<string>()));
            CollectionAssert.AreEquivalent(this.metrics.Select(m => m.Name), this.metrics.FilterBy(new List<string>()).Select(m => m.Name));
        }

        [Test]
        public void FilterByExtensionReturnsTheExpectedFilteredSetOfMetrics_Scenario1()
        {
            List<string> filters = new List<string>
            {
                "read"
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Take(8);
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }

        [Test]
        public void FilterByExtensionReturnsTheExpectedFilteredSetOfMetrics_Scenario2()
        {
            List<string> filters = new List<string>
            {
                "bandwidth",
                "iops"
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Where(m => m.Name.Contains("bandwidth") || m.Name.Contains("iops"));
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }

        [Test]
        public void FilterByExtensionReturnsTheExpectedFilteredSetOfMetrics_Scenario3()
        {
            List<string> filters = new List<string>
            {
                "_p50",
                "_p70",
                "_p99"
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Where(m => m.Name.Contains("_p50") || m.Name.Contains("_p70") || m.Name.Contains("_p99"));
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }

        [Test]
        public void FilterByExtensionReturnsTheExpectedFilteredSetOfMetrics_Scenario4()
        {
            List<string> filters = new List<string>
            {
                // An actual regular expression
                "(read|write)_(bandwidth|iops)"
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Where(m => m.Name.Contains("bandwidth") || m.Name.Contains("iops"));
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }

        [Test]
        public void FilterByExtensionIsNotCaseSensitive()
        {
            List<string> filters = new List<string>
            {
                "BaNdWiDtH",
                "IoPs"
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Where(m => m.Name.Contains("bandwidth") || m.Name.Contains("iops"));
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }
    }
}
