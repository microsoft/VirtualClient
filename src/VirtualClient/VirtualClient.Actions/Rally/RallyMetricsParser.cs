// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Elasticsearch-Rally workload output
    /// </summary>
    public class RallyMetricsParser : MetricsParser
    {
        private RallyResult result;

        /// <summary>
        /// Constructor for <see cref="RallyMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public RallyMetricsParser(string rawText)
            : base(rawText)
        {
            this.RawText = rawText;
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();

            List<RallyMetrics> statistics = this.result.Results.RallyMetrics;

            foreach (RallyMetrics op_metric in statistics)
            {
                string taskName = op_metric.Task;

                var metadata = new Dictionary<string, IConvertible>
                {
                    { "rallyVersion", this.result.RallyVersion },
                    { "raceId", this.result.RaceId },
                    { "raceTimestamp", this.result.RaceTimestamp },
                    { "track", this.result.Track },
                };

                // throughput
                string throughputMetricName = string.Join("_", taskName, "throughput", "median");

                metrics.Add(new Metric(throughputMetricName, op_metric.Throughput.Median, unit: op_metric.Throughput.Unit, metadata: metadata));

                // latency

                string latencyMetricName = string.Join("_", taskName, "latency");

                metrics.Add(new Metric(latencyMetricName + "_50_0", op_metric.Latency.FiftyP, unit: op_metric.Latency.Unit, metadata: metadata));
                metrics.Add(new Metric(latencyMetricName + "_90_0", op_metric.Latency.NinetyP, unit: op_metric.Latency.Unit, metadata: metadata));
                metrics.Add(new Metric(latencyMetricName + "_99_0", op_metric.Latency.NinetyNineP, unit: op_metric.Latency.Unit, metadata: metadata));

                // service time

                string serviceTimeMetricName = string.Join("_", taskName, "service_time");

                metrics.Add(new Metric(serviceTimeMetricName + "_50_0", op_metric.ServiceTime.FiftyP, unit: op_metric.ServiceTime.Unit, metadata: metadata));
                metrics.Add(new Metric(serviceTimeMetricName + "_90_0", op_metric.ServiceTime.NinetyP, unit: op_metric.ServiceTime.Unit, metadata: metadata));
                metrics.Add(new Metric(serviceTimeMetricName + "_99_0", op_metric.ServiceTime.NinetyNineP, unit: op_metric.ServiceTime.Unit, metadata: metadata));

                // processing time

                string processingTimeMetricName = string.Join("_", taskName, "processing_time");

                metrics.Add(new Metric(processingTimeMetricName + "_50_0", op_metric.ProcessingTime.FiftyP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));
                metrics.Add(new Metric(processingTimeMetricName + "_90_0", op_metric.ProcessingTime.NinetyP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));
                metrics.Add(new Metric(processingTimeMetricName + "_99_0", op_metric.ProcessingTime.NinetyNineP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));

                // duration

                string durationMetricName = string.Join("_", taskName, "duration");

                metrics.Add(new Metric(durationMetricName, op_metric.Duration, unit: "ms", metadata: metadata));
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.result = JsonConvert.DeserializeObject<RallyResult>(this.RawText);
        }
    }
}
