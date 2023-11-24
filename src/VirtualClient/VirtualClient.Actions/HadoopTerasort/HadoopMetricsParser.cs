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
    /// Parser for Hadoop output document
    /// </summary>
    public class HadoopMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex HadoopSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values using the equals to delimeter.
        /// </summary>
        private static readonly Regex HadoopDataTableDelimiter = new Regex(@"=", RegexOptions.ExplicitCapture);

        /// <summary>
        /// To separate value/unit like '1.86 GB/sec'. This regex looks forward for digit and backward for word.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(\S.*?)\s*\(([^)]*)\)*", RegexOptions.None);

        /// <summary>
        /// Column Values in the Hadoop output.
        /// </summary>
        private IList<string> columnNames = new List<string> { "Name", "Value" };

        /// <summary>
        /// Split Column Values to find the unit in the Hadoop output.
        /// </summary>
        private IList<string> splitColumnNames = new List<string> { "RowName", "Unit" };

        /// <summary>
        /// Constructor for <see cref="HadoopMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HadoopMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// File System Counters result.
        /// </summary>
        public DataTable FileSystemCounters { get; set; }

        /// <summary>
        /// Job Counters result.
        /// </summary>
        public DataTable JobCounters { get; set; }

        /// <summary>
        /// Map Reduce Framework result.
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

            List<Metric> metrics =
            [
                .. this.FileSystemCounters.GetMetrics(nameIndex: 0, valueIndex: 1, metricRelativity: MetricRelativity.LowerIsBetter),
                .. this.JobCounters.GetMetrics(nameIndex: 0, valueIndex: 1, metricRelativity: MetricRelativity.HigherIsBetter),
                .. this.MapReduceFrameworkCounters.GetMetrics(nameIndex: 0, valueIndex: 1, metricRelativity: MetricRelativity.HigherIsBetter),
            ];

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
            
            this.FileSystemCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
        }

        private void ParseJobCounters()
        {
            string sectionName = "Job Counters";
            this.JobCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
        }

        private void ParseMapReduceFrameworkCounters()
        {
            string sectionName = "Map-Reduce Framework";
            this.MapReduceFrameworkCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
        }
    }
}