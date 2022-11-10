// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for Gzip output document.
    /// </summary>
    public class GzipMetricsParser : MetricsParser
    {
        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex GzipSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by colon and 2 or more spaces.
        /// </summary>
        private static readonly Regex GzipDataTableDelimiter = new Regex(@"(:\s{2,})", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="GzipMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public GzipMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Reduction Ratio for gzip compressor.
        /// </summary>
        public DataTable ReductionRatio { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, GzipSectionDelimiter);
            this.ThrowIfInvalidOutputFormat();
            this.CreateReductionRatioDataTable();

            List<Metric> metrics = new List<Metric>();

            int totalIterations = this.ReductionRatio.Rows.Count;

            for (int i = 0; i < totalIterations; i++)
            {
                metrics.Add(new Metric("ReductionRatio", Convert.ToDouble(this.ReductionRatio.Rows[i].ItemArray[2]), MetricRelativity.HigherIsBetter));
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, "%", " percent");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "-- replaced.*", string.Empty);
            this.PreprocessedText = $"ReductionRatio{Environment.NewLine}" + this.PreprocessedText;
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", $"{Environment.NewLine}");
        }

        private void CreateReductionRatioDataTable()
        {
            string sectionName = "ReductionRatio";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.ReductionRatio = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], GzipMetricsParser.GzipDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.ReductionRatio.SplitDataColumn(columnIndex: 1, GzipMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("ReductionRatio") || (this.Sections["ReductionRatio"] == string.Empty))
            {
                throw new SchemaException("The Gzip results file has incorrect format for parsing");
            }
        }
    }
}