// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp.Framing;
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
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-gpu-1xH100.csv");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryGpuParser testParser = new NvidiaSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(31, metrics.Count);
            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);
        }

        [Test]
        public void NvidiaSmiQueryGpuParserParsesMetricsCorrectly_Scenario4xT4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-gpu-8xH100.csv");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryGpuParser testParser = new NvidiaSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(248, metrics.Count);
            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 34, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 71.71, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 72.05, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 25, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 33, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.78, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.97, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 25, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 33, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 72.17, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 71.44, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);

            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "temperature.memory", 35, "celsius");
            MetricAssert.Exists(metrics, "power.draw.average", 70.89, "W");
            MetricAssert.Exists(metrics, "clocks.gr", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 345, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 765, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 2619, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 81559, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 81007, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 70.68, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 5);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);
        }

        [Test]
        public void NvidiaSmiQueryGpuParserParsesMetricsCorrectlyWithNAValues_Scenario1xT4()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-gpu-1xT4-NA.csv");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryGpuParser testParser = new NvidiaSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(29, metrics.Count);
            MetricAssert.Exists(metrics, "utilization.gpu", 0, "%");
            MetricAssert.Exists(metrics, "utilization.memory", 0, "%");
            MetricAssert.Exists(metrics, "temperature.gpu", 26, "celsius");
            MetricAssert.Exists(metrics, "clocks.gr", 300, "MHz");
            MetricAssert.Exists(metrics, "clocks.sm", 300, "MHz");
            MetricAssert.Exists(metrics, "clocks.video", 540, "MHz");
            MetricAssert.Exists(metrics, "clocks.mem", 405, "MHz");
            MetricAssert.Exists(metrics, "memory.total", 15360, "MiB");
            MetricAssert.Exists(metrics, "memory.free", 14917, "MiB");
            MetricAssert.Exists(metrics, "memory.used", 0, "MiB");
            MetricAssert.Exists(metrics, "power.draw.instant", 9.31, "W");
            MetricAssert.Exists(metrics, "pcie.link.gen.gpucurrent", 1);
            MetricAssert.Exists(metrics, "pcie.link.width.current", 16);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.corrected.aggregate.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.volatile.total", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.device_memory", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.dram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.sram", 0);
            MetricAssert.Exists(metrics, "ecc.errors.uncorrected.aggregate.total", 0);
        }
    }
}