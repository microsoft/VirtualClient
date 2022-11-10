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
        /// Filters the set of metrics down to those whose names match or contain the filter terms
        /// provided (case-insensitive).
        /// </summary>
        /// <param name="metrics">The set of metrics to filter down.</param>
        /// <param name="filterTerms">A set of terms to match against the metric names.</param>
        /// <returns>
        /// A set of metrics whose names match or contain the filter terms (case-insensitive).
        /// </returns>
        public static IEnumerable<Metric> FilterBy(this IEnumerable<Metric> metrics, IEnumerable<string> filterTerms)
        {
            metrics.ThrowIfNull(nameof(metrics));

            IEnumerable<Metric> filteredMetrics = metrics;
            if (filterTerms?.Any() == true)
            {
                filteredMetrics = metrics.Where(m => filterTerms.Contains(m.Name, MetricFilterComparer.Instance)).ToList();
            }

            return filteredMetrics;
        }

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
