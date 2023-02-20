// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using VirtualClient.Contracts;
    using YamlDotNet.Core.Tokens;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for DCGMI dmon output document.
    /// </summary>
    public class DCGMIDmonCommandParser : MetricsParser
    {
        /// <summary>
        /// To match status line of the result.
        /// </summary>
        private const string GetStatus = @"\s*Status:\s*(.*?)\s*(\r\n|\n)";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex SectionDelimiter = new Regex(@"==*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex DataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Constructor for <see cref="DCGMIDmonCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIDmonCommandParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// list of modules.
        /// </summary>
        public DataTable DmonResult { get; set; }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            try
            {
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, SectionDelimiter);
                this.ThrowIfInvalidOutputFormat();
                this.CalculateResults();

                foreach (DataRow row in this.DmonResult.Rows)
                {
                    int columnIndex = 0;
                    foreach (DataColumn column in this.DmonResult.Columns)
                    {
                        string metricName = $"{row[0]}_{column.ColumnName}";
                        double metricValue;
                        string value = row[columnIndex].ToString().Trim();
                        if (double.TryParse(value, out metricValue))
                        {
                            this.Metrics.Add(new Metric(metricName, metricValue));
                        }

                        columnIndex++;
                    }
                }
            }
            catch
            {
                throw new SchemaException("The DCGMI dmon output file has incorrect format for parsing");
            }

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = $"CUDA Generator metrics{Environment.NewLine}" + this.RawText;
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @$"ID\s*", string.Empty);
            this.PreprocessedText = this.PreprocessedText.Replace("#Entity", "Entity_ID");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(\r\n|\n)", $"{Environment.NewLine}");            // this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, new Regex(@"--*", RegexOptions.ExplicitCapture));
        }

        private void CalculateResults()
        {
            string sectionName = "CUDA Generator metrics";
            this.DmonResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DCGMIDmonCommandParser.DataTableDelimiter, sectionName);
        }

        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count < 1 || !this.Sections.ContainsKey("CUDA Generator metrics"))
            {
                throw new SchemaException("The DCGMI dmon output file has incorrect format for parsing");
            }
        }
    }
}
