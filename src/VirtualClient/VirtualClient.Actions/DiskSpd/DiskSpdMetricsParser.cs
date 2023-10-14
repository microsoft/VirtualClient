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
    /// Parser for diskspd metrics.
    /// </summary>
    public class DiskSpdMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex DiskSpdSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by '|' and trim surrouding spaces..
        /// </summary>
        private static readonly Regex DiskSpdDataTableDelimiter = new Regex(@"( )*(\|)( )*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Matches lines with all dashes.
        /// </summary>
        private static readonly Regex DashLineRegex = new Regex(@"(-){2,}(\s)*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="DiskSpdMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DiskSpdMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Cpu usage.
        /// </summary>
        public DataTable CpuUsage { get; set; }

        /// <summary>
        /// Total IO result table
        /// </summary>
        public DataTable TotalIo { get; set; }

        /// <summary>
        /// Read IO result table
        /// </summary>
        public DataTable ReadIo { get; set; }

        /// <summary>
        /// Write IO result table
        /// </summary>
        public DataTable WriteIo { get; set; }

        /// <summary>
        /// Latency result table
        /// </summary>
        public DataTable Latency { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, DiskSpdMetricsParser.DiskSpdSectionDelimiter);

            this.ParseCPUResult();
            this.ParseTotalIoResult();
            this.ParseReadIoResult();
            this.ParseWriteIoResult();
            this.ParseLatencyResult();

            List<Metric> metrics = new List<Metric>();

            metrics.AddRange(this.TotalIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"total {this.TotalIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.TotalIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"total {this.TotalIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.TotalIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"total {this.TotalIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.TotalIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"total {this.TotalIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.TotalIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"total {this.TotalIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));

            metrics.AddRange(this.ReadIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"read {this.ReadIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.ReadIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"read {this.ReadIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.ReadIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"read {this.ReadIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.ReadIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"read {this.ReadIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.ReadIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"read {this.ReadIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));

            metrics.AddRange(this.WriteIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"write {this.WriteIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WriteIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"write {this.WriteIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WriteIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"write {this.WriteIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WriteIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"write {this.WriteIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WriteIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"write {this.WriteIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));

            metrics.AddRange(this.Latency.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "ms", namePrefix: "read latency ", metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.Latency.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "ms", namePrefix: "write latency ", metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.Latency.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "ms", namePrefix: "total latency ", metricRelativity: MetricRelativity.LowerIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.RawText, DiskSpdMetricsParser.DashLineRegex);

            /*
             * Giving the CPU table a title
             * 
             * Convert:
             * CPU |  Usage |  User  |  Kernel |  Idle
             * 
             * To:
             * CPU
             * CPU |  Usage |  User  |  Kernel |  Idle
             */

            this.PreprocessedText = this.PreprocessedText.Replace("CPU", $"CPU{Environment.NewLine}CPU");

            /*
             * Replace total: to it's actual TableName "Latency"
             * 
             * total:
             *   %-ile |  Read (ms) | Write (ms) | Total (ms)
             *   
             * To:
             * 
             * Latency:
             *   %-ile |  Read (ms) | Write (ms) | Total (ms)
             */
            this.PreprocessedText = this.PreprocessedText.Replace($"total:{Environment.NewLine}", $"Latency{Environment.NewLine}");

            // Change all the 'total:' to 'total|'
            this.PreprocessedText = this.PreprocessedText.Replace("total:", $"total|");

            // change all 'I/O per s' to 'iops'
            this.PreprocessedText = this.PreprocessedText.Replace("I/O per s", $"iops");

            // Removing % sign as it will be reflected in the unit.
            this.PreprocessedText = this.PreprocessedText.Replace("%", string.Empty);

            // LatStdDev -> latency stdev
            this.PreprocessedText = this.PreprocessedText.Replace("LatStdDev", "latency stdev");

            // IopsStdDev -> iops stdev
            this.PreprocessedText = this.PreprocessedText.Replace("IopsStdDev", "iops stdev");

            // I/Os -> I/O operations
            this.PreprocessedText = this.PreprocessedText.Replace("I/Os", "I/O operations");

            // MiB/s -> throughput
            this.PreprocessedText = this.PreprocessedText.Replace("MiB/s", "throughput");
        }

        private void ParseCPUResult()
        {
            string sectionName = "CPU";
            this.CpuUsage = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
        }

        private void ParseTotalIoResult()
        {
            string sectionName = "Total IO";
            this.TotalIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
        }

        private void ParseReadIoResult()
        {
            string sectionName = "Read IO";
            this.ReadIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
        }

        private void ParseWriteIoResult()
        {
            string sectionName = "Write IO";
            this.WriteIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
        }

        private void ParseLatencyResult()
        {
            string sectionName = "Latency";
            this.Latency = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
        }
    }
}
