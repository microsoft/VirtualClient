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
                                "metricName": "metric1",
                                "metricValue": "value1",
                                "metricUnit": "unit1",
                                "metricMetadata": {
                                    "metadata1": "m1",
                                    "metadata2": "m2"
                                }
                            }
                        ]
                    */
                    var metricList = JsonConvert.DeserializeObject<List<CustomMetric>>(this.RawText);

                    foreach (var customMetric in metricList)
                    {
                        if (string.IsNullOrWhiteSpace(customMetric.Name))
                        {
                            throw new WorkloadResultsException(
                                $"Invalid JSON metrics content formatting. 'metricName' is a required property.",
                                ErrorReason.InvalidResults);
                        }

                        if (customMetric.Value == null || !double.TryParse(customMetric.Value.ToString(), out double metricValue))
                        {
                            throw new WorkloadResultsException(
                                $"Invalid JSON metrics content formatting. 'metricValue' for '{customMetric.Name}' is not a valid numeric data type. Provided metric value is '{customMetric.Value}'",
                                ErrorReason.InvalidResults);
                        }

                        // Robustly handle metricMetadata
                        IDictionary<string, IConvertible> metricMetadata = null;
                        if (customMetric.MetadataRaw != null && customMetric.MetadataRaw.Type != JTokenType.Null)
                        {
                            if (customMetric.MetadataRaw.Type == JTokenType.Object)
                            {
                                metricMetadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
                                foreach (var prop in customMetric.MetadataRaw.Children<JProperty>())
                                {
                                    metricMetadata[prop.Name] = prop.Value.ToString();
                                }
                            }
                            else
                            {
                                throw new WorkloadResultsException(
                                    $"Invalid JSON metrics content formatting. 'metricMetadata' for '{customMetric.Name}' must be a JSON object.",
                                    ErrorReason.InvalidResults);
                            }
                        }

                        metrics.Add(
                            customMetric.Unit != null
                                ? new Metric(customMetric.Name, metricValue, customMetric.Unit, MetricRelativity.Undefined, tags: null, description: null, metadata: metricMetadata)
                                : new Metric(customMetric.Name, metricValue, MetricRelativity.Undefined, tags: null, description: null, metadata: metricMetadata));
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

        internal class CustomMetric
        {
            [JsonProperty("metricName")]
            public string Name { get; set; }

            [JsonProperty("metricValue")]
            public object Value { get; set; }

            [JsonProperty("metricUnit")]
            public string Unit { get; set; }

            // Use JToken to allow robust handling
            [JsonProperty("metricMetadata")]
            public JToken MetadataRaw { get; set; }
        }
    }
}
