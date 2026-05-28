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
    ///  Parser for Microsoft's Stream results.
    /// </summary>
    public class StreamMsftMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex StreamSectionDelimiter = new Regex($"({Environment.NewLine})(\\s)*({Environment.NewLine})", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces, so that "N-Body Physics" will not be separated into two cells.
        /// </summary>
        private static readonly Regex StreamDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="StreamMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public StreamMsftMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Stream Results.
        /// </summary>
        public DataTable StreamResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, StreamMsftMetricsParser.StreamSectionDelimiter);

            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("RESULTS TABLE"))
            {
                throw new SchemaException("The Stream results has incorrect format/data for parsing");
            }

            this.ParseWorkloadResult();

            List<Metric> metrics = new List<Metric>();

            for (int index = 0; index < this.StreamResult.Columns.Count; index++)
            {
                string metricName = this.StreamResult.Columns[index].ColumnName;
                string unit = "MBps";
                MetricRelativity metricRelativity = MetricRelativity.HigherIsBetter;

                if (metricName.Contains("Rate", StringComparison.OrdinalIgnoreCase))
                {
                    metricRelativity = MetricRelativity.HigherIsBetter;
                    unit = "MBps";
                }
                else if (metricName.Contains("Latency", StringComparison.OrdinalIgnoreCase))
                {
                    metricRelativity = MetricRelativity.LowerIsBetter;
                    unit = "ns";
                }

                metrics.AddRange(this.StreamResult.GetMetrics(nameIndex: 0, valueIndex: index, unit: unit, namePrefix: $"{metricName} ", metricRelativity: metricRelativity));
            }

            if (metrics.Count == 0)
            {
                throw new SchemaException($"The Stream results has incorrect format/data for parsing. Output is having 0 metrics.");
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Converting all CRLF(Windows EOL) to LF(Unix EOL).
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");

            // Converting all LF to Environment.NewLine
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", Environment.NewLine);

            // Replacing dash lines with new lines.
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(-){3,}", Environment.NewLine);

            // Removing ":" from metric names.
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @":", string.Empty);

            // Creating section for the results table with the name "RESULTS TABLE".
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Function", $"{Environment.NewLine}RESULTS TABLE{Environment.NewLine}Function");

            // Creating columns in case of best rate heading
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Function(\s)*Best(\s)*Rate(\s)*MB/s", $"Function  Best Rate  Avg Rate  Min Rate  Avg Latency  Min Latency  Max Latency");

            // Extra Space for delimeter
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"LATENCY", $"ns  ");
            
            // Creating columns in case of latency 
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Function(\s)*Best(\s)*Latency(\s)*ns", $"Function  Avg Latency  Min Latency  Max Latency");

            // Removing report tile (Not required Section).
            Regex reportTitle = new Regex(@"Stream Report");
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, reportTitle);

            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.PreprocessedText.Trim();
        }

        private void ParseWorkloadResult()
        {
            string sectionName = "RESULTS TABLE";
            this.StreamResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], StreamMsftMetricsParser.StreamDataTableDelimiter, sectionName);
        }
    }
}