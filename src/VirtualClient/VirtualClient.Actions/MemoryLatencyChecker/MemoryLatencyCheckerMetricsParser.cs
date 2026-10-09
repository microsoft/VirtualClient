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
    /// Parser for Memory Latency Checker Workload.
    /// </summary>
    public class MemoryLatencyCheckerMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex MLCSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="MemoryLatencyCheckerMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="benchmark">Benchmark ran.</param>
        public MemoryLatencyCheckerMetricsParser(string rawText, string benchmark)
            : base(rawText)
        {
            this.Benchmark = benchmark;
        }

        /// <summary>
        /// The MLC Benchmark run.
        /// </summary>
        protected string Benchmark { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, MemoryLatencyCheckerMetricsParser.MLCSectionDelimiter);

            List<Metric> metrics = new List<Metric>();
            DataTable result = new DataTable();

            switch (this.Benchmark)
            {
                case MemoryLatencyCheckerBenchmark.IdleLatency:

                    MatchCollection matches = Regex.Matches(this.Sections["IDLE LATENCY RESULTS"], $"{TextParsingExtensions.DoubleTypeRegex}", RegexOptions.ExplicitCapture);

                    if (matches.Count > 0)
                    {
                        metrics.Add(new Metric($"base_frequency_clock_iterations", Convert.ToDouble(matches[0].Value), string.Empty, MetricRelativity.LowerIsBetter));
                        metrics.Add(new Metric($"time_elapsed_per_iteration", Convert.ToDouble(matches[1].Value), "nanoseconds", MetricRelativity.LowerIsBetter));
                    }

                    break;

                case MemoryLatencyCheckerBenchmark.PeakInjectionBandwidth:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["PEAK INJECTION MEMORY BANDWIDTH RESULTS"], new Regex(@"\s*\:\s+", RegexOptions.ExplicitCapture), "PEAK INJECTION MEMORY BANDWIDTH RESULTS");

                    metrics.AddRange(result.GetMetrics(
                        nameIndex: 0,
                        valueIndex: 1,
                        unit: "MBps",
                        namePrefix: "peak_injection_bandwidth_at_",
                        metricRelativity: MetricRelativity.HigherIsBetter));

                    break;

                case MemoryLatencyCheckerBenchmark.MaxBandwidth:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["MAXIMUM MEMORY BANDWIDTH RESULTS"], new Regex(@"\s*\:\s+", RegexOptions.ExplicitCapture), "PEAK INJECTION MEMORY BANDWIDTH RESULTS");

                    metrics.AddRange(result.GetMetrics(
                        nameIndex: 0,
                        valueIndex: 1,
                        unit: "MBps",
                        namePrefix: "maximum_bandwidth_at_",
                        metricRelativity: MetricRelativity.HigherIsBetter));

                    break;

                case MemoryLatencyCheckerBenchmark.BandwidthMatrix:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["MEMORY BANDWIDTH RESULTS"], new Regex(@"(\s){1,}", RegexOptions.ExplicitCapture), "MEMORY BANDWIDTH RESULTS");

                    for (int colNum = 1; colNum < result.Columns.Count; colNum++)
                    {
                        metrics.AddRange(result.GetMetrics(
                            nameIndex: 0,
                            valueIndex: colNum,
                            unit: "MBps",
                            namePrefix: "memory_bandwidth_nodes_" + (colNum - 1).ToString() + "_",
                            metricRelativity: MetricRelativity.HigherIsBetter));
                    }

                    break;

                case MemoryLatencyCheckerBenchmark.LoadedLatency:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["LOADED LATENCY RESULTS"], new Regex(@"(\s){1,}", RegexOptions.ExplicitCapture), "LOADED LATENCY RESULTS");

                    metrics.AddRange(result.GetMetrics(
                        nameIndex: 0,
                        valueIndex: 1,
                        unit: "nanoseconds",
                        namePrefix: "loaded_latency_at_delay_",
                        metricRelativity: MetricRelativity.LowerIsBetter));

                    metrics.AddRange(result.GetMetrics(
                        nameIndex: 0,
                        valueIndex: 2,
                        unit: "MBps",
                        namePrefix: "load_bandwidth_at_delay_",
                        metricRelativity: MetricRelativity.Undefined));
                    break;

                case MemoryLatencyCheckerBenchmark.LatencyMatrix:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["LATENCY MATRIX RESULTS"], new Regex(@"(\s){1,}", RegexOptions.ExplicitCapture), "LATENCY MATRIX RESULTS");

                    for (int colNum = 1; colNum < result.Columns.Count; colNum++)
                    {
                        metrics.AddRange(result.GetMetrics(
                            nameIndex: 0,
                            valueIndex: colNum,
                            unit: "nanoseconds",
                            namePrefix: "latency-matrix_nodes_" + (colNum - 1).ToString() + "_",
                            metricRelativity: MetricRelativity.LowerIsBetter));
                    }

                    break;

                case MemoryLatencyCheckerBenchmark.C2CLatency:

                    result = DataTableExtensions.ConvertToDataTable(this.Sections["C2C LATENCY RESULTS"], new Regex(@"\s+(?=\d)", RegexOptions.ExplicitCapture), "C2C LATENCY RESULTS");

                    metrics.AddRange(result.GetMetrics(
                        nameIndex: 0,
                        valueIndex: 1,
                        unit: "nanoseconds",
                        metricRelativity: MetricRelativity.LowerIsBetter));

                    break;

                default:
                    break;
            }

            if (metrics.Count == 0)
            {
                throw new WorkloadResultsException($"The Workload did not generate any valid metrics! ");
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Converting all CRLF(Windows EOL) to LF(Unix EOL).
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");

            // Converting all LF to CRLF. (File contained a mixture of the end of lines)
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", "\r\n");

            // Removing the Initial Unneccesary Lines
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Intel\(R\) Memory Latency Checker(.|\r\n)*?(?=Measuring)",
                string.Empty);

            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Each iteration",
                $"{Environment.NewLine}IDLE LATENCY RESULTS{Environment.NewLine}");

            // Creating section for the IDLE LATENCY RESULTS table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring idle latencies(.|\r\n)*?\nNuma node",
                $"{Environment.NewLine}LATENCY MATRIX RESULTS{Environment.NewLine}Node");

            // Creating section for the PEAK INJECTION MEMORY BANDWIDTH RESULTS table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring Peak Injection Memory Bandwidths(.|\r\n)*?\r\n(?=(\w| )+Reads)",
                $"{Environment.NewLine}PEAK INJECTION MEMORY BANDWIDTH RESULTS{Environment.NewLine}Type : Value{Environment.NewLine}");

            // Creating section for the MAXIMUM MEMORY BANDWIDTH RESULTS table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring Maximum Memory Bandwidths(.|\r\n)*?\r\n(?=(\w| )+Reads)",
                $"{Environment.NewLine}MAXIMUM MEMORY BANDWIDTH RESULTS{Environment.NewLine}Type : Value{Environment.NewLine}");

            // Creating section for the MEMORY BANDWIDTH RESULTS table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring Memory Bandwidths(.|\r\n)*?\r\nNuma node",
                $"{Environment.NewLine}MEMORY BANDWIDTH RESULTS{Environment.NewLine}Node");

            // Creating section for the LOADED LATENCY RESULTS table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring Loaded Latencies(.|\r\n)*?\=(?=\s+\d+\s+)",
                $"{Environment.NewLine}LOADED LATENCY RESULTS{Environment.NewLine}Delay Latency Bandwidth");

            // Creating section for the C2C Transfer Latency table with the required name and removing unnecessary lines in between.
            this.PreprocessedText = Regex.Replace(
                this.PreprocessedText,
                @"Measuring cache-to-cache transfer latency(.|\r\n)*?(?=\r\nLocal Socket)",
                $"{Environment.NewLine}C2C LATENCY RESULTS{Environment.NewLine}Type  0");

            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.PreprocessedText.Trim();
        }

        /// <summary>
        /// Defines the MLC benchmark scenario.
        /// </summary>
        internal class MemoryLatencyCheckerBenchmark
        {
            public const string LatencyMatrix = "latency_matrix";
            public const string BandwidthMatrix = "bandwidth_matrix";
            public const string PeakInjectionBandwidth = "peak_injection_bandwidth";
            public const string MaxBandwidth = "max_bandwidth";
            public const string LoadedLatency = "loaded_latency";
            public const string IdleLatency = "idle_latency";
            public const string C2CLatency = "c2c_latency";
        }
    }
}