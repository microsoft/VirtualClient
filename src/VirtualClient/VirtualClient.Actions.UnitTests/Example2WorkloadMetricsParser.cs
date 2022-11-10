// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    /// Example workload parser used in conjunction with the <see cref="ExampleWorkloadExecutor"/>
    /// to illustrate different testing methodologies.
    /// </summary>
    public class Example2WorkloadMetricsParser : MetricsParser
    {
        private Results workloadResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="Example2WorkloadMetricsParser"/> class.
        /// </summary>
        /// <param name="rawResults">Raw string output of a workload.</param>
        public Example2WorkloadMetricsParser(string rawResults)
            : base(rawResults)
        {
        }

        /// <summary>
        /// Returns the metrics parsed from the workload results.
        /// </summary>
        /// <returns></returns>
        public override IList<Metric> Parse()
        {
            try
            {
                this.workloadResults = this.RawText.FromJson<Results>();
            }
            catch (JsonException exc)
            {
                throw new SchemaException($"Invalid workload results. The results are not in the expected format: {this.RawText}", exc);
            }

            return new List<Metric>
            {
                new Metric("calculations/sec", this.workloadResults.CalculationsPerSecond),
                new Metric("avg. latency", this.workloadResults.AverageLatency, MetricUnit.Milliseconds),
                new Metric("score", this.workloadResults.Score)
            };
        }

        private class Results
        {
            [JsonProperty(PropertyName = "avgLatency", Required = Required.Always)]
            public double AverageLatency { get; set; }

            [JsonProperty(PropertyName = "calculationsPerSec", Required = Required.Always)]
            public double CalculationsPerSecond { get; set; }

            [JsonProperty(PropertyName = "score", Required = Required.Always)]
            public double Score { get; set; }
        }
    }
}
