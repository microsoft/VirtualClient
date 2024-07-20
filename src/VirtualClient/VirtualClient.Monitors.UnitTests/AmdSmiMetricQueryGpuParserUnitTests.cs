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
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "result.txt");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiMetricQueryGpuParser testParser = new AmdSmiMetricQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, "utilization.gpu [%]", 98, "%");
            MetricAssert.Exists(metrics, "framebuffer.total [MB]", 14928, "MB");
            MetricAssert.Exists(metrics, "framebuffer.used [MB]", 363, "MB");

        }

        [Test]
        public void AmdSmiMetricQueryGpuParserParsesMetricsCorrectly_MI300X()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "result1.txt");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiMetricQueryGpuParser testParser = new AmdSmiMetricQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 36, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 30, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 133, "W");
            MetricAssert.Exists(metrics, "gfx_clk_avg", 132.125, "MHz");
            MetricAssert.Exists(metrics, "mem_clk", 900, "MHz");
            MetricAssert.Exists(metrics, "video_vclk_avg", 29, "MHz");
            MetricAssert.Exists(metrics, "video_dclk_avg", 22, "MHz");
            MetricAssert.Exists(metrics, "pcie_bw", 24, "MB/s");
        }
    }
}