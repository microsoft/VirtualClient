// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions for Virtual Client metric objects and related components.
    /// </summary>
    public static class MetricExtensions
    {
        /// <summary>
        /// Extension allows metadata properties to be added to each of the metrics in a set.
        /// </summary>
        public static void AddMetadata(this IEnumerable<Metric> metrics, IDictionary<string, IConvertible> metadata)
        {
            metrics.ThrowIfNull(nameof(metrics));
            metadata.ThrowIfNullOrEmpty(nameof(metadata));

            if (metrics?.Any() == true)
            {
                foreach (Metric metric in metrics)
                {
                    metric.Metadata.AddRange(metadata, true);
                }
            }
        }

        /// <summary>
        /// Filters the set of metrics down to those whose names match or contain the filter terms
        /// provided (case-insensitive) by default and filters the metrics down to those whose names exactly matches the filter terms (case-sensitive)
        /// </summary>
        /// <param name="metrics">The set of metrics to filter down.</param>
        /// <param name="filterTerms">A set of terms to match against the metric names.</param>
        /// <param name="strictFiltering">strictFiltering ensures the filters exactly macthes with the metric names and are case sensitive.</param>
        /// <returns>
        /// A set of metrics whose names match or contain the filter terms (case-insensitive).
        /// </returns>
        public static IEnumerable<Metric> FilterBy(this IEnumerable<Metric> metrics, IEnumerable<string> filterTerms, bool strictFiltering = false)
        {
            metrics.ThrowIfNull(nameof(metrics));

            IEnumerable<Metric> filteredMetrics = metrics;

            if (filterTerms?.Any() == true)
            {
                var regexFilterTerms = strictFiltering
                    ? filterTerms.Select(term => new Regex(term, RegexOptions.Compiled))
                    : filterTerms.Select(term => new Regex(term, RegexOptions.IgnoreCase | RegexOptions.Compiled));
                // filteredMetrics = metrics.Where(m => filterTerms.Contains(m.Name, MetricFilterComparer.Instance)).ToList();
                // filteredMetrics = metrics.Where(metric => regexFilterTerms.Any(regex => regex.IsMatch(metric.Name)));
                filteredMetrics = strictFiltering
                    ? metrics.Where(metric =>
                        regexFilterTerms.Any(regex => regex.IsMatch(metric.Name) && regex.Match(metric.Name).Value == metric.Name))
                    : metrics.Where(metric =>
                        regexFilterTerms.Any(regex => regex.IsMatch(metric.Name)));
            }

            return filteredMetrics;
        }

        /*/// <summary>
        /// Filters the set of metrics down to those whose names exactly matches the filter terms which can be filter names and regex (case-sensitive)
        /// </summary>
        /// <param name="metrics">The set of metrics to filter down.</param>
        /// <param name="filterTerms">A set of terms to match against the metric names.</param>
        /// <returns>
        /// A set of metrics whose names exactly matches filter terms (case-sensitive).
        /// </returns>
        public static IEnumerable<Metric> FilterBy(this IEnumerable<Metric> metrics, IEnumerable<Regex> filterTerms)
        {
            metrics.ThrowIfNull(nameof(metrics));

            IEnumerable<Metric> filteredMetrics = new List<Metric>();

            if (filterTerms?.Any() == true)
            {
                // filteredMetrics = metrics.Where(metric => filterTerms.Any(regex => regex.IsMatch(metric.Name) && regex.Match(metric.Name).Value == metric.Name));
                filteredMetrics = metrics.Where(metric => filterTerms.Any(regex => regex.IsMatch(metric.Name)));
            }

            return filteredMetrics;
        }*/

        private class MetricFilterComparer : IEqualityComparer<string>
        {
            private MetricFilterComparer()
            {
            }

            public static MetricFilterComparer Instance { get; } = new MetricFilterComparer();

            public bool Equals(string x, string y)
            {
                return Regex.IsMatch(y, x, RegexOptions.IgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.ToLowerInvariant().GetHashCode();
            }
        }
    }
}
