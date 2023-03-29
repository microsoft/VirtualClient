namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Sysbench CPU output document.
    /// </summary>
    public class SysbenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="SysbenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SysbenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();

            this.AddMetric(metrics, "Total number of events", "total\\snumber\\sof\\sevents:\\s*-?\\d+(\\.\\d+)?", string.Empty, MetricRelativity.HigherIsBetter);
            this.AddMetric(metrics, "Total Time", "total\\stime:\\s*-?\\d+(\\.\\d+)?", "Seconds");

            this.AddMetric(metrics, "Latency Min", "min:\\s*?\\d+(\\.\\d+)?", "milliSeconds", MetricRelativity.LowerIsBetter);
            this.AddMetric(metrics, "Latency Avg", "avg:\\s*?\\d+(\\.\\d+)?", "milliSeconds", MetricRelativity.LowerIsBetter);
            this.AddMetric(metrics, "Latency Max", "max:\\s*?\\d+(\\.\\d+)?", "milliSeconds", MetricRelativity.LowerIsBetter);
            this.AddMetric(metrics, "Latency 95th Percentile", "95(th)*\\spercentile:\\s*?\\d+(\\.\\d+)?", "milliSeconds", MetricRelativity.LowerIsBetter);
            
            this.AddMetric(metrics, "Thread Fairness Avg Events", "events\\s\\(avg/stddev\\):\\s*?\\d+(\\.\\d+)?", string.Empty, MetricRelativity.HigherIsBetter);
            this.AddMetric(metrics, "Thread Fairness Avg Execution Time", "execution\\stime\\s\\(avg/stddev\\):\\s*?\\d+(\\.\\d+)?", string.Empty, MetricRelativity.HigherIsBetter);

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = this.RawText;
        }

        private void AddMetric(List<Metric> metrics, string name, string regexToSearch, string unit = "", MetricRelativity metricRelativity = MetricRelativity.Undefined)
        {
            string metric = Regex.Match(this.RawText, regexToSearch).Value;
            double metricValue = Convert.ToDouble(Regex.Match(metric, "-?\\d+(\\.\\d+)?").Value);
            metrics.Add(new Metric(name, metricValue, unit, metricRelativity));
        }
    }
}
