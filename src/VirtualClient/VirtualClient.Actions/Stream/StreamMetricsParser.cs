// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using VirtualClient;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for STREAM results (Linux and Windows formats).
    /// </summary>
    public class StreamMetricsParser : MetricsParser
    {
        /// <summary>
        /// To match Linux-style metrics data rows.
        /// e.g. Copy:  18514.5  0.242368  0.231979  0.317779
        /// </summary>
        private static readonly Regex LinuxDataRowRegex = new Regex(
            @"^\s*(?<func>[\w\-\+]+):?\s+(?<best>\d+(\.\d+)?)\s+\d+(\.\d+)?\s+\d+(\.\d+)?(\s+\d+(\.\d+)?)?",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// To match Windows pipe-delimited metrics data rows.
        /// e.g. | Copy  | 42156.3  | 42890.7  | 41234.5  | 0.122345 | 0.119876 | 0.124567 |
        /// </summary>
        private static readonly Regex WindowsTableRowRegex = new Regex(
            @"^\|\s*(?<func>[\w\-\+]+):?\s*\|\s*(?<avg>\d+(\.\d+)?)\s*\|\s*(?<best>\d+(\.\d+)?)\s*\|\s*(?<worst>\d+(\.\d+)?)\s*\|\s*(?<avgtime>\d+(\.\d+)?)\s*\|\s*(?<mintime>\d+(\.\d+)?)\s*\|\s*(?<maxtime>\d+(\.\d+)?)\s*\|",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// To detect the Windows pipe table header row.
        /// </summary>
        private static readonly Regex WindowsHeaderRegex = new Regex(
            @"^\|\s*Function\s*\|",
            RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Constructor for <see cref="StreamMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public StreamMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            Regex dataRowRegex = WindowsHeaderRegex.IsMatch(this.PreprocessedText)
                ? WindowsTableRowRegex
                : LinuxDataRowRegex;

            IList<Metric> metrics = this.ExtractMetrics(dataRowRegex);

            if (metrics.Count == 0)
            {
                throw new SchemaException("The STREAM results have incorrect format/data for parsing.");
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            string text = this.RawText ?? string.Empty;
            text = Regex.Replace(text, "\r\n", "\n");
            text = Regex.Replace(text, "\n", Environment.NewLine);
            this.PreprocessedText = text.Trim();
        }

        /// <summary>
        /// To extract Best Rate metrics from regex matches.
        /// </summary>
        private IList<Metric> ExtractMetrics(Regex dataRowRegex)
        {
            List<Metric> metrics = new List<Metric>();
            MatchCollection matches = dataRowRegex.Matches(this.PreprocessedText);

            foreach (Match match in matches)
            {
                string func = match.Groups["func"].Value;
                string bestText = match.Groups["best"].Value;

                if (string.IsNullOrWhiteSpace(func) || string.IsNullOrWhiteSpace(bestText))
                {
                    continue;
                }

                if (!double.TryParse(bestText, NumberStyles.Float, CultureInfo.InvariantCulture, out double best))
                {
                    continue;
                }

                metrics.Add(
                    new Metric(
                        name: $"Best Rate {func}",
                        value: best,
                        unit: "MBps",
                        relativity: MetricRelativity.HigherIsBetter));
            }

            return metrics;
        }
    }
}