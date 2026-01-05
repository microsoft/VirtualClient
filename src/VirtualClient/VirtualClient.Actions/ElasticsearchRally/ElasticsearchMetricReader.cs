namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using Microsoft.VisualBasic;

    internal class ElasticsearchMetricReader
    {
        /// <summary>
        /// Reads Elasticsearch Rally metrics from the provided report contents.
        /// </summary>
        /// <param name="reportContents"></param>
        /// <param name="metadata"></param>
        /// <returns></returns>
        public static IList<Metric> Read(
            string[] reportContents,
            Dictionary<string, IConvertible> metadata)
        {
            IList<Metric> metrics = new List<Metric>();

            foreach (string line in reportContents)
            {
                // Metric,Task,Value,Unit
                string[] cols = line.Split(',');

                if (cols.Length != 4 || !double.TryParse(cols[2], out double value))
                {
                    continue;
                }

                string metricName = cols[0].ToLower();
                string taskName = cols[1].ToLower();
                string unit = cols[3];

                int verbosity = 1; // 0: Critical, 1: Standard, 2: Informational.
                MetricRelativity relativity = MetricRelativity.Undefined;

                if (
                    metricName.StartsWith(MetricNames.Mean) ||
                    metricName.StartsWith(MetricNames.P100))
                {
                    verbosity = 0;
                }

                if (metricName.EndsWith(MetricNames.Throughput))
                {
                    relativity = MetricRelativity.HigherIsBetter;
                }
                else if (
                    metricName.EndsWith(MetricNames.Latency) ||
                    metricName.EndsWith(MetricNames.ServiceTime) || 
                    metricName.EndsWith(MetricNames.ProcessingTime))
                {
                    relativity = MetricRelativity.LowerIsBetter;
                }

                if (taskName.Length > 0)
                {
                    metricName = $"{taskName}_{metricName}";
                }

                metricName = metricName.Replace(' ', '_').Replace('-', '_');

                Metric metric = new Metric(
                    name: metricName,
                    value: value,
                    unit: unit,
                    relativity: relativity,
                    verbosity: verbosity,
                    metadata: metadata);

                metrics.Add(metric);
            }

            return metrics;
        }

        private struct MetricNames
        {
            public const string Latency = "latency";
            public const string Mean = "mean";
            public const string ServiceTime = "service time";
            public const string P100 = "100th";
            public const string ProcessingTime = "processing time";
            public const string Throughput = "throughput";
        }
    }
}
