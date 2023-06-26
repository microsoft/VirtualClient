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
    public class HadoopMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex HadoopSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces, so that "N-Body Physics" will not be separated into two cells.
        /// </summary>
        private static readonly Regex HadoopDataTableDelimiter = new Regex(@"=", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="HadoopMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HadoopMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Single core result.
        /// </summary>
        public DataTable FileSystemCounters { get; set; }

        /// <summary>
        /// Multi core result.
        /// </summary>
        public DataTable JobCounters { get; set; }

        /// <summary>
        /// Multi core result.
        /// </summary>
        public DataTable MapReduceFrameworkCounters { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, HadoopMetricsParser.HadoopSectionDelimiter);
            this.ParseFileSystemCounters();
            this.ParseJobCounters();
            this.ParseMapReduceFrameworkCounters();

            List<Metric> metrics = new List<Metric>();

            metrics.AddRange(this.FileSystemCounters.GetMetrics(nameIndex: 0, valueIndex: 1, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.JobCounters.GetMetrics(nameIndex: 0, valueIndex: 1, metricRelativity: MetricRelativity.HigherIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = this.RawText.Replace("File System Counters", $"{Environment.NewLine}File System Counters");
            this.PreprocessedText = this.PreprocessedText.Replace("Job Counters", $"{Environment.NewLine}Job Counters");
            this.PreprocessedText = this.PreprocessedText.Replace("Map-Reduce Framework", $"{Environment.NewLine}Map-Reduce Framework");
        }

        private void ParseFileSystemCounters()
        {
            string sectionName = "File System Counters";
            IList<string> columnNames = new List<string> { "Name", "Value" };
            this.FileSystemCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, columnNames);
        }

        private void ParseJobCounters()
        {
            string sectionName = "Job Counters";
            IList<string> columnNames = new List<string> { "Name", "Value" };
            this.JobCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, columnNames);
        }

        private void ParseMapReduceFrameworkCounters()
        {
            string sectionName = "Map-Reduce Framework";
            IList<string> columnNames = new List<string> { "Name", "Value" };
            this.MapReduceFrameworkCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, columnNames);
        }
    }
}