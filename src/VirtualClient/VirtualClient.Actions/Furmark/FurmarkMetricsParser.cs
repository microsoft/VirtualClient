// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    ///  Parser for Furmark_scores output document.
    /// </summary>
    public class FurmarkMetricsParser : MetricsParser
    {
        private static readonly string ScorePattern = @"\[Score=(\d+)\]";
        private static readonly string DurationPattern = @"\[DurationInMs=(\d+)\]";

        /// <summary>
        /// Constructor for <see cref="FurmarkMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public FurmarkMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            string input = this.RawText;

            int score = int.Parse(Regex.Match(input, ScorePattern).Groups[1].Value);
            int durationInMs = int.Parse(Regex.Match(input, DurationPattern).Groups[1].Value);
            metrics.Add(new Metric("Score", score));
            metrics.Add(new Metric("DurationInMs", durationInMs, "ms"));
            return metrics;
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null)
            {
                throw new SchemaException("furmark workload didn't generate results files.");
            }
        }
    }
}
