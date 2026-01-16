// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::VirtualClient;
    using global::VirtualClient.Actions;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Generic parser for JSON results log.
    /// </summary>
    internal class JsonMetricsParser : MetricsParser
    {
        private EventContext eventContext;
        private ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMetricsParser"/> class.
        /// </summary>
        /// <param name="results">The generic script results in JSON format.</param>
        /// <param name="logger">ILogger for logging in parser.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the metric.</param>
        public JsonMetricsParser(string results, ILogger logger, EventContext eventContext)
            : base(results)
        {
            this.logger = logger;
            this.eventContext = eventContext;
        }

        /// <summary>
        /// Parses key metrics from the JSON based output log.
        /// </summary>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();

            try
            {
                JToken token = JToken.Parse(this.RawText);

                if (token.Type == JTokenType.Object)
                {
                    // Format 1: { "metric1": 123, "metric2": 2.5, ... }
                    JObject keyValuePairs = (JObject)token;
                    foreach (var keyValuePair in keyValuePairs.Properties())
                    {
                        if (double.TryParse(keyValuePair.Value.ToString(), out var value))
                        {
                            metrics.Add(new Metric(keyValuePair.Name, value, MetricRelativity.Undefined));
                        }
                        else
                        {
                            throw new WorkloadResultsException(
                                $"Invalid JSON metrics content formatting. The metric value for '{keyValuePair.Name}' is not a valid numeric data type. Provided metric value is '{keyValuePair.Value}'",
                                ErrorReason.InvalidResults);
                        }
                    }
                }
                else if (token.Type == JTokenType.Array)
                {
                    /* Format 2:
                        [
                            {
                                "Nmae": "metric1",
                                "Value": 0,
                                "Unit": "unit1",
                                "MetaData": {
                                    "metadata1": "m1",
                                    "metadata2": "m2"
                                }
                            }
                        ]
                    */
                    try
                    {
                        metrics = JsonConvert.DeserializeObject<List<Metric>>(this.RawText);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadResultsException(
                            "Invalid JSON metrics content formatting. Failed to deserialize the Array JSON Contents into Metrics format.", exc, ErrorReason.InvalidResults);
                    }

                    if (metrics.Any(m => string.IsNullOrWhiteSpace(m.Name)))
                    {
                        throw new WorkloadResultsException(
                            "Invalid JSON metrics content formatting. 'Name' is a required property for each metric, it can't be null or whitespace.",
                            ErrorReason.InvalidResults);
                    }
                }
                else
                {
                    throw new WorkloadResultsException(
                        "Invalid JSON metrics content formatting. The root element must be either an object or an array.",
                        ErrorReason.InvalidResults);
                }
            }
            catch (WorkloadResultsException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException(
                    $"Invalid JSON metrics content formatting. The metrics content must be in a valid JSON key/value pair format " +
                    $"(e.g. {{ \"metric1\": 1234, \"metric2\": 987.65, \"metric3\": 32.0023481 }} ) or an array of Json objects.",
                    exc,
                    ErrorReason.InvalidResults);
            }

            return metrics;
        }
    }
}
