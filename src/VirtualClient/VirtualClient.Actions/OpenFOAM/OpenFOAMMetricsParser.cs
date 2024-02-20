// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for OpenFOAM output document.
    /// </summary>
    public class OpenFOAMMetricsParser : MetricsParser
    {
        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Identifies string not starting with word 'ExecutionTime'.
        /// </summary>
        private static readonly Regex ExecutionTimeLinesRegex = new Regex(@"^((?!ExecutionTime).)*$", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex OpenFOAMSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex OpenFOAMDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="OpenFOAMMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public OpenFOAMMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Execution Times for OpenFOAM Simulations.
        /// </summary>
        public DataTable ExecutionTimes { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, OpenFOAMSectionDelimiter);
                this.ThrowIfInvalidOutputFormat();
                this.CreateExecutionTimesDataTable();

                List<Metric> metrics = new List<Metric>();
                double numberOfIterationsPerMinute = this.CalculateIterationsPerMinute();

                metrics.Add(new Metric("Iterations/min", numberOfIterationsPerMinute, "itrs/min", MetricRelativity.HigherIsBetter));
                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse OpenFOAM metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.RawText, OpenFOAMMetricsParser.ExecutionTimeLinesRegex);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n\n", "\n");
            this.PreprocessedText = $"ExecutionTime{Environment.NewLine}" + this.PreprocessedText;
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"ExecutionTime =", $" executionTime  ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "  ClockTime = [0-9]* .*", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", $"{Environment.NewLine}");
        }

        private void CreateExecutionTimesDataTable()
        {
            string sectionName = "ExecutionTime";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.ExecutionTimes = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], OpenFOAMMetricsParser.OpenFOAMDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.ExecutionTimes.SplitDataColumn(columnIndex: 1, OpenFOAMMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private double CalculateIterationsPerMinute()
        {
            int totalIterations = this.ExecutionTimes.Rows.Count;
            double totalTime = Convert.ToDouble(this.ExecutionTimes.Rows[totalIterations - 1].ItemArray[2]);
            return (totalIterations * 60) / totalTime;
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("ExecutionTime") || (this.Sections["ExecutionTime"] == string.Empty))
            {
                throw new SchemaException("The OpenFOAM results file has incorrect format for parsing");
            }
        }
    }
}