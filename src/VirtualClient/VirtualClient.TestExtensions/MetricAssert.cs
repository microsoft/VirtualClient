// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    /// <summary>
    /// Class for verifying metrics
    /// </summary>
    public static class MetricAssert
    {
        /// <summary>
        /// Asserts the <paramref name="metric"/> exists in the <paramref name="results"/>.
        /// </summary>
        public static void Exists(
            IList<Metric> results,
            Metric metric,
            List<string> expectedTags = null)
        {
            Exists(results, metric.Name, metric.Value, metric.Unit, expectedTags);
        }

        /// <summary>
        /// Asserts the metric exists in the given list of results
        /// </summary>
        public static void Exists(IList<Metric> results, string expectedMetric, double? expectedMetricValue, string expectedMetricUnit = null, List<string> expectedTags = null)
        {
            List<Metric> matchingMetrics = results.Where(m => m.Name == expectedMetric).ToList();

            Assert.IsTrue(
                matchingMetrics.Count >= 1,
                $"The metric does not exist in the set: {expectedMetric}.");

            Assert.IsTrue(
                matchingMetrics.Any(m => m.Value.Equals(expectedMetricValue)),
                $"The metric '{expectedMetric}' exists but none with value '{expectedMetricValue}'.");

            if (expectedMetricUnit != null)
            {
                Assert.IsTrue(
                    matchingMetrics.Any(m => m.Value.Equals(expectedMetricValue) && m.Unit == expectedMetricUnit),
                    $"The metric '{expectedMetric}' with value '{expectedMetricValue}'. exists but none with unit '{expectedMetricUnit}'.");
            }

            if (expectedTags != null)
            {
                Assert.IsTrue(
                    matchingMetrics.Any(m => m.Value.Equals(expectedMetricValue) && m.Unit == expectedMetricUnit && string.Join(",", m.Tags) == string.Join(",", expectedTags)),
                    $"The metric '{expectedMetric}', value '{expectedMetricValue}', unit '{expectedMetricUnit}' exists but none with tag '{string.Join(",", expectedTags)}'.");
            }
        }

    }
}
