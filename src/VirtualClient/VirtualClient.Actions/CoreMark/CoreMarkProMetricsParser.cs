// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for CoreMarkPro output document
    /// </summary>
    public class CoreMarkProMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex CoreMarkProSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces, so that "N-Body Physics" will not be separated into two cells.
        /// </summary>
        private static readonly Regex CoreMarkProDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="CoreMarkProMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public CoreMarkProMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Single core result.
        /// </summary>
        public DataTable WorkloadResult { get; set; }

        /// <summary>
        /// Multi core result.
        /// </summary>
        public DataTable MarkResult { get; set; }

        /// <summary>
        /// True if the results have been parsed.
        /// </summary>
        protected bool IsParsed
        {
            get
            {
                return this.MarkResult != null && this.WorkloadResult != null;
            }
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, CoreMarkProMetricsParser.CoreMarkProSectionDelimiter);
            this.ParseWorkloadResult();
            this.ParseMarkResult();

            List<Metric> metrics = new List<Metric>();
            
            metrics.AddRange(this.WorkloadResult.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "iterations/sec", namePrefix: "MultiCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WorkloadResult.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "iterations/sec", namePrefix: "SingleCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.WorkloadResult.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "scale", namePrefix: "Scaling-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MarkResult.GetMetrics(nameIndex: 0, valueIndex: 1, unit: "Score", namePrefix: "MultiCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MarkResult.GetMetrics(nameIndex: 0, valueIndex: 2, unit: "Score", namePrefix: "SingleCore-", metricRelativity: MetricRelativity.HigherIsBetter));
            metrics.AddRange(this.MarkResult.GetMetrics(nameIndex: 0, valueIndex: 3, unit: "scale", namePrefix: "Scaling-", metricRelativity: MetricRelativity.HigherIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            /* Change this:
             * 
             * WORKLOAD RESULTS TABLE
             * 
             *                                                  MultiCore SingleCore           
             * Workload Name                                     (iter/s)   (iter/s)    Scaling
             * 
             * into this:
             * 
             * WORKLOAD RESULTS TABLE
             * Workload Name                                     (MultiCore)   (SingleCore)    Scaling
             * 
             */
            Regex replaceHeader = new Regex(@"(WORKLOAD RESULTS TABLE)(\s)+(MultiCore)(\s)+(SingleCore)");
            this.PreprocessedText = Regex.Replace(this.RawText, replaceHeader.ToString(), $"WORKLOAD RESULTS TABLE");
            // DataTable class doesn't allow duplicate column name, so the two iter/s need to be replaced.
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(\(iter\/s\))(\s)+(\(iter\/s\))", $"MultiCore   SingleCore");

            /* Change this:
             * 
             * MARK RESULTS TABLE
             * 
             * Mark Name                                        MultiCore SingleCore    Scaling
             * 
             * into this: (Noticed the extra space between MultiCore and SingleCore)
             * 
             * MARK RESULTS TABLE
             * Mark Name                                        MultiCore  SingleCore    Scaling
             * 
             */
            Regex removeLineBetweenMarkResultsAndMarkName = new Regex(@"(MARK RESULTS TABLE)(\s)+(Mark Name)");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, removeLineBetweenMarkResultsAndMarkName.ToString(), $"MARK RESULTS TABLE{Environment.NewLine}Mark Name");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "MultiCore SingleCore", $"MultiCore  SingleCore");

            // Also remove dashlines.
            Regex dashLine = new Regex(@"(-){2,}", RegexOptions.ExplicitCapture);
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, dashLine);
        }

        private void ParseWorkloadResult()
        {
            string sectionName = "WORKLOAD RESULTS TABLE";
            this.WorkloadResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], CoreMarkProMetricsParser.CoreMarkProDataTableDelimiter, sectionName);
        }

        private void ParseMarkResult()
        {
            string sectionName = "MARK RESULTS TABLE";
            this.MarkResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], CoreMarkProMetricsParser.CoreMarkProDataTableDelimiter, sectionName);
        }
    }
}
