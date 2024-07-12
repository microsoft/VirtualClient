// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class AmdSmiMetricQueryGpuParserUnitTests
    {
        [Test]
        public void AmdSmiMetricQueryGpuParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "result1.txt");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiMetricQueryGpuParser testParser = new AmdSmiMetricQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(184, metrics.Count);
            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 36, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 30, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 133, "W");
            MetricAssert.Exists(metrics, "gfx_0_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_1_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_2_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_3_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_4_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_5_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_6_clk", 132, "MHz");
            MetricAssert.Exists(metrics, "gfx_7_clk", 133, "MHz");
            MetricAssert.Exists(metrics, "mem_0_clk", 900, "MHz");
            MetricAssert.Exists(metrics, "vclk_0_clk", 29, "MHz");
            MetricAssert.Exists(metrics, "vclk_1_clk", 29, "MHz");
            MetricAssert.Exists(metrics, "vclk_2_clk", 29, "MHz");
            MetricAssert.Exists(metrics, "vclk_3_clk", 29, "MHz");
            MetricAssert.Exists(metrics, "dclk_0_clk", 22, "MHz");
            MetricAssert.Exists(metrics, "dclk_1_clk", 22, "MHz");
            MetricAssert.Exists(metrics, "dclk_2_clk", 22, "MHz");
            MetricAssert.Exists(metrics, "dclk_3_clk", 22, "MHz");
            MetricAssert.Exists(metrics, "pcie_bw", 192, "Mb/s");

        }
    }
}