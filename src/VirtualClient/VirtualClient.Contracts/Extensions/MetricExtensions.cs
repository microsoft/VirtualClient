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
                string verbosityFilter = filterTerms.Where(f => f.Contains("verbosity", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (!string.IsNullOrEmpty(verbosityFilter)) 
                {
                    switch (verbosityFilter.ToLower())
                    {
                        case "verbosity:0":
                            filteredMetrics = filteredMetrics.Where(m => ((int)m.Verbosity) <= 0);
                            break;
                        case "verbosity:1":
                            filteredMetrics = filteredMetrics.Where(m => ((int)m.Verbosity) <= 1);
                            break;
                        case "verbosity:2":
                            filteredMetrics = filteredMetrics.Where(m => ((int)m.Verbosity) <= 2);
                            break;

                        default:
                            filteredMetrics = Enumerable.Empty<Metric>();
                            break;
                    }

                    filterTerms = filterTerms.Where(f => !f.Contains("verbosity", StringComparison.OrdinalIgnoreCase));
                }

                if (filterTerms?.Any() == true)
                {
                    filteredMetrics = metrics.Where(m => filterTerms.Contains(m.Name, MetricFilterComparer.Instance)).ToList();
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

                IEnumerable<Metric> metricsToPrint = criticalOnly ? metrics.Where(m => m.Verbosity == 0).ToList() : metrics;

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
