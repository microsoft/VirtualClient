// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for NvidiaSmi output document.
    /// </summary>
    public class NvidiaSmiQueryNvLinkParser : MetricsParser
    {
        private static readonly Regex GpuInfoExpression = new Regex(@"GPU (?<GPU>\d+): (?<Name>.+) \(UUID: (?<UUID>.+)\)", RegexOptions.Compiled);
        private static readonly Regex NvLinkTxExpression = new Regex(@"Link (?<LinkNumber>\d+): Data Tx: (?<Throughput>[\d.]+) KiB", RegexOptions.Compiled);
        private static readonly Regex NvLinkRxExpression = new Regex(@"Link (?<LinkNumber>\d+): Data Rx: (?<Throughput>[\d.]+) KiB", RegexOptions.Compiled);

        /// <summary>
        /// Constructor for <see cref="NvidiaSmiQueryNvLinkParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public NvidiaSmiQueryNvLinkParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();
            string[] lines = this.RawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string gpuName = string.Empty;
            string gpuUuid = string.Empty;
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

                Match nvLinkTxMatch = NvLinkTxExpression.Match(line);
                if (nvLinkTxMatch.Success)
                {
                    int linkNumber = int.Parse(nvLinkTxMatch.Groups["LinkNumber"].Value.Trim());
                    double linkSpeed = double.Parse(nvLinkTxMatch.Groups["Throughput"].Value.Trim());
                    IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                    {
                        { "GPU Name", gpuName },
                        { "GPU UUID", gpuUuid }
                    };
                    metrics.Add(new Metric($"GPU {gpuNumber}: NvLink Tx {linkNumber} Throughput", linkSpeed, unit: "KiB", description: "Nvidia-smi nvlink", metadata: metadata));
                }

                Match nvLinkRxMatch = NvLinkRxExpression.Match(line);
                if (nvLinkRxMatch.Success)
                {
                    int linkNumber = int.Parse(nvLinkRxMatch.Groups["LinkNumber"].Value.Trim());
                    double linkSpeed = double.Parse(nvLinkRxMatch.Groups["Throughput"].Value.Trim());
                    IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                    {
                        { "GPU Name", gpuName },
                        { "GPU UUID", gpuUuid }
                    };
                    metrics.Add(new Metric($"GPU {gpuNumber}: NvLink Rx {linkNumber} Throughput", linkSpeed, unit: "KiB", description: "Nvidia-smi nvlink", metadata: metadata));
                }
            }

            return metrics;
        }
    }
}
