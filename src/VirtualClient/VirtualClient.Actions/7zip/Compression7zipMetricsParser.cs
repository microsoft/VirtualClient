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
    /// Parser for Compressor7zip output document.
    /// </summary>
    public class Compression7zipMetricsParser : MetricsParser
    {
        /// <summary>
        /// Identifies string not containing words 'bytes' and 'Time'.
        /// </summary>
        private static readonly Regex BytesOrTimeLinesRegex = new Regex(@"^((?!bytes|Time).)*$", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex Compressor7zipSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by colon and 2 or more spaces.
        /// </summary>
        private static readonly Regex Compressor7zipDataTableDelimiter = new Regex(@"(:\s{1,})", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="Compression7zipMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public Compression7zipMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Sizes and Time for Compressor7zip compressor.
        /// </summary>
        public DataTable SizeAndTime { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, Compressor7zipSectionDelimiter);
            this.CreateSizeAndTimeDataTable();
            this.ThrowIfInvalidOutputFormat();

            List<Metric> metrics = new List<Metric>();
            int rows = this.SizeAndTime.Rows.Count;

            metrics.Add(new Metric("Compressed size and Original size ratio", (Convert.ToDouble(this.SizeAndTime.Rows[2].ItemArray[2]) / Convert.ToDouble(this.SizeAndTime.Rows[0].ItemArray[2])) * 100, MetricRelativity.LowerIsBetter));
            double compressionTime = 0;

            for (int i = rows - 1; i > 2; i--)
            {
                compressionTime += Convert.ToDouble(this.SizeAndTime.Rows[i].ItemArray[2]);
            }

            metrics.Add(new Metric("CompressionTime", compressionTime, MetricUnit.Seconds, MetricRelativity.LowerIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[\r\n|\n]+", $"{Environment.NewLine}");
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, Compression7zipMetricsParser.BytesOrTimeLinesRegex);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "Time =", "Time:");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "=.*", "seconds");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"\(.*\)", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "[1-9]+.*,", string.Empty);
            this.PreprocessedText = $"SizeAndTime{Environment.NewLine} Original Size: " + this.PreprocessedText;
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", $"{Environment.NewLine}");
        }

        private void CreateSizeAndTimeDataTable()
        {
            string sectionName = "SizeAndTime";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.SizeAndTime = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], Compression7zipMetricsParser.Compressor7zipDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.SizeAndTime.SplitDataColumn(columnIndex: 1, Compression7zipMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            int rows = this.SizeAndTime.Rows.Count;

            if (rows < 7)
            {
                throw new SchemaException("The Compressor7zip results file has incorrect data for parsing");
            }
        }
    }
}