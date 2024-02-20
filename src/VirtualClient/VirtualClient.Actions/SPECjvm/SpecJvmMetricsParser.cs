// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for SpecJvm output document
    /// </summary>
    public class SpecJvmMetricsParser : MetricsParser
    {
        private static string operationPerSecond = "ops/m";

        /// <summary>
        /// Constructor for <see cref="SpecJvmMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SpecJvmMetricsParser(string rawText)
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
                List<string> lines = this.PreprocessedText.Split(Environment.NewLine).ToList();
                foreach (string line in lines)
                {
                    if (line.Contains(":"))
                    {
                        Regex columnRegex = new Regex(@$":", RegexOptions.ExplicitCapture);
                        string[] nameAndValue = Regex.Split(line, columnRegex.ToString(), columnRegex.Options);
                        string metricName = nameAndValue[0].Trim();
                        double metricValue = Convert.ToDouble(nameAndValue[1].Replace(SpecJvmMetricsParser.operationPerSecond, string.Empty));
                        this.Metrics.Add(new Metric(metricName, metricValue, SpecJvmMetricsParser.operationPerSecond, MetricRelativity.HigherIsBetter));
                    }
                    else
                    {
                        Regex whiteSpaceRegex = new Regex(@$"(\s){{2,}}", RegexOptions.ExplicitCapture);
                        string[] nameAndValue = Regex.Split(line, whiteSpaceRegex.ToString(), whiteSpaceRegex.Options);
                        string metricName = nameAndValue[0].Trim();
                        string[] a = line.Split(" ");
                        double metricValue = Convert.ToDouble(nameAndValue[1]);
                        this.Metrics.Add(new Metric(metricName, metricValue, SpecJvmMetricsParser.operationPerSecond, MetricRelativity.HigherIsBetter));
                    }
                }

                return this.Metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse SPECjvm metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            /*
             * Only capture data in selected test section, which is between the third === line and fourth === line.
                ================================
                compress                      123.71                                  
                crypto                        228.47                                  
                derby                         288.43                                  
                mpegaudio                     86.42                                   
                scimark.large                 49.62                                   
                scimark.small                 197.48                                  
                serial                        90.27                                   
                sunflow                       48.02                                   
                Noncompliant composite result: 115.78 ops/m
                ================================
             */

            // Getting the text after the "===========" line. 
            Regex equalSignLine = new Regex(@$"(=){{2,}}({Environment.NewLine})", RegexOptions.Multiline | RegexOptions.ExplicitCapture);
            this.PreprocessedText = Regex.Split(this.RawText, equalSignLine.ToString(), equalSignLine.Options)[3].Trim();
        }
    }
}