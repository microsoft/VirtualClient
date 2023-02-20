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
    using Microsoft.CodeAnalysis;
    using Microsoft.Identity.Client;
    using VirtualClient.Contracts;
    using YamlDotNet.Core.Tokens;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for DCGMI Proftester output document.
    /// </summary>
    public class DCGMIProftesterCommandParser : MetricsParser
    {
        /// <summary>
        /// To match status line of the result.
        /// </summary>
        private const string GetTestFields = @"GPU (\d+) TestField (\d+) test (\w+)";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// Constructor for <see cref="DCGMIProftesterCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIProftesterCommandParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// list of modules.
        /// </summary>
        public DataTable ProftesterResult { get; set; }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            string gpuID;
            string testFieldID;
            double testStatus;
            try
            {
                var testFieldMatches = Regex.Matches(this.PreprocessedText, GetTestFields);
                this.ThrowIfInvalidOutputFormat(testFieldMatches.Count);

                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                for (int i = 0; i < testFieldMatches.Count; i++)
                {
                    var testFieldLine = Regex.Split(testFieldMatches.ElementAt(i).Value.Trim(), SpaceDelimiter);
                    gpuID = testFieldLine[1].Trim();
                    testFieldID = testFieldLine[3].Trim();
                    string status = testFieldLine[5].Trim();
                    if (status == "PASSED")
                    {
                        testStatus = 1;
                    }
                    else
                    {
                        testStatus = 0;
                    }

                    this.Metrics.Add(new Metric($"GPU{gpuID}_TestField{testFieldID}_TestStatus", testStatus, metadata: metadata));
                }

                metadata.Add("output", this.RawText);
            }
            catch
            {
                throw new SchemaException("The DCGMI Proftester output file has incorrect format for parsing");
            }

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, $",", string.Empty);
        }

        private void ThrowIfInvalidOutputFormat(int testCount)
        {
            if (testCount < 1)
            {
                throw new SchemaException("The DCGMI Proftester output file has incorrect format for parsing");
            }
        }
    }
}
