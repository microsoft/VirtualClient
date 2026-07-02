// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions for Virtual Client metric objects and related components.
    /// </summary>
    public static class MetricExtensions
    {
        private static readonly Regex VerbosityExpression = new Regex(@"^verbosity:\s*(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
        /// Filters the set of metrics down to those that match the filter criteria provided.
        /// Supports both verbosity-based filtering and regex-based name filtering (case-insensitive).
        /// </summary>
        /// <param name="metrics">The set of metrics to filter down.</param>
        /// <param name="filterTerms">
        /// A set of filter terms. Can include:
        /// - Verbosity filters: "verbosity:N" where N is 1-5 (filters metrics with verbosity less than or equal to N)
        /// - Name filters: Any regex pattern to match against metric names (case-insensitive)
        /// - Exclusion filters: Prefix with "-" to exclude matching metrics (e.g., "-h000*")
        /// </param>
        /// <param name="caseSensitive">True to perform case-sensitive search. Default = false (case-insensitive).</param>
        /// <returns>
        /// A filtered set of metrics matching the criteria.
        /// </returns>
        /// <remarks>
        /// Verbosity levels define a convention for organizing metrics by importance:
        /// - 1 (Standard/Critical): Most important metrics - bandwidth, throughput, IOPS, key latency percentiles (p50, p99)
        /// - 2 (Detailed): Additional detailed metrics - supplementary percentiles (p70, p90, p95, p99.9)
        /// - 3 (Reserved): Reserved for future expansion
        /// - 4 (Reserved): Reserved for future expansion
        /// - 5 (Verbose): All diagnostic/internal metrics - histogram buckets, standard deviations, byte counts, I/O counts
        /// 
        /// Currently, only levels 1, 2, and 5 are actively used. Levels 3 and 4 are reserved for future use.
        /// 
        /// Filters are composable - verbosity filtering is applied first, then exclusion filters, then inclusion (name-based) filtering.
        /// </remarks>
        public static IEnumerable<Metric> FilterBy(this IEnumerable<Metric> metrics, IEnumerable<string> filterTerms, bool caseSensitive = false)
        {
            metrics.ThrowIfNull(nameof(metrics));

            IEnumerable<Metric> filteredMetrics = metrics;
            IEnumerable<string> effectiveFilterTerms = filterTerms.ToList();

            if (effectiveFilterTerms?.Any() == true)
            {
                RegexOptions regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

                // Step 1: Handle verbosity filtering first
                Match verbosityMatch = effectiveFilterTerms.Select(f => Regex.Match(f, @"^verbosity:\s*(\d+)$", regexOptions)).FirstOrDefault(m => m.Success);

                if (verbosityMatch?.Success == true)
                {
                    // Extract the verbosity level from the filter (e.g., "verbosity:3" -> 3)
                    int verbosity = int.Parse(verbosityMatch.Groups[1].Value);
                    if (verbosity >= 0 && verbosity <= 5)
                    {
                        // Filter metrics to include only those with verbosity <= maxVerbosity
                        filteredMetrics = filteredMetrics.Where(m => m.Verbosity <= verbosity);
                    }

                    // Remove verbosity filter from remaining filters
                    (effectiveFilterTerms as IList<string>).Remove(verbosityMatch.Value);
                }

                if (effectiveFilterTerms?.Any() == true)
                {
                    // Step 2: Handle exclusion filters (prefix with "-")
                    var exclusionFilters = effectiveFilterTerms.Where(f => f.StartsWith("-")).Select(f => f.Substring(1)).ToList();
                    if (exclusionFilters.Any())
                    {
                        filteredMetrics = filteredMetrics.Where(m => !exclusionFilters.Any(f => Regex.IsMatch(m.Name, f, regexOptions)));
                        effectiveFilterTerms = effectiveFilterTerms.Where(f => !f.StartsWith("-"));
                    }

                    // Step 3: Handle inclusion/regex name filtering
                    if (effectiveFilterTerms?.Any() == true)
                    {
                        filteredMetrics = filteredMetrics.Where(m => effectiveFilterTerms.Any(f => Regex.IsMatch(m.Name, f, regexOptions)));
                    }
                }
            }

            return filteredMetrics;
        }

        /// <summary>
        /// Print metrics to console. Default to critical only.
        /// </summary>
        /// <param name="metrics">List of Metrics</param>
        /// <param name="scenario">Scenario</param>
        /// <param name="toolname"></param>
        /// <param name="criticalOnly">Boolean to note if prints critical only metric</param>
        public static void LogConsole(this IEnumerable<Metric> metrics, string scenario, string toolname, bool criticalOnly = true)
        {
            try
            {
                Console.WriteLine();
                Console.WriteLine($"*** {toolname} Metrics ***");
                Dictionary<string, int> colWidths = new Dictionary<string, int>();

                DataTable table = new DataTable();
                table.Columns.Add("Scenario", typeof(string));
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Value", typeof(double));
                table.Columns.Add("Unit", typeof(string));

                // Support both old (0) and new (1) critical levels for backward compatibility
                IEnumerable<Metric> metricsToPrint = criticalOnly ? metrics.Where(m => m.Verbosity == 0 || m.Verbosity == 1).ToList() : metrics;

                foreach (Metric metric in metricsToPrint)
                {
                    DataRow row = table.NewRow();
                    row["Scenario"] = scenario; // You can modify this as needed
                    row["Name"] = metric.Name;
                    row["Value"] = metric.Value;
                    row["Unit"] = metric.Unit;
                    table.Rows.Add(row);
                }

                MetricExtensions.LogConsole(table);
            }
            catch (Exception exc)
            {
                // Don't throw on print console errors
                Console.WriteLine($"Failed to print metrics on console: '{exc.Message}'");
            }
        }

        /// <summary>
        /// Data table with formated column width.
        /// </summary>
        /// <param name="dataTable">Input data table.</param>
        private static void LogConsole(DataTable dataTable)
        {
            Dictionary<string, int> colWidths = new Dictionary<string, int>();

            foreach (DataColumn col in dataTable.Columns)
            {
                Console.Write("| " + col.ColumnName);
                var maxLabelSize = dataTable.Rows.OfType<DataRow>()
                        .Select(m => (m.Field<object>(col.ColumnName)?.ToString() ?? string.Empty).Length)
                        .OrderByDescending(m => m).FirstOrDefault();

                maxLabelSize = Math.Max(col.ColumnName.Length, maxLabelSize);

                colWidths.Add(col.ColumnName, maxLabelSize);
                for (int i = 0; i < maxLabelSize - col.ColumnName.Length + 1; i++)
                {
                    Console.Write(" ");
                }
            }

            Console.Write("|");
            Console.WriteLine();
            Console.WriteLine(new string('-', colWidths.Values.Sum() + 13)); // 13 is the extra width for the spaces and pipes

            foreach (DataRow dataRow in dataTable.Rows)
            {
                for (int j = 0; j < dataRow.ItemArray.Length; j++)
                {
                    Console.Write("| " + dataRow.ItemArray[j]);
                    for (int i = 0; i < colWidths[dataTable.Columns[j].ColumnName] - dataRow.ItemArray[j].ToString().Length + 1; i++)
                    {
                        Console.Write(" ");
                    }
                }

                Console.Write("|");
                Console.WriteLine();
            }

            Console.WriteLine();
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
