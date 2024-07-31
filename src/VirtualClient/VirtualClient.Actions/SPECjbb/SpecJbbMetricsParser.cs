// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;

    /// <summary>
    /// Parser for SpecJbb output document
    /// </summary>
    public class SpecJbbMetricsParser : MetricsParser
    {
        private const string OperationPerSecond = "jOPS";

        /// <summary>
        /// Constructor for <see cref="SpecJbbMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SpecJbbMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Metrics = new List<Metric>();

                // If the line doesn't have column, it's individual result.
                // If the line has column, it's the summary line.
                List<string> metrics = this.PreprocessedText.Split(",").ToList();
                foreach (string metric in metrics)
                {
                    string[] tokens = metric.Split("=");
                    string name = tokens[0];
                    string value = tokens[1];

                    this.Metrics.Add(new Metric(name.Trim(), (value.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) ? null : Convert.ToDouble(value), SpecJbbMetricsParser.OperationPerSecond, MetricRelativity.HigherIsBetter));
                }

                return this.Metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse SPECjbb metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            string runResultAnchor = "RUN RESULT:";
            // Find the line that starts with RUN RESULT
            // RUN RESULT: hbIR (max attempted) = 4222, hbIR (settled) = 4123, max-jOPS = 4188, critical-jOPS = 1666
            this.PreprocessedText = this.RawText.Split(Environment.NewLine, StringSplitOptions.None).ToList()
                .Where(l => l.Contains(runResultAnchor, StringComparison.Ordinal)).FirstOrDefault().Trim();
            this.PreprocessedText = this.PreprocessedText.Substring(this.PreprocessedText.LastIndexOf(runResultAnchor) + runResultAnchor.Length);
        }
    }
}