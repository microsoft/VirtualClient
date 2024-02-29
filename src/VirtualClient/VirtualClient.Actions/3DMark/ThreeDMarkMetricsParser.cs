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
        /// <param name="benchmark">The 3dmark benchmark name.</param>
        public ThreeDMarkMetricsParser(string rawText, string benchmark)
            : base(rawText)
        {
            this.Benchmark = benchmark;
        }

        private string Benchmark { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            IList<Metric> metrics = new List<Metric>();

            try
            {
                if (this.Benchmark.ToLower() == "timespy")
                {
                    metrics.Add(new Metric("timespy.graphics.1 [fps]", this.ParseXMLTag("TimeSpyPerformanceGraphicsTest1"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("timespy.graphics.2 [fps]", this.ParseXMLTag("TimeSpyPerformanceGraphicsTest2"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("timespy.cpu [fps]", this.ParseXMLTag("TimeSpyPerformanceCpuSection2"), "fps", MetricRelativity.HigherIsBetter));
                    foreach (Metric metric in this.CalculateTimeSpyAggregates(metrics))
                    {
                        metrics.Add(metric);
                    }
                }
                else if (this.Benchmark.ToLower() == "timespyextreme")
                {
                    metrics.Add(new Metric("timespyextreme.graphics.1 [fps]", this.ParseXMLTag("TimeSpyExtremeGraphicsTest1"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("timespyextreme.graphics.2 [fps]", this.ParseXMLTag("TimeSpyExtremeGraphicsTest2"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("timespyextreme.cpu [fps]", this.ParseXMLTag("TimeSpyExtremeCpuSection0"), "fps", MetricRelativity.HigherIsBetter));
                    foreach (Metric metric in this.CalculateTimeSpyAggregates(metrics))
                    {
                        metrics.Add(metric);
                    }
                }
                else if (this.Benchmark.ToLower() == "pciexpress")
                {
                    metrics.Add(new Metric("pciebandwidth.gpu [GB/s]", this.ParseXMLTag("PciExpressBandwidthPerformance"), "GB/s", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("pcie.gpu [fps]", this.ParseXMLTag("PciExpressFpsPerformance"), "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "dxraytracing")
                {
                    metrics.Add(new Metric("dxraytracing.gpu [fps]", this.ParseXMLTag("DirectxRaytracingFtFpsPerformance"), "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "portroyal")
                {
                    metrics.Add(new Metric("portroyal.gpu [fps]", this.ParseXMLTag("PortRoyalCustomGraphicsTest1"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("portroyal.finalscore", this.ParseXMLTag("PortRoyalCustomGraphicsScore"), "score", MetricRelativity.HigherIsBetter));
                }
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

        /// <summary>
        /// Calculates the 3DMark TimeSpy aggregate scores
        /// </summary>
        private IList<Metric> CalculateTimeSpyAggregates(IList<Metric> metrics)
        {
            IList<Metric> aggregates = new List<Metric>();
            double tsgt1 = 0;
            double tsgt2 = 0;
            double tsct = 0;
            foreach (Metric metric in metrics)
            {
                if (metric.Name == "timespy.graphics.1 [fps]" || metric.Name == "timespyextreme.graphics.1 [fps]")
                {
                    tsgt1 = metric.Value;
                }
                else if (metric.Name == "timespy.graphics.2 [fps]" || metric.Name == "timespyextreme.graphics.2 [fps]")
                {
                    tsgt2 = metric.Value;
                }
                else if (metric.Name == "timespy.cpu [fps]" || metric.Name == "timespyextreme.cpu [fps]")
                {
                    tsct = metric.Value;
                }
            }

            // Weighted Harmonic Mean of Individual Scores
            if (tsgt1 != 0 && tsgt2 != 0 && tsct != 0)
            {
                double graphicsScore = 165 * (2 / ((1 / tsgt1) + (1 / tsgt2)));
                double cpuScore = 298 * tsct;
                double aggScore = 1 / ((0.85 / graphicsScore) + (0.15 / cpuScore));
                aggregates.Add(new Metric("timespy.graphics.agg", graphicsScore, "score", MetricRelativity.HigherIsBetter));
                aggregates.Add(new Metric("timespy.cpu.agg", cpuScore, "score", MetricRelativity.HigherIsBetter));
                aggregates.Add(new Metric("timespy.finalscore", aggScore, "score", MetricRelativity.HigherIsBetter));
            }

            return aggregates;
        }
    }
}