// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MLPerf output document
    /// </summary>
    public class MLPerfMetricsParser : MetricsParser
    {
        private static readonly string AccuracyResultsPattern = @"(\w+)\s*=\s*([\d.]+)"; 
        private static readonly string PerformanceResultsPattern = @"([\w\d_.]+)\s*:\s*([\d.]+)";
        private static readonly string ValueSplitRegexPattern = @"^[^:]+:";
        private static readonly string RemoveEndDotPattern = @"\.$";

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

            JObject results = JObject.Parse(this.PreprocessedText);

            foreach (JProperty model in results.Properties())
            {
                try
                {
                    MetricRelativity relativity = MetricRelativity.HigherIsBetter;
                    
                    if (this.AccuracyMode && (model.Value.ToString().Contains("PASSED") || model.Value.ToString().Contains("FAILED")))
                    {
                        // Adding metric for accuracy result being passed/failed
                        metricName = model.Name + "-AccuracyMode";
                        bool value = model.Value.ToString().Contains("PASSED");
                        metricValue = Convert.ToDouble(value);
                        metricUnit = "PASS/FAIL";

                        Metric metric = new Metric(metricName, metricValue, metricUnit, relativity);
                        this.Metrics.Add(metric);

                        // Adding exact and threshold value for each accuracy run
                        string metricValueString = Regex.Replace(model.Value.ToString(), ValueSplitRegexPattern, string.Empty);
                        MatchCollection matches = Regex.Matches(metricValueString, AccuracyResultsPattern);
                        double thresholdValue = 1;
                        double accuracyValue = 0;

                        if (matches.Count == 2)
                        {
                            foreach (Match match in matches)
                            {
                                metricName = model.Name + "-" + match.Groups[1].Value + "Value";

                                string metricValueWithoutEndDot = Regex.Replace(match.Groups[2].Value, RemoveEndDotPattern, string.Empty);
                                metricValue = double.Parse(metricValueWithoutEndDot);
                                this.Metrics.Add(new Metric(metricName, metricValue));

                                if (match.Groups[1].Value.Contains("Threshold"))
                                {
                                    thresholdValue = metricValue;
                                }
                                else
                                {
                                    accuracyValue = metricValue;
                                }
                            }

                            // Adding ratio of Accuracy Value and Threshold Value
                            metricValue = accuracyValue / thresholdValue;
                            metricName = model.Name + "-Accuracy Threshold Ratio";
                            this.Metrics.Add(new Metric(metricName, metricValue, relativity));
                        }
                    }
                    else if (model.Value.ToString().Contains("INVALID") || model.Value.ToString().Contains("VALID"))
                    {
                        // Adding metric for perf result being valid/invalid
                        metricName = model.Name + "-PerformanceMode";
                        bool value = !model.Value.ToString().Contains("INVALID");
                        metricValue = Convert.ToDouble(value);
                        metricUnit = "VALID/INVALID";

                        Metric metric = new Metric(metricName, metricValue, metricUnit, relativity);
                        this.Metrics.Add(metric);

                        // Getting exact value of metric
                        string metricValueString = Regex.Replace(model.Value.ToString(), ValueSplitRegexPattern, string.Empty);
                        Match match = Regex.Match(metricValueString, PerformanceResultsPattern);

                        metricName = model.Name + "-" + match.Groups[1].Value;
                        metricValue = double.Parse(match.Groups[2].Value);
                        this.Metrics.Add(new Metric(metricName, metricValue));
                    }                  
                }
                catch
                {
                    // do nothing as this result file has non-valid values.
                }
            }

            return this.Metrics;
        }
    }
}
