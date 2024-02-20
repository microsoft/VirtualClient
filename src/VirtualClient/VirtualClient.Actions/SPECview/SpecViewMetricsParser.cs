// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the SPECview workload.
    /// </summary>
    public class SpecViewMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="SpecViewMetricsParser"/>
        /// </summary>
        /// <param name="rawText">The raw text from the SPECview export process.</param>
        public SpecViewMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var metrics = new List<Metric>();
            int index;
            string name, metricName;
            double weight, fps;
            IDictionary<string, IConvertible> metadata;

            string[] lines = this.RawText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Skip header lines
                if ((line == "Composites") | (line == "viewset,index,name,weight,fps"))
                {
                    continue;
                }

                string[] parts = line.Split(',');

                // Check if the line is a composite row - only two items
                if (parts.Length == 2)
                {
                    // composite rows will have invalid index -1 and weight 100%
                    index = -1;
                    weight = 100;
                    name = string.Empty;
                    fps = double.Parse(parts[1]);
                    metricName = "compositeScore";
                }

                // Parsing individual test scores
                else if (parts.Length == 5)
                {
                    index = int.Parse(parts[1]);
                    name = parts[2];
                    weight = double.Parse(parts[3]);
                    fps = double.Parse(parts[4]);
                    metricName = "individualScore";
                }
                else
                {
                    throw new WorkloadException($"Exceptions occurred when trying to parse the workload result of 'SPEcviewperf'.", ErrorReason.WorkloadFailed);
                }

                metadata = new Dictionary<string, IConvertible> { { "weight", weight }, { "index", index }, { "name", name } };
                metrics.Add(new Metric(metricName, value: fps, unit: "fps", metadata: metadata));
            }

            return metrics;
        }
    }
}
