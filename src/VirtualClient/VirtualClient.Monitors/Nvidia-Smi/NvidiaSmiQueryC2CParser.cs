// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for NvidiaSmi C2C output document.
    /// </summary>
    public class NvidiaSmiC2CParser : MetricsParser
    {
        private static readonly Regex GpuInfoExpression = new Regex(@"GPU (?<GPU>\d+): (?<Name>.+) \(UUID: (?<UUID>.+)\)", RegexOptions.Compiled);
        private static readonly Regex C2CLinkExpression = new Regex(@"C2C Link (?<LinkNumber>\d+): (?<LinkSpeed>[\d.]+) GB/s", RegexOptions.Compiled);

        /// <summary>
        /// Constructor for NvidiaSmiC2CParser.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public NvidiaSmiC2CParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Parses the raw text and returns a list of metrics.
        /// </summary>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();
            string[] lines = this.RawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string gpuName = null;
            string gpuUuid = null;
            int gpuNumber = -1;

            foreach (string line in lines)
            {
                Match gpuMatch = GpuInfoExpression.Match(line);
                if (gpuMatch.Success)
                {
                    gpuNumber = int.Parse(gpuMatch.Groups["GPU"].Value.Trim());
                    gpuName = gpuMatch.Groups["Name"].Value.Trim();
                    gpuUuid = gpuMatch.Groups["UUID"].Value.Trim();
                    continue;
                }

                Match c2cLinkMatch = C2CLinkExpression.Match(line);
                if (c2cLinkMatch.Success)
                {
                    int linkNumber = int.Parse(c2cLinkMatch.Groups["LinkNumber"].Value.Trim());
                    double linkSpeed = double.Parse(c2cLinkMatch.Groups["LinkSpeed"].Value.Trim());
                    IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                    {
                        { "GPU Name", gpuName },
                        { "GPU UUID", gpuUuid }
                    };
                    metrics.Add(new Metric($"GPU {gpuNumber}: C2C Link {linkNumber} Speed", linkSpeed, unit: "GB/s", description: "Nvidia-smi c2c", metadata: metadata));
                }
            }

            return metrics;
        }
    }
}