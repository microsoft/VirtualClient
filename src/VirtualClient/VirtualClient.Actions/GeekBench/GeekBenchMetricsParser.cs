// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using DataTableExtensions = global::VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for geekbench output document
    /// </summary>
    public class GeekBenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// To separate value/unit like '1.86 GB/sec'. This regex looks forward for digit and backward for word.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex GeekBenchSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces, so that "N-Body Physics" will not be separated into two cells.
        /// </summary>
        private static readonly Regex GeekBenchDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="GeekBenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public GeekBenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Single core result.
        /// </summary>
        public DataTable SingleCoreResult { get; set; }

        /// <summary>
        /// Multi core result.
        /// </summary>
        public DataTable MultiCoreResult { get; set; }

        /// <summary>
        /// Single core summary.
        /// </summary>
        public DataTable SingleCoreSummary { get; set; }

        /// <summary>
        /// Multi core summary.
        /// </summary>
        public DataTable MultiCoreSummary { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, GeekBenchMetricsParser.GeekBenchSectionDelimiter);
            this.ParseSingleCoreResult();
            this.ParseMultiCoreResult();
            this.ParseSingleCoreSummary();
            this.ParseMultiCoreSummary();

            List<Metric> metrics = new List<Metric>();

            metrics.AddRange(this.SingleCoreResult.GetMetrics(nameIndex: 0, valueIndex: 3, unitIndex: 4, namePrefix: "SingleCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.SingleCoreResult.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: "SingleCoreScore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MultiCoreResult.GetMetrics(nameIndex: 0, valueIndex: 3, unitIndex: 4, namePrefix: "MultiCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MultiCoreResult.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: "MultiCoreScore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.SingleCoreSummary.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: "SingleCoreSummary-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MultiCoreSummary.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: "MultiCoreSummary-", metricRelativity: MetricRelativity.HigherIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            /*
             * Transforming this:
             *  Benchmark Summary
             *    Single - Core Score              888
             *      Crypto Score                  1091
             *      Integer Score                  777
             *      Floating Point Score           901
             *    Multi - Core Score             12345
             *      Crypto Score                 16208
             *      Integer Score                10518
             *      Floating Point Score         14544
             *
             * Into this:
             *  SingleCoreSummary
             *    Single - Core Score              888
             *      Crypto Score                  1091
             *      Integer Score                  777
             *      Floating Point Score           901
             *
             *  MultiCoreSummary
             *    Multi - Core Score             12345
             *      Crypto Score                 16208
             *      Integer Score                10518
             *      Floating Point Score         14544
             *
             *  So that the data table extensions can correctly add prefix to the summary.
             */
            this.PreprocessedText = this.RawText.Replace("Benchmark Summary", $"SingleCoreSummary");
            this.PreprocessedText = this.PreprocessedText.Replace("Multi-Core Score", $"{Environment.NewLine}MultiCoreSummary{Environment.NewLine}Multi-Core Score");
        }

        private void ParseSingleCoreResult()
        {
            string sectionName = "Single-Core";
            IList<string> columnNames = new List<string> { "Name", "Score", "Measurement" };
            this.SingleCoreResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], GeekBenchMetricsParser.GeekBenchDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.SingleCoreResult.SplitDataColumn(columnIndex: 2, GeekBenchMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseMultiCoreResult()
        {
            string sectionName = "Multi-Core";
            IList<string> columnNames = new List<string> { "Name", "Score", "Measurement" };
            this.MultiCoreResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], GeekBenchMetricsParser.GeekBenchDataTableDelimiter, sectionName, columnNames);
            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.MultiCoreResult.SplitDataColumn(columnIndex: 2, GeekBenchMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseSingleCoreSummary()
        {
            string sectionName = "SingleCoreSummary";
            IList<string> columnNames = new List<string> { "Name", "Score" };
            this.SingleCoreSummary = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], GeekBenchMetricsParser.GeekBenchDataTableDelimiter, sectionName, columnNames);
        }

        private void ParseMultiCoreSummary()
        {
            string sectionName = "MultiCoreSummary";
            IList<string> columnNames = new List<string> { "Name", "Score" };
            this.MultiCoreSummary = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], GeekBenchMetricsParser.GeekBenchDataTableDelimiter, sectionName, columnNames);
        }
    }
}