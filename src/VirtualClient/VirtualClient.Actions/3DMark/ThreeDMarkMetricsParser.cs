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
                    metrics.Add(new Metric("graphics1", this.ParseXMLTag("TimeSpyCustomGraphicsTest1"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("graphics2", this.ParseXMLTag("TimeSpyCustomGraphicsTest2"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("cpu2", this.ParseXMLTag("TimeSpyCustomCpuSection2"), "fps", MetricRelativity.HigherIsBetter));

                    // aggregate scores
                    double cpuScore = this.ParseXMLTag("TimeSpyCustomCPUScore");
                    double graphicsScore = this.ParseXMLTag("TimeSpyCustomGraphicsScore");
                    double threeDMarkScore = this.CalculateTimeSpyAggregates(cpuScore, graphicsScore);
                    metrics.Add(new Metric("graphicsScore", graphicsScore, "score", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("cpuScore", cpuScore, "score", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("3dMarkScore", threeDMarkScore, "score", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "timespy_extreme")
                {
                    double cpuScore = this.ParseXMLTag("TimeSpyExtremeCustomCPUScore");
                    double graphicsScore = this.ParseXMLTag("TimeSpyExtremeCustomGraphicsScore");
                    double threeDMarkScore = this.CalculateTimeSpyAggregates(cpuScore, graphicsScore);
                    metrics.Add(new Metric("graphicsScore", graphicsScore, "score", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("cpuScore", cpuScore, "score", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("3dMarkScore", threeDMarkScore, "score", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "pciexpress")
                {
                    metrics.Add(new Metric("pciebandwidth", this.ParseXMLTag("PciExpressBandwidthCustom"), "GB/s", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("pciefps", this.ParseXMLTag("PciExpressFpsCustom"), "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "directxraytracing")
                {
                    metrics.Add(new Metric("featureTestPerformance", this.ParseXMLTag("DirectxRaytracingFtFpsPerformance"), "fps", MetricRelativity.HigherIsBetter));
                }
                else if (this.Benchmark.ToLower() == "portroyal")
                {
                    metrics.Add(new Metric("portroyal.gpu [fps]", this.ParseXMLTag("PortRoyalCustomGraphicsTest1"), "fps", MetricRelativity.HigherIsBetter));
                    metrics.Add(new Metric("portroyal.finalscore", this.ParseXMLTag("PortRoyalCustomGraphicsScore"), "score", MetricRelativity.HigherIsBetter));
                }
            }
            catch (Exception exc)
            {
                throw new SchemaException($"The 3DMark output file has incorrect format for parsing.", exc);
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

            if (val == 0)
            {
                throw new SchemaException($"3dMark tested 0 for {tagName}.");
            }

            return val;
        }

        /// <summary>
        /// Calculates the 3DMark TimeSpy Score
        /// </summary>
        private double CalculateTimeSpyAggregates(double cpuScore, double graphicsScore)
        {
            return 1 / ((0.85 / graphicsScore) + (0.15 / cpuScore));
        }
    }
}