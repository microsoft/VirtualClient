// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Renci.SshNet.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the SPECview workload.
    /// </summary>
    public class SpecViewMetricsParser : MetricsParser
    {
        private const string Unit = "fps";

        /// <summary>
        /// Constructor for <see cref="SpecViewMetricsParser"/>
        /// </summary>
        /// <param name="rawText">The raw text from the SPECview export process.</param>
        public SpecViewMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            IList<Metric> metrics = new List<Metric>();
            try
            {
                string[] lines = this.RawText.Split('\n');
                foreach (string line in lines)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] parts = line.Split(',');

                    // Check if the line is a composite row - only two items
                    if (parts.Length == 2 && double.TryParse(parts[1], out double value))
                    {
                        string name = parts[0];
                        Metric metric = new (name, value, SpecViewMetricsParser.Unit, MetricRelativity.HigherIsBetter);
                        metrics.Add(metric);
                    }
                }
            }
            catch (Exception exc)
            {
                throw new WorkloadException($"Results not found. The workload 'SpecView' did not produce any valid results.", exc, ErrorReason.WorkloadFailed);
            }

            return metrics;
        }
    }
}
