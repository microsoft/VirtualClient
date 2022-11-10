// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Prime95 Workload.
    /// </summary>
    public class Prime95MetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="Prime95MetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public Prime95MetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();            

            // Add count of Self-Tests
            int selfTestPassCount = 0;
            int fatalErrorsCount = 0;
            foreach (string line in this.PreprocessedText.Split("\n"))
            {
                if (Regex.IsMatch(line, @"Self-test.*K.*passed.*"))
                {
                    selfTestPassCount++;
                }

                if (Regex.IsMatch(line, @"FATAL ERROR.*"))
                {
                    fatalErrorsCount++;
                }
            }

            metrics.Add(new Metric("passTestCount", selfTestPassCount, MetricRelativity.HigherIsBetter));
            metrics.Add(new Metric("failTestCount", fatalErrorsCount, MetricRelativity.LowerIsBetter));

            if (metrics.Count != 2)
            {
                throw new WorkloadResultsException($"The Prime95 Workload did not generate valid metrics! ");
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Converting all CRLF(Windows EOL) to LF(Unix EOL).
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");

            // Converting all LF to CRLF.
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, "\n", "\r\n");

            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.PreprocessedText.Trim();
        }        
    }
}
