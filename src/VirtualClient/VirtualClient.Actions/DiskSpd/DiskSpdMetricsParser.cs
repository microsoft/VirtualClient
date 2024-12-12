// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;
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

        private string commandLine;
        private ReadWriteMode readWriteMode;

        /// <summary>
        /// Constructor for <see cref="DiskSpdMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="commandLine">DiskSpd commandline</param>
        public DiskSpdMetricsParser(string rawText, string commandLine)
            : base(rawText)
        {
            this.commandLine = commandLine;
            this.Metrics = new List<Metric>();
            this.readWriteMode = ReadWriteMode.ReadWrite;
        }

        private enum ReadWriteMode
        {
            ReadWrite,
            ReadOnly,
            WriteOnly,
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, DiskSpdMetricsParser.DiskSpdSectionDelimiter);

                this.ParseCPUResult();
                this.ParseTotalIoResult();

                if (this.readWriteMode != ReadWriteMode.WriteOnly)
                {
                    this.ParseReadIoResult();
                }

                if (this.readWriteMode != ReadWriteMode.ReadOnly)
                {
                    this.ParseWriteIoResult();
                }

                this.ParseLatencyResult();

                return this.Metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException($"Results parsing failed for 'diskspd' workload.", exc, ErrorReason.WorkloadResultsParsingFailed);
            }
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
             * 
             * Convert:
             * Group | CPU |  Usage |  User  |  Kernel |  Idle
             * 
             * To:
             * CPU
             * Group | CPU |  Usage |  User  |  Kernel |  Idle
             */

            if (this.PreprocessedText.Contains("Group"))
            {
                this.PreprocessedText = this.PreprocessedText.Replace("Group", $"CPU{Environment.NewLine}Group");
            }
            else
            {
                this.PreprocessedText = this.PreprocessedText.Replace("CPU", $"CPU{Environment.NewLine}CPU");
            }

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

            // Change all the 'avg:' to 'average'
            this.PreprocessedText = this.PreprocessedText.Replace("avg.", $"average");

            // change all 'I/O per s' to 'iops'
            this.PreprocessedText = this.PreprocessedText.Replace("I/O per s", $"iops");

            // Removing % sign as it will be reflected in the unit.
            this.PreprocessedText = this.PreprocessedText.Replace("%", string.Empty);

            // AvgLat -> latency average
            this.PreprocessedText = this.PreprocessedText.Replace("AvgLat", "latency average");

            // LatStdDev -> latency stdev
            this.PreprocessedText = this.PreprocessedText.Replace("LatStdDev", "latency stdev");

            // IopsStdDev -> iops stdev
            this.PreprocessedText = this.PreprocessedText.Replace("IopsStdDev", "iops stdev");

            // I/Os -> I/O operations
            this.PreprocessedText = this.PreprocessedText.Replace("I/Os", "io operations");

            // MiB/s -> throughput
            this.PreprocessedText = this.PreprocessedText.Replace("MiB/s", "throughput");

            if (this.commandLine.Contains("-w100"))
            {
                this.readWriteMode = ReadWriteMode.WriteOnly;
            }
            else if (this.commandLine.Contains("-w0"))
            {
                this.readWriteMode = ReadWriteMode.ReadOnly;
            }
        }

        private void ParseCPUResult()
        {
            string sectionName = "CPU";

            if (this.Sections[sectionName].Contains("Group"))
            {
                this.Sections[sectionName] = this.ProcessAndUpdateString(this.Sections[sectionName]);
            }

            DataTable cpuUsage = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);

            this.Metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[1].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: MetricVerbosity.Informational));
            this.Metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[2].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: MetricVerbosity.Informational));
            this.Metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[3].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: MetricVerbosity.Informational));
        }

        private void ParseTotalIoResult()
        {
            string sectionName = "Total IO";
            DataTable totalIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"total {totalIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"total {totalIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"total {totalIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"total {totalIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"total {totalIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));

            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = MetricVerbosity.Critical;
            }

            this.Metrics.AddRange(metrics);
        }

        private void ParseReadIoResult()
        {
            string sectionName = "Read IO";
            DataTable readIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"read {readIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"read {readIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"read {readIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"read {readIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"read {readIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));

            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = MetricVerbosity.Critical;
            }

            this.Metrics.AddRange(metrics);
        }

        private void ParseWriteIoResult()
        {
            string sectionName = "Write IO";
            DataTable writeIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"write {writeIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"write {writeIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: MetricVerbosity.Informational));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"write {writeIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"write {writeIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"write {writeIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter));
            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = MetricVerbosity.Critical;
            }

            this.Metrics.AddRange(metrics);
        }

        private void ParseLatencyResult()
        {
            string sectionName = "Latency";
            DataTable latency = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            if (this.readWriteMode != ReadWriteMode.WriteOnly)
            {
                metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "ms", namePrefix: "read latency ", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            if (this.readWriteMode != ReadWriteMode.ReadOnly)
            {
                metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "ms", namePrefix: "write latency ", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "ms", namePrefix: "total latency ", metricRelativity: MetricRelativity.LowerIsBetter));

            string[] criticalMetrics = { "total latency 50th", "total latency 90th", "total latency 99th" };
            foreach (var metric in metrics.Where(m => criticalMetrics.Contains(m.Name)))
            {
                metric.Verbosity = MetricVerbosity.Critical;
            }

            this.Metrics.AddRange(metrics);
        }

        private string ProcessAndUpdateString(string input)
        {
            string[] lines = input.Split('\n');

            lines[0] = lines[0].Replace("Group | CPU", "CPU");

            for (int i = 1; i < lines.Length; i++)
            {
                string[] columns = lines[i].Split('|', StringSplitOptions.RemoveEmptyEntries);

                if (columns.Length >= 2 && int.TryParse(columns[0].Trim(), out int group) && int.TryParse(columns[1].Trim(), out int cpu))
                {
                    lines[i] = Regex.Replace(lines[i], @$"\b{group}\s*\|\s*{cpu}\b", $"{(64 * group) + cpu}");
                }
            }

            return string.Join('\n', lines);
        }
    }
}
