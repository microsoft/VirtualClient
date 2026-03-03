// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for CoreMark output document
    /// </summary>
    public class CoreMarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// Separate the column values by 2 or more spaces, so that "N-Body Physics" will not be separated into two cells.
        /// </summary>
        private static readonly Regex CoreMarkDataTableDelimiter = new Regex(@"(\s)*(:)(\s)*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="CoreMarkMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public CoreMarkMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Single core result.
        /// </summary>
        public DataTable CoreMarkResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.ParseCoremarkResult();

            List<Metric> metrics = new List<Metric>();

            metrics.AddRange(this.CoreMarkResult.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "NA", namePrefix: string.Empty, ignoreFormatError: true));
            // CoreMark result doesn't define the unit so needs manually assign units.
            metrics.Where(m => m.Name == "CoreMark Size").FirstOrDefault().Unit = "bytes";
            metrics.Where(m => m.Name == "CoreMark Size").FirstOrDefault().Verbosity = 5;
            metrics.Where(m => m.Name == "Total ticks").FirstOrDefault().Unit = "ticks";
            metrics.Where(m => m.Name == "Total ticks").FirstOrDefault().Verbosity = 5;
            metrics.Where(m => m.Name == "Total time (secs)").FirstOrDefault().Unit = "secs";
            metrics.Where(m => m.Name == "Total time (secs)").FirstOrDefault().Verbosity = 5;
            metrics.Where(m => m.Name == "Iterations/Sec").FirstOrDefault().Unit = "iterations/sec";
            metrics.Where(m => m.Name == "Iterations/Sec").FirstOrDefault().Relativity = MetricRelativity.HigherIsBetter;
            metrics.Where(m => m.Name == "Iterations/Sec").FirstOrDefault().Verbosity = 1;
            metrics.Where(m => m.Name == "Iterations").FirstOrDefault().Unit = "iterations";
            metrics.Where(m => m.Name == "Iterations").FirstOrDefault().Relativity = MetricRelativity.Undefined;
            metrics.Where(m => m.Name == "Iterations").FirstOrDefault().Verbosity = 5;

            // This line won't be there if it's running single thread.
            if (metrics.Any(m => m.Name == "Parallel PThreads"))
            {
                metrics.Where(m => m.Name == "Parallel PThreads").FirstOrDefault().Unit = "threads";
                metrics.Where(m => m.Name == "Parallel PThreads").FirstOrDefault().Verbosity = 5;
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Remove all the rows that don't have column sign.
            List<string> result = new List<string>();
            // Coremark always use \n as line delimiter. Even on Windows.
            this.PreprocessedText = this.RawText.Replace("\n", Environment.NewLine);
            List<string> rows = this.PreprocessedText.Split(Environment.NewLine, StringSplitOptions.None).ToList();
            foreach (string row in rows)
            {
                // Remove all dashline and all star lines.
                if (row.Contains(':'))
                {
                    result.Add(row);
                }
            }

            this.PreprocessedText = string.Join(Environment.NewLine, result);
        }

        private void ParseCoremarkResult()
        {
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.CoreMarkResult = DataTableExtensions.ConvertToDataTable(
                this.PreprocessedText, CoreMarkMetricsParser.CoreMarkDataTableDelimiter, "CoreMark", columnNames);
        }
    }
}
