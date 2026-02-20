// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
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

                new Metric("verbose_test_1", 123, "unit", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("verbose_test_2", -123, "unit", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("verbose_test_3", 123, "unit", MetricRelativity.HigherIsBetter, verbosity: 2),
                new Metric("verbose_test_4", -123, "unit", MetricRelativity.HigherIsBetter, verbosity: 2),
                new Metric("verbose_test_5", -123, "unit", MetricRelativity.HigherIsBetter, verbosity: 5),
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
        public void MetricFiltersCorrectFiltersVerbosity()
        {
            IEnumerable<string> filter = new List<string> { "Verbosity:1" };
            CollectionAssert.AreEquivalent(this.metrics.Where(m => m.Verbosity <= 1).Select(m => m.Name), this.metrics.FilterBy(filter).Select(m => m.Name));

            filter = new List<string> { "Verbosity:2" };
            CollectionAssert.AreEquivalent(this.metrics.Where(m => m.Verbosity <= 2).Select(m => m.Name), this.metrics.FilterBy(filter).Select(m => m.Name));

            filter = new List<string> { "Verbosity:5" };
            CollectionAssert.AreEquivalent(this.metrics.Where(m => m.Verbosity <= 5).Select(m => m.Name), this.metrics.FilterBy(filter).Select(m => m.Name));

            filter = new List<string> { "Verbosity:others" };
            CollectionAssert.AreEquivalent(Enumerable.Empty<Metric>().Select(m => m.Name), this.metrics.FilterBy(filter).Select(m => m.Name));
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

        [Test]
        public void FilterByExtensionReturnsTheExpectedFilteredWithBothVerbosityAndText()
        {
            List<string> filters = new List<string>
            {
                "test_2",
                "verbosity:1",
            };

            IEnumerable<Metric> expectedMetrics = this.metrics.Where(m => m.Name == "verbose_test_2");
            IEnumerable<Metric> actualMetrics = this.metrics.FilterBy(filters);

            Assert.IsNotNull(actualMetrics);
            Assert.IsNotEmpty(actualMetrics);
            CollectionAssert.AreEquivalent(expectedMetrics, actualMetrics);
        }

        [Test]
        public void FilterByExtensionSupportsAllVerbosityLevels()
        {
            // Setup metrics with different verbosity levels (1, 2, 5 only)
            var metrics = new List<Metric>
            {
                new Metric("critical_metric_1a", 1, "unit", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("critical_metric_1b", 2, "unit", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("detailed_metric_2a", 3, "unit", MetricRelativity.HigherIsBetter, verbosity: 2),
                new Metric("detailed_metric_2b", 4, "unit", MetricRelativity.HigherIsBetter, verbosity: 2),
                new Metric("verbose_metric_5a", 5, "unit", MetricRelativity.HigherIsBetter, verbosity: 5),
                new Metric("verbose_metric_5b", 6, "unit", MetricRelativity.HigherIsBetter, verbosity: 5)
            };

            // Test verbosity:1 - should return only level 1 metrics
            var filter1 = new List<string> { "verbosity:1" };
            var result1 = metrics.FilterBy(filter1);
            Assert.AreEqual(2, result1.Count());
            Assert.IsTrue(result1.All(m => m.Verbosity == 1));

            // Test verbosity:2 - should return level 1 + level 2 metrics
            var filter2 = new List<string> { "verbosity:2" };
            var result2 = metrics.FilterBy(filter2);
            Assert.AreEqual(4, result2.Count());
            Assert.IsTrue(result2.All(m => m.Verbosity <= 2));

            // Test verbosity:5 - should return all metrics
            var filter5 = new List<string> { "verbosity:5" };
            var result5 = metrics.FilterBy(filter5);
            Assert.AreEqual(6, result5.Count());
        }

        [Test]
        public void FilterByExtensionHandlesInvalidVerbosityValues()
        {
            var metrics = new List<Metric>
            {
                new Metric("test_metric", 1, "unit", MetricRelativity.HigherIsBetter, verbosity: 1)
            };

            // Test invalid verbosity format
            var invalidFilter1 = new List<string> { "verbosity:invalid" };
            var result1 = metrics.FilterBy(invalidFilter1);
            Assert.AreEqual(0, result1.Count());

            // Test out of range verbosity (> 5)
            var invalidFilter2 = new List<string> { "verbosity:10" };
            var result2 = metrics.FilterBy(invalidFilter2);
            Assert.AreEqual(0, result2.Count());

            // Test verbosity less than 0 (negative)
            var invalidFilter3 = new List<string> { "verbosity:-1" };
            var result3 = metrics.FilterBy(invalidFilter3);
            Assert.AreEqual(0, result3.Count());
        }

        [Test]
        public void FilterByExtensionHandlesBackwardCompatibilityForVerbosityZero()
        {
            var metrics = new List<Metric>
            {
                new Metric("critical_metric", 1, "unit", MetricRelativity.HigherIsBetter, verbosity: 0),
                new Metric("standard_metric", 2, "unit", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("detailed_metric", 3, "unit", MetricRelativity.HigherIsBetter, verbosity: 2)
            };

            // verbosity:0 should be mapped to verbosity:1 for backward compatibility
            var filter = new List<string> { "verbosity:0" };
            var result = metrics.FilterBy(filter);

            // Should include metrics with verbosity 0 and 1 (since 0 maps to 1)
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.Any(m => m.Name == "critical_metric"));
            Assert.IsTrue(result.Any(m => m.Name == "standard_metric"));
            Assert.IsFalse(result.Any(m => m.Name == "detailed_metric"));
        }

        [Test]
        public void FilterByExtensionSupportsExclusionFilters()
        {
            var metrics = new List<Metric>
            {
                new Metric("h000_metric", 1),
                new Metric("h001_metric", 2),
                new Metric("bandwidth", 3),
                new Metric("iops", 4)
            };

            // Test exclusion filter
            var exclusionFilter = new List<string> { "-h000*", "-h001*" };
            var result = metrics.FilterBy(exclusionFilter);
            Assert.AreEqual(2, result.Count());
            Assert.IsFalse(result.Any(m => m.Name.StartsWith("h00")));
            Assert.IsTrue(result.Any(m => m.Name == "bandwidth"));
            Assert.IsTrue(result.Any(m => m.Name == "iops"));
        }

        [Test]
        public void FilterByExtensionComposesVerbosityAndNameFilters()
        {
            var metrics = new List<Metric>
            {
                new Metric("bandwidth_read", 1, "MB/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("bandwidth_write", 2, "MB/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("iops_read", 3, "ops/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("iops_write", 4, "ops/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("latency_p99", 5, "ms", MetricRelativity.LowerIsBetter, verbosity: 5)
            };

            // Filter: verbosity <= 1 AND name contains "bandwidth"
            var composedFilter = new List<string> { "verbosity:1", "bandwidth" };
            var result = metrics.FilterBy(composedFilter);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(result.All(m => m.Name.Contains("bandwidth") && m.Verbosity <= 1));
        }

        [Test]
        public void FilterByExtensionSupportsComplexFilterCombinations()
        {
            var metrics = new List<Metric>
            {
                new Metric("h000_latency", 1, "ms", MetricRelativity.LowerIsBetter, verbosity: 5),
                new Metric("h001_latency", 2, "ms", MetricRelativity.LowerIsBetter, verbosity: 5),
                new Metric("bandwidth_read", 3, "MB/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("bandwidth_write", 4, "MB/s", MetricRelativity.HigherIsBetter, verbosity: 1),
                new Metric("iops", 5, "ops/s", MetricRelativity.HigherIsBetter, verbosity: 1)
            };

            // Complex filter: verbosity <= 5, exclude h00* metrics, include only bandwidth or iops
            var complexFilter = new List<string> { "verbosity:5", "-h00*", "bandwidth|iops" };
            var result = metrics.FilterBy(complexFilter);

            Assert.IsTrue(result.All(m => m.Verbosity <= 5));
            Assert.IsFalse(result.Any(m => m.Name.StartsWith("h00")));
            Assert.IsTrue(result.All(m => m.Name.Contains("bandwidth") || m.Name.Contains("iops")));
        }
    }
}
