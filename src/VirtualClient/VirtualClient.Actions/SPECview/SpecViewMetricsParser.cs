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
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the NTttcp workload.
    /// </summary>
    public class SpecViewMetricsParser : MetricsParser
    {
        /// <summary>
        /// Parser for the 3DMark workload
        /// </summary>
        /// <param name="rawText">The raw text from the 3DMark export process.</param>
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
            }
            catch (Exception exc)
            {
                throw new WorkloadException($"Results not found. The workload '3DMark' did not produce any valid results.", exc, ErrorReason.WorkloadFailed);
            }

            return metrics;
        }

        /// <summary>
        /// Gets the value sandwiched between the given XML tags
        /// </summary>
        /// <param name="tagName"></param>
        /// <returns></returns>
        private double ParseXMLTag(string tagName)
        {
            string pattern = $"<{tagName}.*>((.|\\n)*?)<\\/{tagName}>";
            Match m = Regex.Match(this.RawText, pattern);
            XElement tag = XElement.Parse(m.Value);
            double val = double.Parse(tag.Value);
            return val;
        }
    }
}
