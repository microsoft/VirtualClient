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
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for DCGMI modules output document.
    /// </summary>
    public class DCGMIModulesCommandParser : MetricsParser
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
        /// Constructor for <see cref="DCGMIModulesCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIModulesCommandParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// list of modules.
        /// </summary>
        public DataTable ModulesListResult { get; set; }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            double status;
            try
            {
                var statusMatches = Regex.Matches(this.PreprocessedText, GetStatus);
                var statusLine = Regex.Split(statusMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);
                if (statusLine[1].Trim() == "Success")
                {
                    status = 1;
                }
                else
                {
                    status = 0;
                }

                this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Status:\s*(.*?)\s*(\r\n|\n)", string.Empty);
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, SectionDelimiter);
                this.CalculateModulesList();
                int rows = this.ModulesListResult.Rows.Count;
                this.Metrics.AddRange(this.ModulesListResult.GetMetrics(nameIndex: 1, valueIndex: 2));
            }
            catch 
            {
                throw new SchemaException("The DCGMI Modules output file has incorrect format for parsing");
            }

            this.Metrics.Add(new Metric("Status", status));

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[=+]", "-");
            this.PreprocessedText = this.PreprocessedText.Replace("Loaded", "1");
            this.PreprocessedText = this.PreprocessedText.Replace("Not loaded", "0");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"--*\n", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"\|", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(\r\n|\n)", $"{Environment.NewLine}");            
        }

        private void CalculateModulesList()
        {
            string sectionName = "List Modules";
            this.ModulesListResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DCGMIModulesCommandParser.DataTableDelimiter, sectionName);
        }
    }
}
