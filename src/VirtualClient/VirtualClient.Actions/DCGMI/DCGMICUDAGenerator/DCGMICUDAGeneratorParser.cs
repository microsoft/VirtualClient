// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for DCGMICUDAGenerator output document.
    /// </summary>
    public class DCGMICUDAGeneratorParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="DCGMICUDAGeneratorParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMICUDAGeneratorParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            double metricValue = 0;

            var jsonDocument = JsonDocument.Parse(this.PreprocessedText);

            // Get the "Overall Health" value
            string overallHealthValue = jsonDocument.RootElement
                .GetProperty("body")
                .GetProperty("Overall Health")
                .GetProperty("value")
                .GetString();

            // Get the "Health Monitor Report" header
            string headerValue = jsonDocument.RootElement
                .GetProperty("header")
                .EnumerateArray()
                .First()
                .GetString();
            if (overallHealthValue == "Healthy")
            {
                metricValue = 1;
            }
            else
            {
                metricValue = 0;
            }

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { "overallHealthValue", overallHealthValue },
                             { "headerValue", headerValue }
                         };
            this.Metrics.Add(new Metric(headerValue + "_" + "overallHealthValue", metricValue, metadata: metadata));

            return this.Metrics;
        }
    }
}
