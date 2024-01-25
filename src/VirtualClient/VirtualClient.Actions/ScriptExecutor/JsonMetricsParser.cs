// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.Toolkit.VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::VirtualClient;
    using global::VirtualClient.Actions;
    using global::VirtualClient.Contracts;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Generic parser for JSON results log.
    /// </summary>
    internal class JsonMetricsParser : MetricsParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMetricsParser"/> class.
        /// </summary>
        /// <param name="results">The generic script results in JSON format.</param>
        public JsonMetricsParser(string results)
            : base(results)
        {
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

                    // else
                    // {                        
                    //     // throw new WarningException($"The metric value for {keyValuePair.Name} couldn't be parsed, it should be of Double type.");
                    // }
                }
            }
            catch (Exception e)
            {
                throw new WarningException($"The log File has incorrect JSON format. The log file should have metric name as keys and metricValue as Value in JSON format. Exception: {e}");
            }

            return metrics;
        }
    }
}
