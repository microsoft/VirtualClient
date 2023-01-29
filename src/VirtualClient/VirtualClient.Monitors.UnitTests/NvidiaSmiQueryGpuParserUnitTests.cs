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
    public class NvidiaSmiQueryGpuParserUnitTests
    {
        [Test]
        public void NvidiaSmiQueryGpuParserParsesMetricsCorrectly_Scenario1xT4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-gpu-1xT4.csv");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryGpuParser testParser = new NvidiaSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(6, metrics.Count);
            MetricAssert.Exists(metrics, "temperature.gpu", 36, "celsuis");
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory [%]", 0, "%");
            MetricAssert.Exists(metrics, "memory.total [MiB]", 15360, "MiB");
            MetricAssert.Exists(metrics, "memory.free [MiB]", 14750, "MiB");
            MetricAssert.Exists(metrics, "memory.used [MiB]", 159, "MiB");
        }

        [Test]
        public void NvidiaSmiQueryGpuParserParsesMetricsCorrectly_Scenario4xT4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-gpu-4xT4.csv");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryGpuParser testParser = new NvidiaSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(24, metrics.Count);
            MetricAssert.Exists(metrics, "temperature.gpu", 48, "celsuis");
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory [%]", 0, "%");
            MetricAssert.Exists(metrics, "memory.total [MiB]", 16384, "MiB");
            MetricAssert.Exists(metrics, "memory.free [MiB]", 15670, "MiB");
            MetricAssert.Exists(metrics, "memory.used [MiB]", 257, "MiB");

            MetricAssert.Exists(metrics, "temperature.gpu", 51, "celsuis");
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory [%]", 0, "%");
            MetricAssert.Exists(metrics, "memory.total [MiB]", 16384, "MiB");
            MetricAssert.Exists(metrics, "memory.free [MiB]", 15913, "MiB");
            MetricAssert.Exists(metrics, "memory.used [MiB]", 14, "MiB");

            MetricAssert.Exists(metrics, "temperature.gpu", 53, "celsuis");
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory [%]", 0, "%");
            MetricAssert.Exists(metrics, "memory.total [MiB]", 16384, "MiB");
            MetricAssert.Exists(metrics, "memory.free [MiB]", 15913, "MiB");
            MetricAssert.Exists(metrics, "memory.used [MiB]", 14, "MiB");

            MetricAssert.Exists(metrics, "temperature.gpu", 53, "celsuis");
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory [%]", 0, "%");
            MetricAssert.Exists(metrics, "memory.total [MiB]", 16384, "MiB");
            MetricAssert.Exists(metrics, "memory.free [MiB]", 15913, "MiB");
            MetricAssert.Exists(metrics, "memory.used [MiB]", 14, "MiB");
        }
    }
}