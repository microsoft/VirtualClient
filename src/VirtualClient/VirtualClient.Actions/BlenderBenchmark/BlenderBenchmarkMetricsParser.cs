// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    /// <summary>
    /// Parser for the Blender workload.
    /// </summary>
    public class BlenderBenchmarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// Parser for the Blender workload
        /// </summary>
        /// <param name="rawText">The raw text from the Blender benchmark.</param>
        public BlenderBenchmarkMetricsParser(string rawText)
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
                var metrics = new List<Metric>();
                var blenderResults = JsonConvert.DeserializeObject<List<BlenderBenchmarkResult>>(this.RawText);
                foreach (BlenderBenchmarkResult blenderResult in blenderResults)
                {
                    var metadata = new Dictionary<string, IConvertible>
                    {
                        { "blenderVersion", blenderResult.BlenderVersion.Version },
                        { "benchmarkLauncher", blenderResult.BenchmarkLauncher.Label },

                        // The deviceName and deviceType section contains the device that was tested.
                        { "deviceName", blenderResult.DeviceInfo.ComputeDevices[0].Name },
                        { "deviceType", blenderResult.DeviceInfo.ComputeDevices[0].Type },
                        { "timeLimit",  blenderResult.Stats.TimeLimit }
                    };

                    metrics.Add(new Metric(nameof(blenderResult.Stats.DevicePeakMemory), blenderResult.Stats.DevicePeakMemory, unit: "mb", metadata: metadata));
                    metrics.Add(new Metric(nameof(blenderResult.Stats.NumberOfSamples), blenderResult.Stats.NumberOfSamples, unit: "sample", metadata: metadata));
                    metrics.Add(new Metric(nameof(blenderResult.Stats.TimeForSamples), blenderResult.Stats.TimeForSamples, unit: "second", metadata: metadata));
                    metrics.Add(new Metric(nameof(blenderResult.Stats.SamplesPerMinute), blenderResult.Stats.SamplesPerMinute, unit: "samples_per_minute", metadata: metadata));
                    metrics.Add(new Metric(nameof(blenderResult.Stats.TotalRenderTime), blenderResult.Stats.TotalRenderTime, unit: "second", metadata: metadata));
                    metrics.Add(new Metric(nameof(blenderResult.Stats.RenderTimeNoSync), blenderResult.Stats.RenderTimeNoSync, unit: "second", metadata: metadata));
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
