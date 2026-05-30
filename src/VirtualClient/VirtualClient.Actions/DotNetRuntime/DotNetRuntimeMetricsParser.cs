// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for DotNetRuntime output document.
    /// </summary>
    public class DotNetRuntimeMetricsParser : MetricsParser
    {
        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex DotNetRuntimeSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex DotNetRuntimeDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Regex for removing "==========" lines.
        /// </summary>
        private static readonly Regex EqualLineRegex = new Regex(@"(=){2,}(\s)*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="DotNetRuntimeMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DotNetRuntimeMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Throughput result for DotNet Runtime.
        /// </summary>
        public DataTable ThroughputResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, DotNetRuntimeSectionDelimiter);
            this.ThrowIfInvalidOutputFormat();
            this.CalculateThroughputResult();

            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(this.ThroughputResult.GetMetrics(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.HigherIsBetter));
            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.RawText, DotNetRuntimeMetricsParser.EqualLineRegex);
            this.PreprocessedText = this.PreprocessedText.Replace("TOTALS FOR:", $"DotNetSummary{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"throughput =", $"{Environment.NewLine}Throughput{Environment.NewLine}throughput");
        }

        private void CalculateThroughputResult()
        {
            string sectionName = "Throughput";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.ThroughputResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DotNetRuntimeMetricsParser.DotNetRuntimeDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.ThroughputResult.SplitDataColumn(columnIndex: 1, DotNetRuntimeMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("Throughput"))
            {
                throw new SchemaException("The DotNetRuntime output file has incorrect format for parsing");
            }
        }
    }
}