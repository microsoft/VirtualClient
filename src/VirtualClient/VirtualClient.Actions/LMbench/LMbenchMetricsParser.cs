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
    /// Parser for LMbench output document
    /// </summary>
    public class LMbenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex LMbenchSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="LMbenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public LMbenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Rrocessor, Processes - times in microseconds - smaller is better
        /// </summary>
        public DataTable ProcessorTimes { get; set; }

        /// <summary>
        /// Basic integer operations - times in nanoseconds - smaller is better
        /// </summary>
        public DataTable BasicInt { get; set; }

        /// <summary>
        /// Basic float operations - times in nanoseconds - smaller is better
        /// </summary>
        public DataTable BasicFloat { get; set; }

        /// <summary>
        /// Basic double operations - times in nanoseconds - smaller is better
        /// </summary>
        public DataTable BasicDouble { get; set; }

        /// <summary>
        /// Context switching - times in microseconds - smaller is better
        /// </summary>
        public DataTable ContextSwitching { get; set; }

        /// <summary>
        /// *Local* Communication latencies in microseconds - smaller is better
        /// </summary>
        public DataTable CommunicationLatency { get; set; }

        /// <summary>
        /// File and VM system latencies in microseconds - smaller is better
        /// </summary>
        public DataTable FileVmLatency { get; set; }

        /// <summary>
        /// *Local* Communication bandwidths in MB/s - bigger is better
        /// </summary>
        public DataTable CommunicationBandwidth { get; set; }

        /// <summary>
        /// Memory latencies in nanoseconds - smaller is better
        /// </summary>
        public DataTable MemoryLatency { get; set; }

        /// <summary>
        /// True if the results have been parsed.
        /// </summary>
        protected bool IsParsed
        {
            get
            {
                return this.ProcessorTimes != null && this.BasicInt != null 
                    && this.BasicFloat != null && this.BasicDouble != null 
                    && this.ContextSwitching != null && this.CommunicationLatency != null
                    && this.FileVmLatency != null && this.CommunicationBandwidth != null 
                    && this.MemoryLatency != null;
            }
        } 

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LMbenchMetricsParser.LMbenchSectionDelimiter);

            // LMbench sometimes skipp those four measurements if it can't figure out the OS. Those tables are made optional.
            this.ParseProcessorTimes();
            this.ParseBasicInt();
            this.ParseBasicFloat();
            this.ParseBasicDouble();

            this.ParseContextSwitching();
            this.ParseCommunicationLatency();
            this.ParseCommnunicationBandwidth();
            this.ParseFileVMLatency();
            this.ParseMemoryLatency();

            List<Metric> metrics = new List<Metric>();

            for (int index = 2; index < this.ProcessorTimes.Columns.Count; index++)
            {
                metrics.AddRange(this.ProcessorTimes.GetMetrics(valueIndex: index, name: this.ProcessorTimes.Columns[index].ColumnName, unit: "microseconds", namePrefix: "ProcessorTimes-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.BasicInt.Columns.Count; index++)
            {
                metrics.AddRange(this.BasicInt.GetMetrics(valueIndex: index, name: this.BasicInt.Columns[index].ColumnName, unit: "nanoseconds", namePrefix: "BasicInt-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.BasicFloat.Columns.Count; index++)
            {
                metrics.AddRange(this.BasicFloat.GetMetrics(valueIndex: index, name: this.BasicFloat.Columns[index].ColumnName, unit: "nanoseconds", namePrefix: "BasicFloat-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.BasicDouble.Columns.Count; index++)
            {
                metrics.AddRange(this.BasicDouble.GetMetrics(valueIndex: index, name: this.BasicDouble.Columns[index].ColumnName, unit: "nanoseconds", namePrefix: "BasicDouble-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.ContextSwitching.Columns.Count; index++)
            {
                metrics.AddRange(this.ContextSwitching.GetMetrics(valueIndex: index, name: this.ContextSwitching.Columns[index].ColumnName, unit: "microseconds", namePrefix: "ContextSwitching-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.CommunicationLatency.Columns.Count; index++)
            {
                metrics.AddRange(this.CommunicationLatency.GetMetrics(valueIndex: index, name: this.CommunicationLatency.Columns[index].ColumnName, unit: "microseconds", namePrefix: "CommunicationLatency-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.FileVmLatency.Columns.Count; index++)
            {
                metrics.AddRange(this.FileVmLatency.GetMetrics(valueIndex: index, name: this.FileVmLatency.Columns[index].ColumnName, unit: "microseconds", namePrefix: "FileVmLatency-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            for (int index = 2; index < this.CommunicationBandwidth.Columns.Count; index++)
            {
                metrics.AddRange(this.CommunicationBandwidth.GetMetrics(valueIndex: index, name: this.CommunicationBandwidth.Columns[index].ColumnName, unit: "MB/s", namePrefix: "CommunicationBandwidth-", metricRelativity: MetricRelativity.HigherIsBetter));
            }

            for (int index = 2; index < this.MemoryLatency.Columns.Count - 1; index++)
            {
                // The last column is "Guesses" which is not a metric.
                metrics.AddRange(this.MemoryLatency.GetMetrics(valueIndex: index, name: this.MemoryLatency.Columns[index].ColumnName, unit: "nanoseconds", namePrefix: "MemoryLatency-", metricRelativity: MetricRelativity.LowerIsBetter));
            }

            // The unit is not totally consistent in the output. Adjusting to correct units.
            metrics.Where(m => m.Name == "FileVmLatency-Prot Fault").FirstOrDefault().Unit = "Count";
            metrics.Where(m => m.Name == "FileVmLatency-Page Fault").FirstOrDefault().Unit = "Count";
            metrics.Where(m => m.Name == "MemoryLatency-Mhz").FirstOrDefault().Unit = "Mhz";
            if (metrics.Any(m => m.Name == "ProcessorTimes-Mhz"))
            {
                metrics.Where(m => m.Name == "ProcessorTimes-Mhz").First().Unit = "Mhz";
            }
            
            // The Mhz sometimes fail to estimate Mhz and return -1. Those need to be filtered out.
            metrics.Remove(metrics.Where(m => m.Name == "MemoryLatency-Mhz" && m.Value == -1).FirstOrDefault());

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Remove dashed lines
            Regex dashLineRegex = new Regex(@"(-){2,}(\s)*", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.RawText, dashLineRegex);

            // Add new line to avoid make output included in the tables.
            this.PreprocessedText = this.PreprocessedText.Replace("make", $"{Environment.NewLine}make");

            // Remove the extra label line to make parsing easier.
            Regex ctxswLine = new Regex(@"(ctxsw)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, ctxswLine);
            Regex createLine = new Regex(@"(Create)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, createLine);
            Regex rereadLine = new Regex(@"(reread)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, rereadLine);
            Regex warningLine = new Regex(@"(WARNING)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, warningLine);
            Regex hndlLine = new Regex(@"(hndl)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, hndlLine);
            Regex addLine = new Regex(@"(add)", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, addLine);
        }

        private void ParseProcessorTimes()
        {
            string tableName = "Processor, Processes - times in microseconds - smaller is better";
            
            if (this.Sections.ContainsKey(tableName))
            {
                IList<string> columnNames = new List<string>
                {
                    "Host",
                    "OS",
                    "Mhz",
                    "null call",
                    "null I/O",
                    "stat",
                    "open clos",
                    "slct TCP",
                    "sig inst",
                    "sig hndl",
                    "fork proc",
                    "exec proc",
                    "sh proc",
                };
                IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(0, 9),
                    new KeyValuePair<int, int>(10, 13),
                    new KeyValuePair<int, int>(24, 4),
                    new KeyValuePair<int, int>(29, 4),
                    new KeyValuePair<int, int>(34, 4),
                    new KeyValuePair<int, int>(39, 4),
                    new KeyValuePair<int, int>(44, 4),
                    new KeyValuePair<int, int>(49, 4),
                    new KeyValuePair<int, int>(54, 4),
                    new KeyValuePair<int, int>(59, 4),
                    new KeyValuePair<int, int>(64, 4),
                    new KeyValuePair<int, int>(69, 4),
                    new KeyValuePair<int, int>(74, 4)
                };

                this.ProcessorTimes = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

                // Remove the first row which is the duplicated column names.
                this.ProcessorTimes.Rows.RemoveAt(0);
                this.ProcessorTimes.TranslateUnits();
                this.ProcessorTimes.ReplaceEmptyCell();
            }
            else
            {
                this.ProcessorTimes = new DataTable();
            }
        }

        private void ParseBasicInt()
        {
            string tableName = "Basic integer operations - times in nanoseconds - smaller is better";
            if (this.Sections.ContainsKey(tableName))
            {
                IList<string> columnNames = new List<string>
                {
                    "Host",
                    "OS",
                    "intgr bit",
                    "intgr add",
                    "intgr mul",
                    "intgr div",
                    "intgr mod"
                };
                IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(0, 9),
                    new KeyValuePair<int, int>(10, 13),
                    new KeyValuePair<int, int>(24, 6),
                    new KeyValuePair<int, int>(31, 6),
                    new KeyValuePair<int, int>(38, 6),
                    new KeyValuePair<int, int>(45, 6),
                    new KeyValuePair<int, int>(52, 6)
                };

                this.BasicInt = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

                // Remove the first row which is the duplicated column names.
                this.BasicInt.Rows.RemoveAt(0);
                this.BasicInt.TranslateUnits();
                this.BasicInt.ReplaceEmptyCell();
            }
            else
            {
                this.BasicInt = new DataTable();
            }
        }

        private void ParseBasicFloat()
        {
            string tableName = "Basic float operations - times in nanoseconds - smaller is better";
            if (this.Sections.ContainsKey(tableName))
            {
                IList<string> columnNames = new List<string>
                {
                    "Host",
                    "OS",
                    "float add",
                    "float mul",
                    "float div",
                    "float bogo"
                };
                IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(0, 9),
                    new KeyValuePair<int, int>(10, 13),
                    new KeyValuePair<int, int>(24, 6),
                    new KeyValuePair<int, int>(31, 6),
                    new KeyValuePair<int, int>(38, 6),
                    new KeyValuePair<int, int>(45, 6)
                };

                this.BasicFloat = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

                // Remove the first row which is the duplicated column names.
                this.BasicFloat.Rows.RemoveAt(0);
                this.BasicFloat.TranslateUnits();
                this.BasicFloat.ReplaceEmptyCell();
            }
            else
            {
                this.BasicFloat = new DataTable();
            }
        }

        private void ParseBasicDouble()
        {
            string tableName = "Basic double operations - times in nanoseconds - smaller is better";
            if (this.Sections.ContainsKey(tableName))
            {
                IList<string> columnNames = new List<string>
                {
                    "Host",
                    "OS",
                    "double add",
                    "double mul",
                    "double div",
                    "double bogo"
                };
                IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
                {
                    new KeyValuePair<int, int>(0, 9),
                    new KeyValuePair<int, int>(10, 13),
                    new KeyValuePair<int, int>(24, 6),
                    new KeyValuePair<int, int>(31, 6),
                    new KeyValuePair<int, int>(38, 6),
                    new KeyValuePair<int, int>(45, 6)
                };

                this.BasicDouble = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

                // Remove the first row which is the duplicated column names.
                this.BasicDouble.Rows.RemoveAt(0);
                this.BasicDouble.TranslateUnits();
                this.BasicDouble.ReplaceEmptyCell();
            }
            else
            {
                this.BasicDouble = new DataTable();
            }
        }

        private void ParseContextSwitching()
        {
            string tableName = "Context switching - times in microseconds - smaller is better";
            IList<string> columnNames = new List<string> 
            { 
                "Host", 
                "OS", 
                "2p/0K ctxsw",
                "2p/16K ctxsw",
                "2p/64K ctxsw",
                "8p/16K ctxsw",
                "8p/64K ctxsw",
                "16p/16K ctxsw",
                "16p/64K ctxsw",
            };
            IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(0, 9),
                new KeyValuePair<int, int>(10, 13),
                new KeyValuePair<int, int>(24, 6),
                new KeyValuePair<int, int>(31, 6),
                new KeyValuePair<int, int>(38, 6),
                new KeyValuePair<int, int>(45, 6),
                new KeyValuePair<int, int>(52, 6),
                new KeyValuePair<int, int>(59, 7),
                new KeyValuePair<int, int>(67, 7)
            };

            this.ContextSwitching = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

            // Remove the first row which is the duplicated column names.
            this.ContextSwitching.Rows.RemoveAt(0);
            this.ContextSwitching.TranslateUnits();
            this.ContextSwitching.ReplaceEmptyCell();
        }

        private void ParseCommunicationLatency()
        {
            string tableName = "*Local* Communication latencies in microseconds - smaller is better";
            IList<string> columnNames = new List<string>
            {
                "Host",
                "OS",
                "2p/0K ctxsw",
                "Pipe",
                "AF UNIX",
                "UDP",
                "RPC/UDP",
                "TCP",
                "RPC/TCP",
                "TCP conn"
            };

            IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(0, 9),
                new KeyValuePair<int, int>(10, 13),
                new KeyValuePair<int, int>(24, 5),
                new KeyValuePair<int, int>(30, 5),
                new KeyValuePair<int, int>(36, 4),
                new KeyValuePair<int, int>(41, 5),
                new KeyValuePair<int, int>(47, 5),
                new KeyValuePair<int, int>(53, 5),
                new KeyValuePair<int, int>(59, 5),
                new KeyValuePair<int, int>(65, 4)
            };

            this.CommunicationLatency = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

            // Remove the first row which is the duplicated column names.
            this.CommunicationLatency.Rows.RemoveAt(0);
            this.CommunicationLatency.TranslateUnits();
            this.CommunicationLatency.ReplaceEmptyCell();
        }

        private void ParseFileVMLatency()
        {
            string tableName = "File & VM system latencies in microseconds - smaller is better";
            IList<string> columnNames = new List<string>
            {
                "Host",
                "OS",
                "0K File Create",
                "0K File Delete",
                "10K File Create",
                "10K File Delete",
                "Mmap Latency",
                "Prot Fault",
                "Page Fault",
                "100fd select"
            };

            IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(0, 9),
                new KeyValuePair<int, int>(10, 13),
                new KeyValuePair<int, int>(24, 6),
                new KeyValuePair<int, int>(31, 6),
                new KeyValuePair<int, int>(38, 6),
                new KeyValuePair<int, int>(45, 6),
                new KeyValuePair<int, int>(52, 7),
                new KeyValuePair<int, int>(60, 5),
                new KeyValuePair<int, int>(66, 7),
                new KeyValuePair<int, int>(74, 5)
            };

            this.FileVmLatency = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

            // Remove the first row which is the duplicated column names.
            this.FileVmLatency.Rows.RemoveAt(0);
            this.FileVmLatency.TranslateUnits();
            this.FileVmLatency.ReplaceEmptyCell();
        }

        private void ParseCommnunicationBandwidth()
        {
            string tableName = "*Local* Communication bandwidths in MB/s - bigger is better";
            IList<string> columnNames = new List<string>
            {
                "Host",
                "OS",
                "Pipe",
                "AF UNIX",
                "TCP",
                "File reread",
                "Mmap reread",
                "Bcopy (libc)",
                "Bcopy (hand)",
                "Mem reread",
                "Mem write"
            };

            IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(0, 9),
                new KeyValuePair<int, int>(10, 13),
                new KeyValuePair<int, int>(24, 4),
                new KeyValuePair<int, int>(29, 4),
                new KeyValuePair<int, int>(34, 4),
                new KeyValuePair<int, int>(39, 6),
                new KeyValuePair<int, int>(46, 6),
                new KeyValuePair<int, int>(53, 6),
                new KeyValuePair<int, int>(60, 6),
                new KeyValuePair<int, int>(67, 4),
                new KeyValuePair<int, int>(72, 5)
            };

            this.CommunicationBandwidth = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

            // Remove the first row which is the duplicated column names.
            this.CommunicationBandwidth.Rows.RemoveAt(0);
            this.CommunicationBandwidth.TranslateUnits();
            this.CommunicationBandwidth.ReplaceEmptyCell();
        }

        private void ParseMemoryLatency()
        {
            string tableName = "Memory latencies in nanoseconds - smaller is better";
            IList<string> columnNames = new List<string>
            {
                "Host",
                "OS",
                "Mhz",
                "L1",
                "L2",
                "Main mem",
                "Rand mem",
                "Guesses"
            };

            IList<KeyValuePair<int, int>> cellLocation = new List<KeyValuePair<int, int>>
            {
                new KeyValuePair<int, int>(0, 9),
                new KeyValuePair<int, int>(10, 13),
                new KeyValuePair<int, int>(24, 5),
                new KeyValuePair<int, int>(30, 6),
                new KeyValuePair<int, int>(37, 6),
                new KeyValuePair<int, int>(44, 11),
                new KeyValuePair<int, int>(56, 11),
                new KeyValuePair<int, int>(68, 11)
            };

            this.MemoryLatency = DataTableExtensions.ConvertToDataTable(this.Sections[tableName], cellLocation, columnNames, tableName);

            // Remove the first row which is the duplicated column names.
            this.MemoryLatency.Rows.RemoveAt(0);
            this.MemoryLatency.TranslateUnits();
            this.MemoryLatency.ReplaceEmptyCell();
        }
    }
}