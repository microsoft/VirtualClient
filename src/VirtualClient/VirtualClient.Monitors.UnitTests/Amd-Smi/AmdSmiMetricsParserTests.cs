// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors.UnitTests.Amd_Smi
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Monitors.Amd_Smi;

    [TestFixture]
    [Category("Unit")]
    public class AmdSmiMetricsParserTests
    {
        [Test]
        public void AmdSmiMetricsParserTest()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "metrics.txt");
            string rawText = File.ReadAllText(outputPath);
            string gpuId = "0";

            AmdSmiMetricsParser testParser = new AmdSmiMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, $"GFX_ACTIVITY_GPU{gpuId}", 0, "%");
            MetricAssert.Exists(metrics, $"UMC_ACTIVITY_GPU{gpuId}", 0, "%");
            MetricAssert.Exists(metrics, $"MM_ACTIVITY_GPU{gpuId}", -1, ""); // N/A → -1
            MetricAssert.Exists(metrics, $"SOCKET_POWER_GPU{gpuId}", 137, "W");
            MetricAssert.Exists(metrics, $"GFX_VOLTAGE_GPU{gpuId}", -1, "V"); // N/A → -1
            MetricAssert.Exists(metrics, $"SOC_VOLTAGE_GPU{gpuId}", -1, "V"); // N/A → -1
            MetricAssert.Exists(metrics, $"MEM_VOLTAGE_GPU{gpuId}", -1, "V"); // N/A → -1
            MetricAssert.Exists(metrics, $"POWER_MANAGEMENT_GPU{gpuId}", -1, ""); // ENABLED → 1
            MetricAssert.Exists(metrics, $"TEMPERATURE_EDGE_GPU{gpuId}", -1, "C");
            MetricAssert.Exists(metrics, $"TEMPERATURE_HOTSPOT_GPU{gpuId}", 38, "C");
            MetricAssert.Exists(metrics, $"TEMPERATURE_MEM_GPU{gpuId}", 31, "C");
        }
    }
}