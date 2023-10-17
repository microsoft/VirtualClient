// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the Blender workload.
    /// </summary>
    public class BlenderMetricsParser : MetricsParser
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new ParameterDictionaryJsonConverter()
            }
        };

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
                IDictionary<string, IConvertible> workloadResults = this.RawText.FromJson<IDictionary<string, IConvertible>>(BlenderMetricsParser.SerializerSettings);

                if (workloadResults?.Any() == true)
                {
                    foreach (var entry in workloadResults)
                    {
                        metrics.Add(new Metric(entry.Key, entry.Value.ToDouble(CultureInfo.InvariantCulture)));
                    }
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
