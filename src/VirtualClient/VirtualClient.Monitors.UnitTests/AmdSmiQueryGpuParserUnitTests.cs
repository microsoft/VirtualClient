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
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "metric.csv");
            string rawText = File.ReadAllText(outputPath);
            AmdSmiMetricQueryGpuParser testParser = new AmdSmiMetricQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            string gpuId = "0"; // Assume GPU ID for testing, can be dynamically extracted from parsed data
            MetricAssert.Exists(metrics, $"utilization.gpu [%] (GPU {gpuId})", 98, "%");
            MetricAssert.Exists(metrics, $"framebuffer.total [MB] (GPU {gpuId})", 14928, "MB");
            MetricAssert.Exists(metrics, $"framebuffer.used [MB] (GPU {gpuId})", 363, "MB");
        }

        [Test]
        public void AmdSmiMetricQueryGpuParserParsesMetricsCorrectly_MI300X()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "metric-8xMI300X.csv");
            string rawText = File.ReadAllText(outputPath);
            AmdSmiMetricQueryGpuParser testParser = new AmdSmiMetricQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            string gpuId = "0"; // Assume GPU ID for testing
            MetricAssert.Exists(metrics, $"utilization.gpu (GPU {gpuId})", 0, "%");
            MetricAssert.Exists(metrics, $"utilization.memory (GPU {gpuId})", 0, "%");
            MetricAssert.Exists(metrics, $"temperature.gpu (GPU {gpuId})", 36, "celsius");
            MetricAssert.Exists(metrics, $"temperature.memory (GPU {gpuId})", 30, "celsius");
            MetricAssert.Exists(metrics, $"power.draw.average (GPU {gpuId})", 133, "W");
            MetricAssert.Exists(metrics, $"gfx_clk_avg (GPU {gpuId})", 132.125, "MHz");
            MetricAssert.Exists(metrics, $"mem_clk (GPU {gpuId})", 900, "MHz");
            MetricAssert.Exists(metrics, $"video_vclk_avg (GPU {gpuId})", 29, "MHz");
            MetricAssert.Exists(metrics, $"video_dclk_avg (GPU {gpuId})", 22, "MHz");
            MetricAssert.Exists(metrics, $"pcie_bw (GPU {gpuId})", 24, "MB/s");
        }
    }
}
