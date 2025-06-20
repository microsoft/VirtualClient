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
                    /* Format 2: {
                        "metrics": [
                            {
                                "metricName": "metric1",
                                "metricValue": "value1",
                                "metricUnit": "unit1",
                                "metricMetadata": {
                                    "metadata1": "m1",
                                    "metadata2": "m2"
                                }
                            }
                        ]
                    }*/
                    foreach (var metricObj in token.Children<JObject>())
                    {
                        string metricName = metricObj.Value<string>("metricName");
                        JToken metricValueToken = metricObj["metricValue"];
                        if (string.IsNullOrWhiteSpace(metricName))
                        {
                            throw new WorkloadResultsException(
                                $"Invalid JSON metrics content formatting. 'metricName' is a required property.",
                                ErrorReason.InvalidResults);
                        }

                        if (metricValueToken == null || !double.TryParse(metricValueToken.ToString(), out double metricValue))
                        {
                            throw new WorkloadResultsException(
                                $"Invalid JSON metrics content formatting. 'metricValue' for '{metricName}' is not a valid numeric data type. Provided metric value is '{metricValueToken}'",
                                ErrorReason.InvalidResults);
                        }

                        string metricUnit = metricObj.Value<string>("metricUnit");
                        IDictionary<string, IConvertible> metricMetadata = null;
                        if (metricObj.TryGetValue("metricMetadata", out JToken metadataToken) && metadataToken.Type == JTokenType.Object)
                        {
                            metricMetadata = metadataToken
                                .Children<JProperty>()
                                .ToDictionary(
                                    prop => prop.Name,
                                    prop => (IConvertible)Convert.ChangeType(prop.Value.ToString(), typeof(string)));
                        }

                        metrics.Add(
                            metricUnit != null
                                ? new Metric(metricName, metricValue, metricUnit, MetricRelativity.Undefined, tags: null, description: null, metadata: metricMetadata)
                                : new Metric(metricName, metricValue, MetricRelativity.Undefined, tags: null, description: null, metadata: metricMetadata));
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
                    $"(e.g. {{ \"metric1\": 1234, \"metric2\": 987.65, \"metric3\": 32.0023481 }} ) or an array of metric objects.",
                    exc,
                    ErrorReason.InvalidResults);
            }

            return metrics;
        }
    }
}
