// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static System.Collections.Specialized.BitVector32;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for Linpack test results file.
    /// </summary>
    public class HPLMetricsParser : MetricsParser
    {
        /// <summary>
        /// To match metrics line of the result.
        /// </summary>
        private const string GetMetricsLine = @"(?<=WR).*(?=\n)";
        // private const string GetMetricsLine = @"WR[0-9]+R[0-9]+C[0-9]+$";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SplitAtSpace = @"\s{1,}";

        /*/// <summary>
        /// Sectionize by === lines.
        /// </summary>
        private static readonly Regex LinpackSectionDelimiter = new Regex(@"===*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex LinpackDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);*/

        /// <summary>
        /// constructor for <see cref="HPLMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HPLMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Results for Linpack results .
        /// </summary>
        public DataTable LinpackResult { get; set; }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var matches = Regex.Matches(this.RawText, GetMetricsLine);
            if (matches.Count == 0)
            {
                throw new SchemaException("The HPLinpack output file has incorrect format for parsing");
            }
            else
            {
                this.Metrics = new List<Metric>();
                for (int i = 0; i < matches.Count; i++)
                {
                    var st = Regex.Split(matches.ElementAt(i).Value, SplitAtSpace);

                    /*this.TotalThroughput += Convert.ToDouble(st[1]);
                    this.metrics.Add(new Metric($"Throughput_{i + 1}", Convert.ToDouble(st[1]), "req/sec", MetricRelativity.HigherIsBetter));
                    this.P50Latency = Math.Max(this.P50Latency, Convert.ToDouble(st[5]));
                    this.P90Latency = Math.Max(this.P90Latency, Convert.ToDouble(st[6]));
                    this.P95Latency = Math.Max(this.P95Latency, Convert.ToDouble(st[7]));
                    this.P99Latency = Math.Max(this.P99Latency, Convert.ToDouble(st[8]));
                    this.P99_9Latency = Math.Max(this.P99_9Latency, Convert.ToDouble(st[9]));*/
                    this.Metrics.Add(new Metric($"N_WR{st[0]}", Convert.ToDouble(st[1])));
                    this.Metrics.Add(new Metric($"NB_WR{st[0]}", Convert.ToDouble(st[2])));
                    this.Metrics.Add(new Metric($"P_WR{st[0]}", Convert.ToDouble(st[3])));
                    this.Metrics.Add(new Metric($"Q_WR{st[0]}", Convert.ToDouble(st[4])));
                    this.Metrics.Add(new Metric($"Time_WR{st[0]}", Convert.ToDouble(st[5])));
                    this.Metrics.Add(new Metric($"GFlops_WR{st[0]}", Convert.ToDouble(st[6])));
                }
            }

            /*this.Metrics = new List<Metric>();
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LinpackSectionDelimiter);
            this.ThrowIfInvalidOutputFormat();
            this.CalculateLinpackResult();*/
            return this.Metrics;
        }

        /*/// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"(\r\n|\n)+", $"{Environment.NewLine}");
            this.PreprocessedText = this.PreprocessedText.Replace("T/V", $"LinpackResults{Environment.NewLine}T/V");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @$"--*{Environment.NewLine}", string.Empty);
        }

        private void CalculateLinpackResult()
        {
            this.LinpackResult = DataTableExtensions.ConvertToDataTable(
                            this.Sections["LinpackResults"], HPLMetricsParserUpdated.LinpackDataTableDelimiter, "LinpackResults");

            DataRow row = this.LinpackResult.Rows[0];
            int columnIndex = 0;

            foreach (DataColumn column in this.LinpackResult.Columns)
            {
                double metricValue;
                string value = row[columnIndex].ToString().Trim();
                if (double.TryParse(value, out metricValue))
                {
                    this.Metrics.Add(new Metric(column.ColumnName, metricValue));
                }

                columnIndex++;
            }
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count != 4 || !this.Sections.ContainsKey("LinpackResults"))
            {
                throw new SchemaException("The Linpack output file has incorrect format for parsing");
            }
        }*/
    }
}
