// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client workload results parser.
    /// </summary>
    public class ExampleWorkloadMetricsParser : MetricsParser
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new ParameterDictionaryJsonConverter()
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadMetricsParser"/> class.
        /// </summary>
        /// <param name="results">Results from the example workload.</param>
        public ExampleWorkloadMetricsParser(string results)
            : base(results)
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
                IDictionary<string, IConvertible> workloadResults = this.RawText.FromJson<IDictionary<string, IConvertible>>(ExampleWorkloadMetricsParser.SerializerSettings);

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
