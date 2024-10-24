// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Elasticsearch-Rally workload output
    /// </summary>
    public class RallyMetricsParser : MetricsParser
    {
        private readonly int verbosity;
        private RallyResult result;

        /// <summary>
        /// Constructor for <see cref="RallyMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="metricFilters">Verbosity to filter by.</param>
        public RallyMetricsParser(string rawText, IEnumerable<string> metricFilters)
            : base(rawText)
        {
            this.RawText = rawText;

            if (!metricFilters.Any())
            {
                this.verbosity = 1;
            }
            else
            {
                this.verbosity = int.Parse(metricFilters.First().Split(':').Last());
            }
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();

            List<RallyMetrics> statistics = this.result.Results.RallyMetrics;

            var metadata = new Dictionary<string, IConvertible>
                {
                    { "rallyVersion", this.result.RallyVersion },
                    { "raceId", this.result.RaceId },
                    { "raceTimestamp", this.result.RaceTimestamp },
                    { "track", this.result.Track },
                };

            foreach (RallyMetrics op_metric in statistics)
            {
                string taskName = op_metric.Task;

                // throughput
                string throughputMetricName = string.Join("_", taskName, "throughput");

                metrics.Add(new Metric(throughputMetricName + "_median", op_metric.Throughput.Median, unit: op_metric.Throughput.Unit, metadata: metadata));

                // latency

                string latencyMetricName = string.Join("_", taskName, "latency");

                metrics.Add(new Metric(latencyMetricName + "_50_0", op_metric.Latency.FiftyP, unit: op_metric.Latency.Unit, metadata: metadata));
                metrics.Add(new Metric(latencyMetricName + "_90_0", op_metric.Latency.NinetyP, unit: op_metric.Latency.Unit, metadata: metadata));

                // service time

                string serviceTimeMetricName = string.Join("_", taskName, "service_time");

                metrics.Add(new Metric(serviceTimeMetricName + "_50_0", op_metric.ServiceTime.FiftyP, unit: op_metric.ServiceTime.Unit, metadata: metadata));
                metrics.Add(new Metric(serviceTimeMetricName + "_90_0", op_metric.ServiceTime.NinetyP, unit: op_metric.ServiceTime.Unit, metadata: metadata));

                // processing time

                string processingTimeMetricName = string.Join("_", taskName, "processing_time");

                metrics.Add(new Metric(processingTimeMetricName + "_50_0", op_metric.ProcessingTime.FiftyP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));
                metrics.Add(new Metric(processingTimeMetricName + "_90_0", op_metric.ProcessingTime.NinetyP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));

                // duration

                string durationMetricName = string.Join("_", taskName, "duration");

                metrics.Add(new Metric(durationMetricName, op_metric.Duration, unit: "ms", metadata: metadata));

                if (this.verbosity > 1)
                {
                    // throughput

                    metrics.Add(new Metric(throughputMetricName + "_min", op_metric.Throughput.Min, unit: op_metric.Throughput.Unit, metadata: metadata));
                    metrics.Add(new Metric(throughputMetricName + "_max", op_metric.Throughput.Max, unit: op_metric.Throughput.Unit, metadata: metadata));
                    metrics.Add(new Metric(throughputMetricName + "_mean", op_metric.Throughput.Mean, unit: op_metric.Throughput.Unit, metadata: metadata));

                    // latency

                    metrics.Add(new Metric(latencyMetricName + "_99_0", op_metric.Latency.NinetyNineP, unit: op_metric.Latency.Unit, metadata: metadata));
                    metrics.Add(new Metric(latencyMetricName + "_100_0", op_metric.Latency.HundredP, unit: op_metric.Latency.Unit, metadata: metadata));
                    metrics.Add(new Metric(latencyMetricName + "_mean", op_metric.Latency.Mean, unit: op_metric.Latency.Unit, metadata: metadata));

                    // service time

                    metrics.Add(new Metric(serviceTimeMetricName + "_99_0", op_metric.ServiceTime.NinetyNineP, unit: op_metric.ServiceTime.Unit, metadata: metadata));
                    metrics.Add(new Metric(serviceTimeMetricName + "_100_0", op_metric.ServiceTime.HundredP, unit: op_metric.ServiceTime.Unit, metadata: metadata));
                    metrics.Add(new Metric(serviceTimeMetricName + "_mean", op_metric.ServiceTime.Mean, unit: op_metric.ServiceTime.Unit, metadata: metadata));

                    // processing time
                    metrics.Add(new Metric(processingTimeMetricName + "_99_0", op_metric.ProcessingTime.NinetyNineP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));
                    metrics.Add(new Metric(processingTimeMetricName + "_100_0", op_metric.ProcessingTime.HundredP, unit: op_metric.ProcessingTime.Unit, metadata: metadata));
                    metrics.Add(new Metric(processingTimeMetricName + "_mean", op_metric.ProcessingTime.Mean, unit: op_metric.ProcessingTime.Unit, metadata: metadata));

                    // error rate

                    string errorRateMetricName = string.Join("_", taskName, "error_rate");

                    metrics.Add(new Metric(errorRateMetricName, op_metric.ErrorRate, unit: string.Empty, metadata: metadata));
                }
            }

            if (this.verbosity > 1)
            {
                // extra metric information

                metrics.Add(new Metric(nameof(this.result.Results.TotalTime), this.result.Results.TotalTime, unit: "ms", metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.IndexingThrottleTime), this.result.Results.IndexingThrottleTime, unit: "ms", metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.MergeTime), this.result.Results.MergeTime, unit: "ms", metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.MergeCount), this.result.Results.MergeCount, unit: string.Empty, metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.RefreshTime), this.result.Results.RefreshTime, unit: "ms", metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.RefreshCount), this.result.Results.RefreshCount, unit: string.Empty, metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.FlushTime), this.result.Results.FlushTime, unit: "ms", metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.FlushCount), this.result.Results.FlushCount, unit: string.Empty, metadata: metadata));
                metrics.Add(new Metric(nameof(this.result.Results.MergeThrottleTime), this.result.Results.MergeThrottleTime, unit: "ms", metadata: metadata));
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
