// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
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
        private static readonly Regex HadoopSectionDelimiter = new Regex(@"(\r\n)(\s)*(\r\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values using the equals to delimeter.
        /// </summary>
        private static readonly Regex HadoopDataTableDelimiter = new Regex(@"=", RegexOptions.ExplicitCapture);

        /// <summary>
        /// To separate value/unit like '7863 ms'. This regex looks forward for digit and backward for word with a space between them.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Column Values in the Hadoop output.
        /// </summary>
        private IList<string> columnNames = new List<string> { "Name", "Measurement" };

        /// <summary>
        /// Split Column Values to find the unit in the Hadoop output.
        /// </summary>
        private IList<string> measurementSplitColumnNames = new List<string> { "Value", "Unit" };

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

        /// <summary>
        /// Combined metrics of all the sections.
        /// </summary>
        public List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, HadoopMetricsParser.HadoopSectionDelimiter);
            this.ParseFileSystemCounters();
            this.ParseJobCounters();
            this.ParseMapReduceFrameworkCounters();

            this.Metrics =
            [
                .. this.FileSystemCounters.GetMetrics(nameIndex: 0, unitIndex: 3, valueIndex: 2, metricRelativity: MetricRelativity.Undefined),
                .. this.JobCounters.GetMetrics(nameIndex: 0, unitIndex: 3, valueIndex: 2, metricRelativity: MetricRelativity.Undefined),
                .. this.MapReduceFrameworkCounters.GetMetrics(nameIndex: 0, unitIndex: 3, valueIndex: 2, metricRelativity: MetricRelativity.Undefined),
            ];

            // Job Counters
            this.ModifyMetricRelativity("Total time spent by all maps in occupied slots (ms)", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("Total time spent by all reduces in occupied slots (ms)", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("Total time spent by all map tasks (ms)", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("Total time spent by all reduce tasks (ms)", MetricRelativity.LowerIsBetter);

            // Map-Reduce Framework
            this.ModifyMetricRelativity("Spilled Records", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("Failed Shuffles", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("GC time elapsed (ms)", MetricRelativity.LowerIsBetter);
            this.ModifyMetricRelativity("CPU time spent (ms)", MetricRelativity.LowerIsBetter);

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            List<string> result = new List<string>();
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", "\r\n");
            this.PreprocessedText = this.PreprocessedText.Trim();

            this.PreprocessedText = this.PreprocessedText.Replace("File System Counters", $"\r\nFile System Counters");
            this.PreprocessedText = this.PreprocessedText.Replace("Job Counters", $"\r\nJob Counters");
            this.PreprocessedText = this.PreprocessedText.Replace("Map-Reduce Framework", $"\r\nMap-Reduce Framework");

            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"org\.apache\.hadoop\.examples\.terasort\.TeraGen\$Counters[ \r]*\n", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"File Input Format Counters[ \r]*\n", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"File Output Format Counters[ \r]*\n", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Shuffle Errors[ \r]*\n", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @".* INFO terasort\.TeraSort: done", string.Empty);

            List<string> rows = this.PreprocessedText.Split("\r\n", StringSplitOptions.None).ToList();
            foreach (string row in rows)
            {
                string rownew = row;
                if (row.Contains("bytes", StringComparison.OrdinalIgnoreCase))
                {
                    rownew = row + " bytes";
                }

                if (row.Contains("ms", StringComparison.OrdinalIgnoreCase))
                {
                    rownew = row + " ms";
                }

                result.Add(rownew);
            }

            this.PreprocessedText = string.Join("\r\n", result);
        }

        private void ParseFileSystemCounters()
        {
            string sectionName = "File System Counters";
            this.FileSystemCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
            this.FileSystemCounters.SplitDataColumn(columnIndex: 1, HadoopMetricsParser.ValueUnitSplitRegex, this.measurementSplitColumnNames);
            this.FileSystemCounters.ReplaceEmptyCell("count");
        }

        private void ParseJobCounters()
        {
            string sectionName = "Job Counters";
            this.JobCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
            this.JobCounters.SplitDataColumn(columnIndex: 1, HadoopMetricsParser.ValueUnitSplitRegex, this.measurementSplitColumnNames);
            this.JobCounters.ReplaceEmptyCell("count");
        }

        private void ParseMapReduceFrameworkCounters()
        {
            string sectionName = "Map-Reduce Framework";
            this.MapReduceFrameworkCounters = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HadoopMetricsParser.HadoopDataTableDelimiter, sectionName, this.columnNames);
            this.MapReduceFrameworkCounters.SplitDataColumn(columnIndex: 1, HadoopMetricsParser.ValueUnitSplitRegex, this.measurementSplitColumnNames);
            this.MapReduceFrameworkCounters.ReplaceEmptyCell("count");
        }

        private void ModifyMetricRelativity(string metricName, MetricRelativity relativity)
        {
            Metric metricToModify = this.Metrics.FirstOrDefault(m => m.Name == metricName);
            if (metricToModify != null)
            {
                metricToModify.Relativity = relativity;
            }
        }
    }
}
