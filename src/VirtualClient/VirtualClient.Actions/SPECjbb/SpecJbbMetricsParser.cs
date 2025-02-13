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
        private List<Metric> metrics;

        /// <summary>
        /// Constructor for <see cref="SpecJbbMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SpecJbbMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.metrics = new List<Metric>();

                // If the line doesn't have column, it's individual result.
                // If the line has column, it's the summary line.
                List<string> metrics = this.PreprocessedText.Split(",").ToList();
                foreach (string metric in metrics)
                {
                    string[] tokens = metric.Split("=");
                    string name = tokens[0];
                    string value = tokens[1];

                    if (value.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
                    {
                        this.metrics.Add(new Metric($"{name.Trim()}_Missing", 1, null, MetricRelativity.LowerIsBetter));
                    }
                    else
                    {
                        this.metrics.Add(new Metric(name.Trim(), Convert.ToDouble(value), SpecJbbMetricsParser.OperationPerSecond, MetricRelativity.HigherIsBetter, verbosity: 0));
                    }
                }

                return this.metrics;
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