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
    /// Parser for Pbzip2 output document.
    /// </summary>
    public class Pbzip2MetricsParser : MetricsParser
    {
        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Identifies string not containing words 'Size' and 'Clock'.
        /// </summary>
        private static readonly Regex SizeOrClockLinesRegex = new Regex(@"^((?!Input Size|Output Size|Clock).)*$", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex Pbzip2SectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex Pbzip2DataTableDelimiter = new Regex(@"(:\s)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="Pbzip2MetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="compression">Compression scenario or Decompression scenario</param>
        public Pbzip2MetricsParser(string rawText, bool compression)
            : base(rawText)
        {
            this.Compression = compression;
        }

        /// <summary>
        /// Execution Times for Pbzip2 Simulations.
        /// </summary>
        public DataTable SizeAndTime { get; set; }

        /// <summary>
        /// Compression true depicts compression scenario else decompression scenario
        /// </summary>
        protected bool Compression { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, Pbzip2SectionDelimiter);
            this.ThrowIfInvalidOutputFormat();
            this.CreateSizeAndTimeDataTable();

            List<Metric> metrics = new List<Metric>();
            double totalInputSize, totalOutputSize, compressionTime;

            this.CalculateTotalSizeAndTime(out totalInputSize, out totalOutputSize, out compressionTime);

            if (this.Compression)
            {
                metrics.Add(new Metric("Compressed size and Original size ratio", (totalOutputSize / totalInputSize) * 100, MetricRelativity.LowerIsBetter));
            }
            else
            {
                metrics.Add(new Metric("Decompressed size and Original size ratio", (totalOutputSize / totalInputSize) * 100, MetricRelativity.HigherIsBetter));
            }

            metrics.Add(new Metric("CompressionTime", compressionTime, MetricUnit.Seconds, MetricRelativity.LowerIsBetter));
            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[\r\n|\n]+", $"{Environment.NewLine}");
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, Pbzip2MetricsParser.SizeOrClockLinesRegex);
            this.PreprocessedText = $"SizeAndTime{Environment.NewLine}" + this.PreprocessedText;
        }

        private void CreateSizeAndTimeDataTable()
        {
            string sectionName = "SizeAndTime";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.SizeAndTime = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], Pbzip2MetricsParser.Pbzip2DataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.SizeAndTime.SplitDataColumn(columnIndex: 1, Pbzip2MetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void CalculateTotalSizeAndTime(out double totalInputSize, out double totalOutputSize, out double compressionTime)
        {
            int totalIterations = this.SizeAndTime.Rows.Count;

            totalInputSize = 0;
            totalOutputSize = 0;
            compressionTime = 0;

            for (int i = 0; i < totalIterations - 1; i = i + 2)
            {
                totalInputSize += Convert.ToDouble(this.SizeAndTime.Rows[i].ItemArray[2]);
                totalOutputSize += Convert.ToDouble(this.SizeAndTime.Rows[i + 1].ItemArray[2]);
            }

            compressionTime = Convert.ToDouble(this.SizeAndTime.Rows[totalIterations - 1].ItemArray[2]);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("SizeAndTime") || (this.Sections["SizeAndTime"] == string.Empty))
            {
                throw new SchemaException("The Pbzip2 results file has incorrect format for parsing");
            }
        }
    }
}