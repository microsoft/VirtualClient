// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;
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
                JObject keyValuePairs = JObject.Parse(this.RawText);
                foreach (var keyValuePair in keyValuePairs.Properties())
                {
                    if (double.TryParse(keyValuePair.Value.ToString(), out var value))
                    {
                        metrics.Add(new Metric(keyValuePair.Name, value, MetricRelativity.Undefined));
                    }
                    else
                    {                        
                        this.logger.LogWarning($"The metric value for {keyValuePair.Name} couldn't be parsed, it should be of Double type.", this.eventContext);
                    }
                }
            }
            catch (Exception e)
            {
                this.logger.LogWarning($"The log File has incorrect JSON format. The log file should have metric name as keys and metricValue as Value in JSON format. Exception: {e}", this.eventContext);
            }

            return metrics;
        }
    }
}
