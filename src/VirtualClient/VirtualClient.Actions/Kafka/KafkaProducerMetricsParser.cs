using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    /// <summary>
    /// Producer parser for Kafka.
    /// </summary>
    public class KafkaProducerMetricsParser : MetricsParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaProducerMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText"></param>
        public KafkaProducerMetricsParser(string rawText)
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
                    $"Invalid/unpexpected format. The Kafka benchmark workload producer results were not in the expected format or did not contain valid measurements.");
            }

            return metrics;
        }

        private void AddMetricsFromCsv(List<Metric> metrics)
        {
            string producerResult = this.RawText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();
            if (!string.IsNullOrEmpty(producerResult))
            {
                var values = producerResult.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => Regex.Replace(item, "\\s(.*)", string.Empty))
                    .ToList();

                // The metric values are read in the order at which they exist within the Kafka Producer
                // benchmark CSV output.
                if (double.TryParse(values[0], out double totalRecordsSent))
                {
                    metrics.Add(new Metric(
                        "Total_Records_Sent",
                        totalRecordsSent,
                        MetricUnit.Operations,
                        relativity: MetricRelativity.Undefined,
                        description: "Total records sent."));
                }

                if (double.TryParse(values[1], out double recordsPerSec))
                {
                    metrics.Add(new Metric(
                        "Records_Per_Sec",
                        recordsPerSec,
                        MetricUnit.OperationsPerSec,
                        relativity: MetricRelativity.HigherIsBetter,
                        description: "Records sent per second."));
                }

                if (double.TryParse(values[2], out double latencyAvg))
                {
                    metrics.Add(new Metric(
                        "Latency-Avg",
                        latencyAvg,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Average latency for requests/operations during the period of time."));
                }

                if (double.TryParse(values[3], out double latencyMax))
                {
                    metrics.Add(new Metric(
                        "Latency-Max",
                        latencyMax,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "Maximum latency for requests/operations during the period of time."));
                }

                if (double.TryParse(values[4], out double latencyP50))
                {
                    metrics.Add(new Metric(
                        "Latency-P50",
                        latencyP50,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 50% of all requests was at or under this value."));
                }

                if (double.TryParse(values[5], out double latencyP95))
                {
                    metrics.Add(new Metric(
                        "Latency-P95",
                        latencyP95,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 95% of all requests was at or under this value."));
                }

                if (double.TryParse(values[6], out double latencyP99))
                {
                    metrics.Add(new Metric(
                        "Latency-P99",
                        latencyP99,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 99% of all requests was at or under this value."));
                }

                if (double.TryParse(values[7], out double latencyP99_9))
                {
                    metrics.Add(new Metric(
                        "Latency-P99.9",
                        latencyP99_9,
                        MetricUnit.Milliseconds,
                        relativity: MetricRelativity.LowerIsBetter,
                        description: "The latency for 99.9% of all requests was at or under this value."));
                }
            }
        }
    }
}
