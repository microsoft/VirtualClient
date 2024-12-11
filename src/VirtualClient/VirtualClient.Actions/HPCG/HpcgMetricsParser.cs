// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Hpcg output document
    /// </summary>
    public class HpcgMetricsParser : MetricsParser
    {
        private const string Gflops = "Gflop/s";

        /// <summary>
        /// Constructor for <see cref="HpcgMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HpcgMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();

                this.Metrics = new List<Metric>();
                Regex resultRegex = new Regex($@"Final Summary::HPCG result is VALID with a GFLOP\/s rating of={TextParsingExtensions.DoubleTypeRegex}", RegexOptions.Multiline);
                Match match = Regex.Match(this.PreprocessedText, resultRegex.ToString(), resultRegex.Options);

                if (match.Success)
                {
                    this.Metrics.Add(new Metric($"Total {Gflops}", Convert.ToDouble(match.Groups[1].Value), HpcgMetricsParser.Gflops, MetricRelativity.HigherIsBetter));
                }

                return this.Metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse the HPCG metrics from the results.", exc, ErrorReason.InvalidResults);
            }
        }
    }
}