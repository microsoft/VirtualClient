// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for SuperBenchmark output document
    /// </summary>
    public class SuperBenchmarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="SuperBenchmarkMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public SuperBenchmarkMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            JObject results = JObject.Parse(this.PreprocessedText);
            foreach (JProperty model in results.Properties())
            {
                try
                {
                    MetricRelativity relativity = MetricRelativity.Undefined;
                    
                    if (model.Name.Contains("steptime"))
                    {
                        relativity = MetricRelativity.LowerIsBetter;
                    }
                    else if (model.Name.Contains("throughput"))
                    {
                        relativity = MetricRelativity.HigherIsBetter;
                    }

                    Metric metric = new Metric(model.Name, ((double)model.Value), null, relativity);
                    this.Metrics.Add(metric);
                }
                catch
                {
                    // do nothing as this result file has non-double values.
                }
            }

            return this.Metrics;
        }
    }
}
