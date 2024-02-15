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
    /// Parser for specCPU output document
    /// </summary>
    public class SpecCpuMetricsParser : MetricsParser
    {
        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex SpecCpuDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="SpecCpuMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SpecCpuMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Spec CPU individual metrics table.
        /// </summary>
        public DataTable SpecCpu { get; set; }

        /// <summary>
        /// Spec CPU summary table. Should only contain 2 lines.
        /// </summary>
        public DataTable SpecCpuSummary { get; set; }

        /// <summary>
        /// True if the results have been parsed.
        /// </summary>
        protected bool IsParsed
        {
            get
            {
                return this.SpecCpu != null && this.SpecCpuSummary != null;
            }
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.ParseSpecCpuResult();
                this.ParseSpecCpuSummaryResult();

                List<Metric> metrics = new List<Metric>();

                metrics.AddRange(this.SpecCpu.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "Score", namePrefix: "SPECcpu-base-", metricRelativity: MetricRelativity.HigherIsBetter));
                metrics.AddRange(this.SpecCpu.GetMetrics(nameIndex: 0, valueIndex: 7, unit: "Score", namePrefix: "SPECcpu-peak-", ignoreFormatError: true, metricRelativity: MetricRelativity.HigherIsBetter));
                metrics.AddRange(this.SpecCpuSummary.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: string.Empty, ignoreFormatError: true, metricRelativity: MetricRelativity.HigherIsBetter));

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse SPECcpu metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            /*
             * Only capture data in selected test section, which is
                =================================================================================
                500.perlbench_r     256        757        538  *     256        694        587  *
                502.gcc_r           256        634        572  *     256        511        710  *
                505.mcf_r           256        473        874  *     256        421        982  *
                520.omnetpp_r       256        931        361  *     256        927        362  *
                523.xalancbmk_r     256        300        900  *     256        276        981  *
                525.x264_r          256        249       1800  *     256        249       1800  *
                531.deepsjeng_r     256        394        744  *     256        392        748  *
                541.leela_r         256        572        741  *     256        567        748  *
                548.exchange2_r     256        383       1750  *     256        383       1750  *
                557.xz_r            256        571        484  *     256        569        486  *
                    SPECrate(R)2017_int_base                 777
                    SPECrate(R)2017_int_peak                                                  812
             */

            // Getting the text after the "===========" line.
            Regex equalSignLine = new Regex(@$"(=){{2,}}({Environment.NewLine})", RegexOptions.ExplicitCapture);
            this.PreprocessedText = Regex.Split(this.RawText, equalSignLine.ToString(), equalSignLine.Options).Skip(1).First();

            // Get the text before the double line return.
            Regex doubleLineReturn = new Regex(@$"({Environment.NewLine}){{2,}}", RegexOptions.ExplicitCapture);
            this.PreprocessedText = Regex.Split(this.PreprocessedText, doubleLineReturn.ToString(), doubleLineReturn.Options).First();

            // Split the section between individual metrics and the summary
            this.Sections.Add(nameof(this.SpecCpu), this.PreprocessedText.Substring(0, this.PreprocessedText.IndexOf("SPEC", StringComparison.Ordinal)));
            this.Sections.Add(nameof(this.SpecCpuSummary), this.PreprocessedText.Substring(this.PreprocessedText.IndexOf("SPEC", StringComparison.Ordinal)));
        }

        private void ParseSpecCpuResult()
        {
            IList<string> columnNames = new List<string>
            {
                "BenchMarks",
                "BaseThreads",
                "BaseRunTime",
                "BaseScore",
                "BaseSelected",
                "PeakThreads",
                "PeakRunTime",
                "PeakScore",
                "PeakSelected"
            };
            this.SpecCpu = DataTableExtensions.ConvertToDataTable(
                this.Sections[nameof(this.SpecCpu)], SpecCpuMetricsParser.SpecCpuDataTableDelimiter, nameof(this.SpecCpu), columnNames);
        }

        private void ParseSpecCpuSummaryResult()
        {
            IList<string> columnNames = new List<string>
            {
                "BenchMarks",
                "Score"
            };
            this.SpecCpuSummary = DataTableExtensions.ConvertToDataTable(
                this.Sections[nameof(this.SpecCpuSummary)], SpecCpuMetricsParser.SpecCpuDataTableDelimiter, nameof(this.SpecCpuSummary), columnNames);
        }
    }
}