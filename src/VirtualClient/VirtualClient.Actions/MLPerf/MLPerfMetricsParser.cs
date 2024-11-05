// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MLPerf output document
    /// </summary>
    public class MLPerfMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="MLPerfMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="accuracyMode">Mode for which output file needs to be parsed.</param>
        public MLPerfMetricsParser(string rawText, bool accuracyMode)
            : base(rawText)
        {
            this.AccuracyMode = accuracyMode;
        }

        private List<Metric> Metrics { get; set; }

        private bool AccuracyMode { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            string metricName = string.Empty;
            double metricValue = 0;
            string metricUnit = string.Empty;
            MetricRelativity metricRelativity = MetricRelativity.Undefined;

            JObject parsedObject = JObject.Parse(this.RawText);

            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();
            metadata["config_name"] = $"{(string)parsedObject["config_name"]}";
            metadata["benchmark_short"] = $"{(string)parsedObject["benchmark_short"]}";
            metadata["benchmark_full"] = $"{(string)parsedObject["benchmark_full"]}";
            metadata["scenario"] = $"{(string)parsedObject["scenario"]}";

            if (this.AccuracyMode)
            {
                // Adding metric for accuracy result being passed/failed
                metricName = "AccuracyMode";
                bool passed = (bool)parsedObject["accuracy"][0]["pass"];
                metricValue = Convert.ToDouble(passed);
                metricUnit = "PASS/FAIL";
                metricRelativity = MetricRelativity.HigherIsBetter;

                this.Metrics.Add(new Metric(metricName, metricValue, metricUnit, metricRelativity, metadata: metadata));

                // Adding metric for accuracy threshold
                metricName = "ThresholdValue";
                metricValue = (double)parsedObject["accuracy"][0]["threshold"];
                metricRelativity = MetricRelativity.Undefined;

                this.Metrics.Add(new Metric(metricName, metricValue, metadata: metadata));

                // Adding metric for accuracy value
                metricName = "AccuracyValue";
                metricValue = (double)parsedObject["accuracy"][0]["value"];
                metricRelativity = MetricRelativity.Undefined;

                this.Metrics.Add(new Metric(metricName, metricValue, metadata: metadata));

                // Adding metric for accuracy value
                metricName = "Accuracy Threshold Ratio";
                metricValue = (double)parsedObject["accuracy"][0]["value"] / (double)parsedObject["accuracy"][0]["threshold"];
                metricRelativity = MetricRelativity.HigherIsBetter;

                this.Metrics.Add(new Metric(metricName, metricValue, metricRelativity, metadata: metadata));
            }
            else
            {
                // Adding metric for performance result being valid/invalid
                metricName = "PerformanceMode";
                metricValue = Convert.ToDouble((string)parsedObject["result_validity"] == "VALID");
                metricUnit = "VALID/INVALID";
                metricRelativity = MetricRelativity.HigherIsBetter;

                this.Metrics.Add(new Metric(metricName, metricValue, metricUnit, metricRelativity, metadata: metadata));

                // Adding metric for perofmrnace result value
                metricName = $"{(string)parsedObject["scenario_key"]}";
                string scenarioValue = ((string)parsedObject["summary_string"]).Split(" ")[1];
                scenarioValue = scenarioValue.Substring(0, scenarioValue.Length - 1);
                metricValue = Convert.ToDouble(scenarioValue);

                this.Metrics.Add(new Metric(metricName, metricValue, metadata: metadata));
            }

            return this.Metrics;
        }
    }
}
