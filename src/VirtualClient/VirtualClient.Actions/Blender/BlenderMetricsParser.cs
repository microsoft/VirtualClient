// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using MathNet.Numerics;
    using Newtonsoft.Json;
    using VirtualClient.Actions.Blender;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the Blender workload.
    /// </summary>
    public class BlenderMetricsParser : MetricsParser
    {
        /// <summary>
        /// Parser for the Blender workload
        /// </summary>
        /// <param name="rawText">The raw text from the Blender benchmark.</param>
        public BlenderMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Parses the metrics from the workload results.
        /// </summary>
        /// <returns>List of parsed Metrics.</returns>
        public override IList<Metric> Parse()
        {
            try
            {
                List<Metric> metrics = new List<Metric>();
                List<BlenderResult> blenderResults = JsonConvert.DeserializeObject<List<BlenderResult>>(this.RawText);
                foreach (BlenderResult blenderResult in blenderResults)
                {
                    metrics.Add(new Metric(blenderResult.Scene.Label, blenderResult.Stats.SamplesPerMinute, unit: "samples_per_minute"));
                }

                return metrics;
            }
            catch (JsonException exc)
            {
                throw new WorkloadResultsException(
                    "Workload results parsing failure. The example workload results are not valid or are not formatted in a valid JSON structure.",
                    exc,
                    ErrorReason.WorkloadResultsParsingFailed);
            }
        }
    }
}
