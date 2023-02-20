// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for DCGMIFieldGroup Command output document.
    /// </summary>
    public class DCGMIFieldGroupCommandParser : MetricsParser
    {
        /// <summary>
        /// To match ID line of the result.
        /// </summary>
        private const string GetIDLines = @"\|\s*ID\s*\|\s*(\d+)\s*\|";

        /// <summary>
        /// To match Name line of the result.
        /// </summary>
        private const string GetNameLines = @"\|\s*Name\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// To match FieldIDs line of the result.
        /// </summary>
        private const string GetFieldIDsLines = @"\|\s*Field IDs\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// Split string at '|' char.
        /// </summary>
        private const string Delimiter = @"\|";

        /// <summary>
        /// Constructor for <see cref="DCGMIFieldGroupCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIFieldGroupCommandParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            string id = string.Empty;
            string name = string.Empty;
            string fieldIDs = string.Empty;
            try
            {
                double metricValue = double.Parse(this.PreprocessedText.Split()[0]);
                var idMatches = Regex.Matches(this.RawText, GetIDLines);
                var nameMatches = Regex.Matches(this.RawText, GetNameLines);
                var fieldIDsMatches = Regex.Matches(this.RawText, GetFieldIDsLines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                for (int i = 0; i < idMatches.Count; i++)
                {
                    var idLine = Regex.Split(idMatches.ElementAt(i).Value.Trim(), Delimiter);
                    var nameLine = Regex.Split(nameMatches.ElementAt(i).Value.Trim(), Delimiter);
                    var fieldIDline = Regex.Split(fieldIDsMatches.ElementAt(i).Value.Trim(), Delimiter);

                    id = idLine[2].Trim();
                    name = nameLine[2].Trim();
                    fieldIDs = fieldIDline[2].Trim();

                    metadata.Add($"id_name_fieldIDs_{i}", id + "_" + name + "_" + fieldIDs);
                }

                metadata.Add("Fieldgroup output", this.RawText);
                this.Metrics.Add(new Metric("fieldCount", metricValue, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI FieldGroup output file has incorrect format for parsing");
            }

            return this.Metrics;
        }
    }
}
