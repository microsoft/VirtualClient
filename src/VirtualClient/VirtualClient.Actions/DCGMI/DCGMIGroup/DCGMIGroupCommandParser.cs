// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for DCGMI Groups output document.
    /// </summary>
    public class DCGMIGroupCommandParser : MetricsParser
    {
        /// <summary>
        /// To match Groups line of the result.
        /// </summary>
        private const string GetGroups = @"(\d+)\s*group(.*?)\s*found.\s*";

        /// <summary>
        /// To match group ID line of the result.
        /// </summary>
        private const string GetGroupIDLines = @"\s*Group ID\s*\|\s*(\d+)\s*";

        /// <summary>
        /// To match group Name line of the result.
        /// </summary>
        private const string GetGroupNameLines = @"\|\s*Group Name\s*\|\s*(.*?)\s*\|";
        
        /// <summary>
        /// To match group entities line of the result.
        /// </summary>
        private const string GetEntitiesLines = @"\|\s*Entities\s*\|\s*(.*?)\s*\|";
        
        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// Split string at '|' char.
        /// </summary>
        private const string Delimiter = @"\|";

        /// <summary>
        /// Constructor for <see cref="DCGMIGroupCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIGroupCommandParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            double groupCount;
            string groupID;
            string groupName;
            string entities;

            try
            {
                var groupMatches = Regex.Matches(this.PreprocessedText, GetGroups);
                var groupIDMatches = Regex.Matches(this.PreprocessedText, GetGroupIDLines);
                var groupNameMatches = Regex.Matches(this.PreprocessedText, GetGroupNameLines);
                var entitiesMatches = Regex.Matches(this.PreprocessedText, GetEntitiesLines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                var groupLine = Regex.Split(groupMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);

                groupCount = double.Parse(groupLine[0].Trim());

                for (int i = 0; i < groupIDMatches.Count; i++)
                {
                    var groupIDLine = Regex.Split(groupIDMatches.ElementAt(i).Value.Trim(), Delimiter);
                    var groupNameLine = Regex.Split(groupNameMatches.ElementAt(i).Value.Trim(), Delimiter);
                    var entitiesLine = Regex.Split(entitiesMatches.ElementAt(i).Value.Trim(), Delimiter);

                    groupID = groupIDLine[1].Trim();
                    groupName = groupNameLine[1].Trim();
                    entities = entitiesLine[1].Trim();

                    metadata.Add($"groupid_groupname_entities_{i}", groupID + "_" + groupName + "_" + entities);
                }

                metadata.Add($"output", this.RawText);

                this.Metrics.Add(new Metric("GroupCount", groupCount, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI Group output file has incorrect format for parsing");
            }

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = this.RawText.Replace("->", string.Empty);
        }
    }
}
