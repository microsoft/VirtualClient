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
    public class ThreeDMarkMetricsParser : MetricsParser
    {
        /// <summary>
        /// Parser for the 3DMark workload
        /// </summary>
        /// <param name="rawText">The raw text from the 3DMark export process.</param>
        /// <param name="definition">The 3dmark definition.</param>
        public ThreeDMarkMetricsParser(string rawText, string definition)
            : base(rawText)
        {
            this.Defintion = definition;
        }

        private string Defintion { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            IList<Metric> metrics = new List<Metric>();
            try
            {
                if (this.Defintion == "custom_TSGT1.3dmdef")
                {
                    string pattern = "<TimeSpyPerformanceGraphicsTest1.*>((.|\\n)*?)<\\/TimeSpyPerformanceGraphicsTest1>";
                    Match m = Regex.Match(this.RawText, pattern);
                    XElement tag = XElement.Parse(m.Value);
                    double val = double.Parse(tag.Value);
                    metrics.Add(new Metric("timespy.graphics.1", val, "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Defintion == "custom_TSGT2.3dmdef")
                {
                    string pattern = "<TimeSpyPerformanceGraphicsTest2.*>((.|\\n)*?)<\\/TimeSpyPerformanceGraphicsTest2>";
                    Match m = Regex.Match(this.RawText, pattern);
                    XElement tag = XElement.Parse(m.Value);
                    double val = double.Parse(tag.Value);
                    metrics.Add(new Metric("timespy.graphics.2", val, "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Defintion == "custom_TSCT.3dmdef")
                {
                    string pattern = "<TimeSpyPerformanceCpuSection2.*>((.|\\n)*?)<\\/TimeSpyPerformanceCpuSection2>";
                    Match m = Regex.Match(this.RawText, pattern);
                    XElement tag = XElement.Parse(m.Value);
                    double val = double.Parse(tag.Value);
                    metrics.Add(new Metric("timespy.cpu", val, "fps", MetricRelativity.HigherIsBetter));
                }
            }
            catch (Exception exc)
            {
                throw new WorkloadException($"Results not found. The workload '3DMark' did not produce any valid results.", exc, ErrorReason.WorkloadFailed);
            }

            return metrics;
        }
    }
}
