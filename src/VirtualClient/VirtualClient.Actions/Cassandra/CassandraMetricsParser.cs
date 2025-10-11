namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for Cassandra output document.
    /// </summary>
    public class CassandraMetricsParser : MetricsParser
    {
        private static readonly Regex OpRateRegex = new Regex(@"Op rate\s+:\s+([\d,]+) op/s", RegexOptions.Multiline);
        private static readonly Regex PartitionRateRegex = new Regex(@"Partition rate\s+:\s+([\d,]+) pk/s", RegexOptions.Multiline);
        private static readonly Regex RowRateRegex = new Regex(@"Row rate\s+:\s+([\d,]+) row/s", RegexOptions.Multiline);
        private static readonly Regex LatencyMeanRegex = new Regex(@"Latency mean\s+:\s+([\d.]+) ms", RegexOptions.Multiline);
        private static readonly Regex LatencyMaxRegex = new Regex(@"Latency max\s+:\s+([\d.]+) ms", RegexOptions.Multiline);
        private static readonly Regex TotalErrorsRegex = new Regex(@"Total errors\s+:\s+([\d,]+)", RegexOptions.Multiline);
        private static readonly Regex TotalOperationTimeRegex = new Regex(@"Total operation time\s+:\s+(\d{2}:\d{2}:\d{2})", RegexOptions.Multiline);

        /// <summary>
        /// constructor for <see cref="CassandraMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public CassandraMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// List of parsed metrics from the YCSB output.
        /// </summary>
        public List<Metric> Metrics { get; set; } = new List<Metric>();

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.ThrowIfInvalidOutputFormat();
            this.ExtractMetrics();

            return this.Metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Normalize the text to ensure consistent formatting.
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n\n", "\n"); // Consolidate multiple newlines
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (string.IsNullOrWhiteSpace(this.PreprocessedText))
            {
                throw new SchemaException("The Cassandra Stress output has incorrect format for parsing: empty or null text.");
            }

            if (!OpRateRegex.IsMatch(this.PreprocessedText))
            {
                throw new SchemaException("The Cassandra Stress output has incorrect format for parsing: missing key metrics.");
            }
        }
 
        private void ExtractMetrics()
        {
            this.ExtractMetric(OpRateRegex, "Op Rate", "ops/s");
            this.ExtractMetric(PartitionRateRegex, "Partition Rate", "pk/s");
            this.ExtractMetric(RowRateRegex, "Row Rate", "row/s");
            this.ExtractMetric(LatencyMeanRegex, "Latency Mean", "ms");
            this.ExtractMetric(LatencyMaxRegex, "Latency Max", "ms");
            this.ExtractMetric(TotalErrorsRegex, "Total Errors", "count");
            // this.ExtractMetric(TotalOperationTimeRegex, "Total Operation Time", "time");
            var match = TotalOperationTimeRegex.Match(this.PreprocessedText);
            if (match.Success)
            {
                if (TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan operationTime))
                {
                    this.Metrics.Add(new Metric("Total Operation Time", operationTime.ToString(@"hh\:mm\:ss"), "hh:mm:ss"));
                }
                else
                {
                    throw new FormatException($"Invalid operation time format: {match.Groups[1].Value}");
                }
            }

        }

        private void ExtractMetric(Regex regex, string metricName, string unit)
        {
            var match = regex.Match(this.PreprocessedText);
            if (match.Success)
            {
                this.Metrics.Add(new Metric(metricName, match.Groups[1].Value, unit));
            }
        }
    }
}


