// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for PostgreSQL result document.
    /// </summary>
    public class PostgreSQLMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex PostgreSQLSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex PostgreSQLDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="PostgreSQLMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public PostgreSQLMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Results for PostgreSQL.
        /// </summary>
        protected DataTable PostgreSQLResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, PostgreSQLSectionDelimiter);
                this.ThrowIfInvalidOutputFormat();
                this.CalculateThroughputResult();

                List<Metric> metrics = new List<Metric>(this.PostgreSQLResult.GetMetrics(nameIndex: 1, valueIndex: 0, metricRelativity: MetricRelativity.HigherIsBetter));

                metrics.Add(new Metric(
                    "Operations/sec",
                    metrics.First(m => m.Name == "Operations/min").Value / 60,
                    relativity: MetricRelativity.HigherIsBetter));

                metrics.Add(new Metric(
                    "Transactions/sec",
                    metrics.First(m => m.Name == "Transactions/min").Value / 60,
                    relativity: MetricRelativity.HigherIsBetter));

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse PostgreSQL metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, $"{Environment.NewLine}", " ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, $"(\n\r)|(\r\n)", " ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"TEST RESULT : System achieved ", $"{Environment.NewLine}{Environment.NewLine}TEST RESULT{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"NOPM from ", $" Operations/min{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"PostgreSQL TPM", $" Transactions/min{Environment.NewLine}{Environment.NewLine}");
        }

        private void CalculateThroughputResult()
        {
            string sectionName = "TEST RESULT";
            IList<string> columnNames = new List<string> { "Value", "Name" };
            this.PostgreSQLResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], PostgreSQLDataTableDelimiter, sectionName, columnNames);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("TEST RESULT"))
            {
                throw new SchemaException("The PostgreSQL output file has incorrect format for parsing");
            }
        }
    }
}