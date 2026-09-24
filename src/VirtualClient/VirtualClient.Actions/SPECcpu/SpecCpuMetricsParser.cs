// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using MathNet.Numerics;
    using DataTableExtensions = global::VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for SPEC CPU output documents.
    /// </summary>
    public class SpecCpuMetricsParser : MetricsParser
    {
        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex SpecCpuDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);
        private static readonly Regex SummaryMetricRegex = new Regex(
            @"^(?:Est\.\s+)?SPEC(?<suite>Rate|speed)(?:\(R\))?(?<version>\d{4})_(?<type>fp|int)_(?<tuning>base|peak)$",
            RegexOptions.IgnoreCase);

        private bool isCsv;

        /// <summary>
        /// Constructor for <see cref="SpecCpuMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="csv">The content is from the SPEC CSV results file.</param>
        public SpecCpuMetricsParser(string rawText, bool csv = false)
            : base(rawText)
        {
            this.isCsv = csv;
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
                IEnumerable<Metric> metrics = null;
                if (this.isCsv)
                {
                    metrics = this.ParseMetricsFromCsv();
                }
                else
                {
                    metrics = this.ParseMetricsFromStandardOutput();
                }

                this.SetWorkloadMetadata(metrics);

                return metrics?.OrderBy(m => m.Name).ToList();
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse SPEC CPU benchmark metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            /*
                Only capture data in selected test section, which is
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

            // Split the individual metrics from summary lines such as:
            // SPECrate(R)2017_int_base or Est. SPECrate(R)2026_int_base.
            Match summarySection = Regex.Match(
                this.PreprocessedText,
                @"(?m)^\s*(?:Est\.\s+)?SPEC(?:Rate|speed)",
                RegexOptions.IgnoreCase);
            if (!summarySection.Success)
            {
                throw new SchemaException("Invalid results. SPEC CPU summary metrics cannot be found in the results provided.");
            }

            this.Sections.Add(nameof(this.SpecCpu), this.PreprocessedText.Substring(0, summarySection.Index));
            this.Sections.Add(nameof(this.SpecCpuSummary), this.PreprocessedText.Substring(summarySection.Index));
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

        private IEnumerable<Metric> ParseMetricsFromCsv()
        {
            List<Metric> metrics = new List<Metric>();

            /*
                Only capture data in selected test section, which is
                =================================================================================
                "Selected Results Table"

                Benchmark,"Base # Copies","Est. Base Run Time","Est. Base Rate","Base Selected","Base Status","Peak # Copies","Est. Peak Run Time","Est. Peak Rate","Peak Selected","Peak Status",Description
                503.bwaves_r,8,774.54528,103.575608,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"
                507.cactuBSSN_r,8,529.532167,19.12632,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                508.namd_r,8,355.876987,21.355696,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"
                510.parest_r,8,855.489505,24.463184,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                511.povray_r,8,777.63715,24.021488,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                519.lbm_r,8,613.604624,13.741744,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"
                521.wrf_r,8,575.203376,31.1542,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                526.blender_r,8,543.972241,22.3982,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                527.cam4_r,8,444.302751,31.49204,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                538.imagick_r,8,731.398457,27.20268,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                544.nab_r,8,488.412998,27.566832,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"
                549.fotonik3d_r,8,940.704803,33.141112,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
                554.roms_r,8,609.94932,20.841072,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"

                SPECrate2017_fp_base,26.914269,,26.914269
                SPECrate2017_fp_peak,"Not Run",,,,,,,"Not Run"
            */

            Match resultsSection = Regex.Match(
                this.RawText,
                "\"Selected Results Table\"[\\s\\S]+?SPEC(?:rate|speed)\\d{4}_(?:fp|int)_peak.*",
                RegexOptions.IgnoreCase);
            if (!resultsSection.Success)
            {
                throw new SchemaException($"Invalid results. SPEC CPU benchmark outcomes/information cannot be found in the results provided.");
            }

            IEnumerable<string> results = Regex.Split(resultsSection.Groups[0].Value, "[\r\n]").Where(l => !string.IsNullOrWhiteSpace(l));
            foreach (string line in results)
            {
                string[] fields = line.Split(',', StringSplitOptions.TrimEntries);
                if (fields.Length > 2)
                {
                    if (fields[0].StartsWith("Benchmark"))
                    {
                        continue;
                    }

                    if (this.TryParseBaseMetric(fields, out Metric baseMetric))
                    {
                        metrics.Add(baseMetric);
                    }

                    if (this.TryParsePeakMetric(fields, out Metric peakMetric))
                    {
                        metrics.Add(peakMetric);
                    }

                    if (this.TryParseSummaryMetric(fields, out Metric summaryMetric))
                    {
                        metrics.Add(summaryMetric);
                    }
                }
            }

            return metrics;
        }

        private IEnumerable<Metric> ParseMetricsFromStandardOutput()
        {
            List<Metric> metrics = new List<Metric>();

            this.Preprocess();
            this.ParseSpecCpuResult();
            this.ParseSpecCpuSummaryResult();

            metrics.AddRange(this.SpecCpu.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "score", namePrefix: "SPECcpu-base-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.SpecCpu.GetMetrics(nameIndex: 0, valueIndex: 7, unit: "score", namePrefix: "SPECcpu-peak-", ignoreFormatError: true, metricRelativity: MetricRelativity.HigherIsBetter));
            foreach (DataRow row in this.SpecCpuSummary.Rows)
            {
                string[] fields = row.ItemArray.Select(field => field?.ToString()).ToArray();
                if (this.TryParseSummaryMetric(fields, out Metric summaryMetric))
                {
                    metrics.Add(summaryMetric);
                }
            }

            // Every score in SPECcpu is critical metric.
            metrics.ForEach(m => m.Verbosity = 0);

            return metrics;
        }

        private void SetWorkloadMetadata(IEnumerable<Metric> metrics)
        {
            string workloadTemplate = null;
            IEnumerable<Metric> summaryMetrics = metrics.Where(m => Regex.IsMatch(m.Name, "SPECrate|SPECspeed"));
            foreach (Metric metric in summaryMetrics)
            {
                if (SpecCpuMetricsParser.TryGetSummaryMetric(metric.Name, out SummaryMetric summaryMetric))
                {
                    string workload = summaryMetric.Workload;
                    metric.Metadata["workload"] = workload;

                    if (workloadTemplate == null)
                    {
                        // e.g.
                        // floating_point_base_rate -> floating_point_{0}_rate
                        // floating_point_peak_rate -> floating_point_{0}_rate
                        workloadTemplate = Regex.Replace(summaryMetric.Workload, "base|peak", "{0}", RegexOptions.IgnoreCase);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(workloadTemplate))
            {
                StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
                IEnumerable<Metric> benchmarkMetrics = metrics.Except(summaryMetrics);
                foreach (Metric metric in benchmarkMetrics)
                {
                    if (metric.Name.Contains("base", ignoreCase))
                    {
                        // e.g.
                        // floating_point_base_rate
                        // floating_Point_base_speed
                        metric.Metadata["workload"] = string.Format(workloadTemplate, "base");
                    }
                    else if (metric.Name.Contains("peak", ignoreCase))
                    {
                        // e.g.
                        // integer_peak_rate
                        // integer_peak_speed
                        metric.Metadata["workload"] = string.Format(workloadTemplate, "peak");
                    }
                }
            }
        }

        private bool TryParseBaseMetric(string[] fields, out Metric metric)
        {
            /*
                "Selected Results Table"

                Benchmark,"Base # Copies","Est. Base Run Time","Est. Base Rate","Base Selected","Base Status","Peak # Copies","Est. Peak Run Time","Est. Peak Rate","Peak Selected","Peak Status",Description
                503.bwaves_r,8,774.54528,103.575608,1,S,,,,,NR,"SelectedIteration (base #2; peak NR)"
                507.cactuBSSN_r,8,529.532167,19.12632,1,S,,,,,NR,"SelectedIteration (base #1; peak NR)"
            */

            metric = null;

            // Benchmark
            string benchmark = fields[0].Trim();

            if (!Regex.IsMatch(benchmark, "^SPECrate|SPECspeed", RegexOptions.IgnoreCase))
            {
                // Est. Base Rate
                if (double.TryParse(fields[3], out double baseScore))
                {
                    // Base # Copies
                    double.TryParse(fields[1], out double baseCopies);

                    // Est. Base Run Time
                    double.TryParse(fields[2], out double baseRunTime);

                    metric = new Metric(
                        $"SPECcpu-base-{benchmark}",
                        baseScore,
                        unit: "score",
                        relativity: MetricRelativity.HigherIsBetter,
                        description: $"SPEC CPU '{benchmark}' benchmark base score.",
                        metadata: new Dictionary<string, IConvertible>
                        {
                            { "benchmark", benchmark },
                            { "numCopies", baseCopies },
                            { "runTime", baseRunTime }
                        });

                    metric.Verbosity = 0;
                }
            }

            return metric != null;
        }

        private bool TryParsePeakMetric(string[] fields, out Metric metric)
        {
            /*
                "Selected Results Table"

                Benchmark,"Base # Copies","Base Run Time","Base Rate","Base Selected","Base Status","Peak # Copies","Peak Run Time","Peak Rate","Peak Selected","Peak Status",Description
                503.bwaves_r,128,1649.037039,778.384,1,S,128,1649.037039,778.384,1,S,"SelectedIteration (base #1; peak #1)"
                507.cactuBSSN_r,128,221.93356,730.16448,1,S,128,220.578603,734.649728,1,S,"SelectedIteration (base #3; peak #3)"
            */

            metric = null;

            // Benchmark
            string benchmark = fields[0].Trim();

            if (!Regex.IsMatch(benchmark, "^SPECrate|SPECspeed", RegexOptions.IgnoreCase))
            {
                // Peak Rate
                if (double.TryParse(fields[8], out double peakScore))
                {
                    // Peak # Copies
                    double.TryParse(fields[6], out double peakCopies);

                    // Peak Run Time
                    double.TryParse(fields[7], out double peakRunTime);

                    metric = new Metric(
                        $"SPECcpu-peak-{benchmark}",
                        peakScore,
                        unit: "score",
                        relativity: MetricRelativity.HigherIsBetter,
                        description: $"SPEC CPU '{benchmark}' benchmark peak score.",
                        metadata: new Dictionary<string, IConvertible>
                        {
                            { "benchmark", benchmark },
                            { "numCopies", peakCopies },
                            { "runTime", peakRunTime }
                        });

                    metric.Verbosity = 0;
                }
            }

            return metric != null;
        }

        private bool TryParseSummaryMetric(string[] fields, out Metric metric)
        {
            /*
                "Selected Results Table"

                Benchmark,"Base # Copies","Base Run Time","Base Rate","Base Selected","Base Status","Peak # Copies","Peak Run Time","Peak Rate","Peak Selected","Peak Status",Description
                ...
                ...
                SPECspeed2017_int_base,12.279658,,12.279658
                SPECspeed2017_int_peak,12.293316,,,,,,,12.293316
            */

            metric = null;

            // Benchmark
            string benchmark = fields[0].Trim();

            if (SpecCpuMetricsParser.TryGetSummaryMetric(benchmark, out SummaryMetric description)
                && fields.Length > 1
                && double.TryParse(fields[1], out double score))
            {
                metric = new Metric(
                    description.MetricName,
                    score,
                    unit: "score",
                    relativity: MetricRelativity.HigherIsBetter,
                    description: description.MetricDescription);

                metric.Verbosity = 0;
            }

            return metric != null;
        }

        private static bool TryGetSummaryMetric(string value, out SummaryMetric summaryMetric)
        {
            summaryMetric = null;
            Match match = SpecCpuMetricsParser.SummaryMetricRegex.Match(value?.Trim() ?? string.Empty);
            if (match.Success)
            {
                string suite = match.Groups["suite"].Value.Equals("Rate", StringComparison.OrdinalIgnoreCase)
                    ? "rate"
                    : "speed";
                string version = match.Groups["version"].Value;
                string type = match.Groups["type"].Value.ToLowerInvariant();
                string tuning = match.Groups["tuning"].Value.ToLowerInvariant();
                string workloadType = type == "fp" ? "floating_point" : "integer";

                summaryMetric = new SummaryMetric
                {
                    MetricName = $"SPEC{suite}(R){version}_{type}_{tuning}",
                    MetricDescription = $"SPEC CPU {workloadType.Replace('_', ' ')} {tuning} {suite} summary score.",
                    Workload = $"{workloadType}_{tuning}_{suite}"
                };
            }

            return summaryMetric != null;
        }

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Just a POCO class.")]
        private class SummaryMetric
        {
            public string MetricName;
            public string MetricDescription;
            public string Workload;
        }
    }
}