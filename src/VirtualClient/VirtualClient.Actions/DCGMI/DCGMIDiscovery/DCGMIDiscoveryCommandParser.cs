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
    using Microsoft.Azure.Amqp.Framing;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for DCGMI Discovery output document.
    /// </summary>
    public class DCGMIDiscoveryCommandParser : MetricsParser
    {
        /// <summary>
        /// To match NvSwitch line of the result.
        /// </summary>
        private const string GetNvSwitchLines = @"(\d+)\s*NvSwitch(.*?)\s*found.\s*";

        /// <summary>
        /// To match GPU line of the result.
        /// </summary>
        private const string GetGPULines = @"(\d+)\s*GPU(.*?)\s*found.\s*";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// Constructor for <see cref="DCGMIDiscoveryCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIDiscoveryCommandParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            double gpuCount;
            double nvSwitchCount;
            try
            {
                var nvSwitchMatches = Regex.Matches(this.RawText, GetNvSwitchLines);
                var gpuMatches = Regex.Matches(this.RawText, GetGPULines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                var nvSwitchLine = Regex.Split(nvSwitchMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);
                var gpuLine = Regex.Split(gpuMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);

                nvSwitchCount = double.Parse(nvSwitchLine[0].Trim());
                gpuCount = double.Parse(gpuLine[0].Trim());

                metadata.Add($"output", this.PreprocessedText);

                this.Metrics.Add(new Metric("GPUCount", gpuCount, metadata: metadata));
                this.Metrics.Add(new Metric("NvSwitchCount", nvSwitchCount, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI Discovery output file has incorrect format for parsing");
            }

            return this.Metrics;
        }
    }
}
