// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using VirtualClient;
    using VirtualClient.Contracts;

    /// <summary>
    /// Consumer parser for Kafka.
    /// </summary>
    public class KafkaConsumerMetricsParser : MetricsParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaConsumerMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText"></param>
        public KafkaConsumerMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Kafka result.
        /// </summary>
        public DataTable KafkaResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();
            this.AddMetricsFromCsv(metrics);

            if (metrics.Count <= 0)
            {
                throw new SchemaException(
                    $"Invalid/unpexpected format. The Kafka benchmark workload consumer results were not in the expected format or did not contain valid measurements.");
            }

            return metrics;
        }

        private void AddMetricsFromCsv(List<Metric> metrics)
        {
            string consumerResult = this.RawText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();

            if (!string.IsNullOrEmpty(consumerResult))
            {
                var values = consumerResult.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (values.Length > 9)
                {
                    // The metric values are read in the order at which they exist within the Kafka Consumer
                    // benchmark CSV output.
                    if (double.TryParse(values[2], out double dataConsumedInMb))
                    {
                        metrics.Add(new Metric(
                            "data_consumed_in_megabyte",
                            dataConsumedInMb,
                            MetricUnit.Megabytes,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Total data consumed in MB."));
                    }

                    if (double.TryParse(values[3], out double mbPerSec))
                    {
                        metrics.Add(new Metric(
                            "megabyte_per_sec_throughput",
                            mbPerSec,
                            MetricUnit.MegabytesPerSecond,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Megabytes per second/ Throughput"));
                    }

                    if (double.TryParse(values[4], out double dataConsumedInNMsg))
                    {
                        metrics.Add(new Metric(
                            "data_consumed_in_nMsg",
                            dataConsumedInNMsg,
                            MetricUnit.Operations,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Total data consumed in number of messages."));
                    }

                    if (double.TryParse(values[5], out double nMsgPerSec))
                    {
                        metrics.Add(new Metric(
                            "nMsg_per_sec_throughput",
                            nMsgPerSec,
                            MetricUnit.OperationsPerSec,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Number of messages per second/ Throughput"));
                    }

                    if (double.TryParse(values[7], out double fetchTimeInMilliSec))
                    {
                        metrics.Add(new Metric(
                            "fetch_time_in_millisec",
                            fetchTimeInMilliSec,
                            MetricUnit.Milliseconds,
                            relativity: MetricRelativity.LowerIsBetter,
                            description: "Fetch time per milli seconds"));
                    }

                    if (double.TryParse(values[8], out double fetchMbPerSec))
                    {
                        metrics.Add(new Metric(
                            "fetch_megabyte_per_sec",
                            fetchMbPerSec,
                            MetricUnit.MegabytesPerSecond,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Fetch megabytes per second/ Throughput"));
                    }

                    if (double.TryParse(values[9], out double fetchNMsgPerSec))
                    {
                        metrics.Add(new Metric(
                            "fetch_nMsg_per_sec",
                            fetchNMsgPerSec,
                            MetricUnit.OperationsPerSec,
                            relativity: MetricRelativity.HigherIsBetter,
                            description: "Fetch number of messages per second/ Throughput"));
                    }
                }
            }
        }
    }
}