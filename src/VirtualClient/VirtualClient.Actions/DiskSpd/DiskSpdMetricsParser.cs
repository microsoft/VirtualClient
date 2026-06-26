// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.Logging;
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

        /// <summary>
        /// Matches the DiskSpd CPU-utilization table header by its invariant trailing signature
        /// ("CPU |  Usage |  User  | Kernel |  Idle"), regardless of how many leading topology
        /// columns (Socket/Node/Group/Core/Class) DiskSpd prepended for the system's topology.
        /// </summary>
        private static readonly Regex CpuTableHeaderRegex = new Regex(
            @"\bCPU\s*\|\s*Usage\s*\|\s*User\s*\|\s*Kernel\s*\|\s*Idle",
            RegexOptions.ExplicitCapture);

        private string commandLine;
        private ReadWriteMode readWriteMode;
        private List<Metric> metrics;

        /// <summary>
        /// Constructor for <see cref="DiskSpdMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="commandLine">DiskSpd commandline</param>
        public DiskSpdMetricsParser(string rawText, string commandLine)
            : base(rawText)
        {
            this.commandLine = commandLine;
            this.metrics = new List<Metric>();
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

                return this.metrics;
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
             * DiskSpd prefixes the CPU-utilization table's "CPU" column with a dynamic, hierarchical
             * set of topology columns. Each one is emitted only when the system has more than one of
             * that unit, in this fixed order (see DiskSpd ResultParser.cpp _PrintCpuUtilization):
             *
             *     [Socket |] [Node |] [Group |] [Core |] [Class |] CPU |  Usage |  User  | Kernel |  Idle
             *
             *   - Socket : > 1 socket
             *   - Node   : > 1 NUMA node
             *   - Group  : > 1 processor group (i.e. > 64 vCPUs)
             *   - Core   : SMT / hyper-threading enabled
             *   - Class  : heterogeneous (performance/efficiency) cores
             *
             * Earlier fixes special-cased two exact header strings ("Socket | Node | Group | Core | CPU"
             * and bare "Core | CPU"), so any other combination - e.g. a 64-vCPU VM with 2 NUMA nodes but
             * 1 group emitting "Node | Core | CPU" - was mis-titled and threw KeyNotFoundException('CPU').
             * NormalizeCpuTable handles every combination (including future columns) by keying off the
             * invariant "CPU | Usage | ..." signature instead of hard-coded prefixes.
             */
            this.NormalizeCpuTable();

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

            /*
             * DiskSpd v2.2.0 renamed the latency percentile section header from "total:" to
             * "Total latency distribution:". Normalize the new header to the "Latency" section
             * title as well, otherwise ParseLatencyResult throws KeyNotFoundException('Latency').
             *
             * Total latency distribution:
             *   %-ile |  Read (ms) | Write (ms) | Total (ms)
             */
            this.PreprocessedText = this.PreprocessedText.Replace($"Total latency distribution:{Environment.NewLine}", $"Latency{Environment.NewLine}");

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

        /// <summary>
        /// Normalizes the CPU-utilization table to the canonical "[Group |] CPU |  Usage | ..." form
        /// and gives it a "CPU" section title, independent of which leading topology columns
        /// (Socket/Node/Group/Core/Class) DiskSpd emitted for the current system. The Group column,
        /// when present (> 64 vCPUs), is retained so that ParseCPUResult can map the group-relative
        /// CPU number to a unique processor id; all other leading columns are dropped.
        /// </summary>
        private void NormalizeCpuTable()
        {
            string text = this.PreprocessedText;
            string newLine = text.Contains("\r\n") ? "\r\n" : "\n";

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimEnd('\r');
            }

            int headerIndex = Array.FindIndex(lines, line => CpuTableHeaderRegex.IsMatch(line));
            if (headerIndex < 0)
            {
                // No CPU table found (unexpected for DiskSpd output); leave the text untouched.
                return;
            }

            string[] headerColumns = lines[headerIndex].Split('|');
            int cpuColumn = Array.FindIndex(headerColumns, column => column.Trim() == "CPU");
            int groupColumn = Array.FindIndex(headerColumns, column => column.Trim() == "Group");
            bool hasGroup = groupColumn >= 0;

            if (cpuColumn < 0)
            {
                return;
            }

            List<string> output = new List<string>(lines.Length + 1);
            bool tableComplete = false;
            bool sawDataRow = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (tableComplete || i < headerIndex)
                {
                    output.Add(lines[i]);
                    continue;
                }

                if (i == headerIndex)
                {
                    // "CPU" becomes the section title (consumed by Sectionize as the section key);
                    // the rebuilt header becomes the column-name row consumed by ConvertToDataTable.
                    output.Add("CPU");
                    output.Add(BuildCpuRow(headerColumns, cpuColumn, groupColumn, hasGroup));
                    continue;
                }

                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    // A blank line terminates the CPU table, but only once its data rows are consumed
                    // (DiskSpd may leave a blank line between the header and the first data row).
                    if (sawDataRow)
                    {
                        tableComplete = true;
                    }

                    output.Add(line);
                    continue;
                }

                string[] columns = line.Split('|');
                if (columns.Length == headerColumns.Length && int.TryParse(columns[cpuColumn].Trim(), out _))
                {
                    sawDataRow = true;
                    output.Add(BuildCpuRow(columns, cpuColumn, groupColumn, hasGroup));
                }
                else
                {
                    // The "avg." summary row carries no topology columns; keep it verbatim.
                    output.Add(line);
                }
            }

            this.PreprocessedText = string.Join(newLine, output);
        }

        private static string BuildCpuRow(string[] columns, int cpuColumn, int groupColumn, bool hasGroup)
        {
            List<string> kept = new List<string>();
            if (hasGroup)
            {
                kept.Add(columns[groupColumn].Trim());
            }

            for (int i = cpuColumn; i < columns.Length; i++)
            {
                kept.Add(columns[i].Trim());
            }

            return string.Join(" | ", kept);
        }

        private void ParseCPUResult()
        {
            string sectionName = "CPU";

            if (this.Sections[sectionName].Contains("Group"))
            {
                this.Sections[sectionName] = this.ProcessAndUpdateString(this.Sections[sectionName]);
            }
            else if (this.Sections[sectionName].Contains("Core"))
            {
                this.Sections[sectionName] = this.RemoveCoreColumn(this.Sections[sectionName]);
            }

            DataTable cpuUsage = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);

            this.metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[1].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 5));
            this.metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[2].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 5));
            this.metrics.AddRange(cpuUsage.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "percentage", namePrefix: $"cpu {cpuUsage.Columns[3].ColumnName.ToLower()} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 5));
        }

        private void ParseTotalIoResult()
        {
            string sectionName = "Total IO";
            DataTable totalIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"total {totalIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"total {totalIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"total {totalIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"total {totalIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(totalIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"total {totalIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));

            // Total metrics ending with "total" remain at level 1 (most important)
            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = 1; // Keep at 1 for critical
            }

            this.metrics.AddRange(metrics);
        }

        private void ParseReadIoResult()
        {
            string sectionName = "Read IO";
            DataTable readIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"read {readIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"read {readIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"read {readIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"read {readIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(readIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"read {readIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));

            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = 1;
            }

            this.metrics.AddRange(metrics);
        }

        private void ParseWriteIoResult()
        {
            string sectionName = "Write IO";
            DataTable writeIo = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "bytes", namePrefix: $"write {writeIo.Columns[1].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "I/Os", namePrefix: $"write {writeIo.Columns[2].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 5));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "MiB/s", namePrefix: $"write {writeIo.Columns[3].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 4, unit: "iops", namePrefix: $"write {writeIo.Columns[4].ColumnName} ", metricRelativity: MetricRelativity.HigherIsBetter, metricVerbosity: 1));
            metrics.AddRange(writeIo.GetMetrics(nameIndex: 0, valueIndex: 5, unit: "ms", namePrefix: $"write {writeIo.Columns[5].ColumnName} ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));

            foreach (var metric in metrics.Where(m => m.Name.EndsWith("total") && (m.Unit == "iops" || m.Unit == "ms" || m.Unit == "MiB/s")))
            {
                metric.Verbosity = 1;
            }

            this.metrics.AddRange(metrics);
        }

        private void ParseLatencyResult()
        {
            string sectionName = "Latency";
            DataTable latency = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DiskSpdMetricsParser.DiskSpdDataTableDelimiter, sectionName, columnNames: null);
            List<Metric> metrics = new List<Metric>();
            if (this.readWriteMode != ReadWriteMode.WriteOnly)
            {
                metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "ms", namePrefix: "read latency ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));
            }

            if (this.readWriteMode != ReadWriteMode.ReadOnly)
            {
                metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "ms", namePrefix: "write latency ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));
            }

            metrics.AddRange(latency.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "ms", namePrefix: "total latency ", metricRelativity: MetricRelativity.LowerIsBetter, metricVerbosity: 1));

            string[] criticalMetrics = { "total latency 50th", "total latency 90th", "total latency 99th" };
            foreach (var metric in metrics.Where(m => criticalMetrics.Contains(m.Name)))
            {
                metric.Verbosity = 1; // Keep at 1 for critical percentiles
            }

            this.metrics.AddRange(metrics);
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

        private string RemoveCoreColumn(string input)
        {
            string[] lines = input.Split('\n');

            // Header: "Core | CPU |  Usage | ..." -> "CPU |  Usage | ..."
            lines[0] = lines[0].Replace("Core | CPU", "CPU");

            for (int i = 1; i < lines.Length; i++)
            {
                // Data rows look like "<core> | <cpu> |  <usage> | ...". Drop the leading
                // Core column, keeping CPU as the row identifier (matching the canonical
                // single-column "CPU" format). The trailing average row has no Core column
                // and is left unchanged.
                lines[i] = Regex.Replace(lines[i], @"^\s*\d+\s*\|\s*(\d+)\s*\|", "$1|");
            }

            return string.Join('\n', lines);
        }
    }
}
