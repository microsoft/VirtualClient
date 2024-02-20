// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MLPerf output document
    /// </summary>
    public class MLPerfTrainingMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="MLPerfTrainingMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public MLPerfTrainingMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            string patternDelimiterE2E = $@"'e2e_time':\s*{TextParsingExtensions.DoubleTypeRegex}";
            string patternDelimiterTrainingPerSec = $@"'training_sequences_per_second':\s*{TextParsingExtensions.DoubleTypeRegex}";
            string patternDelimiterFinalLoss = $@"'final_loss':\s*{TextParsingExtensions.DoubleTypeRegex}";
            string patternDelimiterRawTrainTime = $@"'raw_train_time':\s*{TextParsingExtensions.DoubleTypeRegex}";
            string patternDelimiterAccuracy = $@"'eval_mlm_accuracy':\s*{TextParsingExtensions.DoubleTypeRegex}\s*}}\s*\n\(";

            this.AddMetrics("e2e_time", patternDelimiterE2E, "s", MetricRelativity.LowerIsBetter);
            this.AddMetrics("training_sequences_per_second", patternDelimiterTrainingPerSec, null, MetricRelativity.HigherIsBetter);
            this.AddMetrics("final_loss", patternDelimiterFinalLoss, null, MetricRelativity.LowerIsBetter);
            this.AddMetrics("raw_train_time", patternDelimiterRawTrainTime, "s", MetricRelativity.LowerIsBetter);
            this.AddMetrics("eval_mlm_accuracy", patternDelimiterAccuracy, null, MetricRelativity.HigherIsBetter);

            if (!this.Metrics.Any())
            {
                throw new SchemaException("The MlPerf Training output file has incorrect format for parsing");
            }

            return this.Metrics;
        }

        private void AddMetrics(string metricName, string patternDelimiter, string metricUnit, MetricRelativity metricRelativity)
        {
            Regex regex = new Regex(patternDelimiter);
            var matches = regex.Matches(this.PreprocessedText);
            if (matches.Count > 0)
            {
                this.Metrics.Add(new Metric(metricName, matches.Average(mt => double.Parse(mt.Groups[1].Value.Trim())), metricUnit, metricRelativity));
            }
        }
    }
}
