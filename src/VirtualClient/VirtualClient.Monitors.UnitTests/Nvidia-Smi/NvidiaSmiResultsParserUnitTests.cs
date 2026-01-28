// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors.UnitTests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NvidiaSmiResultsParserUnitTests
    {
        [Test]
        public void NvidiaSmiParserParsesExpectedMetricsFromC2CResults_Single_GPU()
        {
            string outputPath = Path.Combine(MockFixture.GetDirectory(typeof(NvidiaSmiResultsParserUnitTests), "Examples", "nvidia-smi"), "query-c2c.txt");
            string exampleResults = File.ReadAllText(outputPath);

            IList<Metric> metrics = NvidiaSmiResultsParser.ParseC2CResults(exampleResults);

            Assert.AreEqual(10, metrics.Count);
            MetricAssert.Exists(metrics, "gpu0_link0_speed", 44.712, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link1_speed", 44.713, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link2_speed", 44.714, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link3_speed", 44.715, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link4_speed", 44.716, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link5_speed", 44.717, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link6_speed", 44.718, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link7_speed", 44.719, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link8_speed", 44.720, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link9_speed", 44.721, MetricUnit.GigabytesPerSecond);
        }

        [Test]
        public void NvidiaSmiParserParsesExpectedMetricsFromC2CResults_Multiple_GPUs()
        {
            string outputPath = Path.Combine(MockFixture.GetDirectory(typeof(NvidiaSmiResultsParserUnitTests), "Examples", "nvidia-smi"), "query-c2c-multiple-gpu.txt");
            string exampleResults = File.ReadAllText(outputPath);

            IList<Metric> metrics = NvidiaSmiResultsParser.ParseC2CResults(exampleResults);

            Assert.AreEqual(20, metrics.Count);
            MetricAssert.Exists(metrics, "gpu0_link0_speed", 44.712, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link1_speed", 44.713, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link2_speed", 44.714, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link3_speed", 44.715, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu0_link4_speed", 44.716, MetricUnit.GigabytesPerSecond);

            MetricAssert.Exists(metrics, "gpu1_link0_speed", 44.717, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu1_link1_speed", 44.718, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu1_link2_speed", 44.719, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu1_link3_speed", 44.720, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu1_link4_speed", 44.721, MetricUnit.GigabytesPerSecond);

            MetricAssert.Exists(metrics, "gpu2_link0_speed", 44.722, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu2_link1_speed", 44.723, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu2_link2_speed", 44.724, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu2_link3_speed", 44.725, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu2_link4_speed", 44.726, MetricUnit.GigabytesPerSecond);

            MetricAssert.Exists(metrics, "gpu3_link0_speed", 44.727, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu3_link1_speed", 44.728, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu3_link2_speed", 44.729, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu3_link3_speed", 44.730, MetricUnit.GigabytesPerSecond);
            MetricAssert.Exists(metrics, "gpu3_link4_speed", 44.731, MetricUnit.GigabytesPerSecond);
        }

        [Test]
        public void NvidiaSmiParserParsesExpectedMetricsFromQueryResults_Scenario1xT4()
        {
            string outputPath = Path.Combine(MockFixture.GetDirectory(typeof(NvidiaSmiResultsParserUnitTests), "Examples", "nvidia-smi"), "query-gpu-1xH100.csv");
            string exampleResults = File.ReadAllText(outputPath);

            IList<Metric> metrics = NvidiaSmiResultsParser.ParseQueryResults(exampleResults);

            Assert.AreEqual(31, metrics.Count);
            Assert.IsTrue(metrics.All(m => int.TryParse(m.Metadata["gpu_index"]?.ToString(), out int index) && index == 0));

            MetricAssert.Exists(metrics, "gpu0_%_utilization", 1);
            MetricAssert.Exists(metrics, "gpu0_%_memory_utilization", 2);
            MetricAssert.Exists(metrics, "gpu0_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu0_memory_temperature", 35, "celsius");
            MetricAssert.Exists(metrics, "gpu0_power_draw_average", 70.89, "watts");
            MetricAssert.Exists(metrics, "gpu0_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_used", 3, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_instant_power_draw", 70.68, "watts");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_corrected_errors", 4, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_corrected_errors", 5, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_corrected_errors", 6, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_corrected_volatile_errors", 7, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_corrected_errors", 8, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_corrected_errors", 9, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_corrected_errors", 10, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_corrected_errors", 11, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_uncorrected_errors", 12, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_uncorrected_errors", 13, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_uncorrected_volatile_errors", 14, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_uncorrected_errors", 15, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_uncorrected_errors", 16, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_uncorrected_errors", 17, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_uncorrected_errors", 20, "count");
        }

        [Test]
        public void NvidiaSmiParserParsesExpectedMetricsFromQueryResults_Scenario1xT4_2()
        {
            string outputPath = Path.Combine(MockFixture.GetDirectory(typeof(NvidiaSmiResultsParserUnitTests), "Examples", "nvidia-smi"), "query-gpu-1xT4-NA.csv");
            string exampleResults = File.ReadAllText(outputPath);

            IList<Metric> metrics = NvidiaSmiResultsParser.ParseQueryResults(exampleResults);

            Assert.AreEqual(29, metrics.Count);
            Assert.IsTrue(metrics.All(m => int.TryParse(m.Metadata["gpu_index"]?.ToString(), out int index) && index == 0));

            MetricAssert.Exists(metrics, "gpu0_%_utilization", 2);
            MetricAssert.Exists(metrics, "gpu0_%_memory_utilization", 3);
            MetricAssert.Exists(metrics, "gpu0_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu0_graphics_clock_speed", 300, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_streaming_multiprocessor_clock_speed", 300, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_video_clock_speed", 540, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_clock_speed", 405, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_total", 15360, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_free", 14917, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_used", 100, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_instant_power_draw", 9.31, "watts");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_gen_current", 1, "amps");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_corrected_errors", 10, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_corrected_errors", 20, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_corrected_errors", 30, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_corrected_volatile_errors", 20, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_corrected_errors", 10, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_corrected_errors", 10, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_corrected_errors", 100, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_corrected_errors", 1, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_uncorrected_errors", 2, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_uncorrected_errors", 3, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_uncorrected_errors", 4, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_uncorrected_volatile_errors", 5, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_uncorrected_errors", 6, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_uncorrected_errors", 7, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_uncorrected_errors", 8, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_uncorrected_errors", 9, "count");


        }

        [Test]
        public void NvidiaSmiParserParsesExpectedMetricsFromQueryResults_Scenario4xT4()
        {
            string outputPath = Path.Combine(MockFixture.GetDirectory(typeof(NvidiaSmiResultsParserUnitTests), "Examples", "nvidia-smi"), "query-gpu-8xH100.csv");
            string exampleResults = File.ReadAllText(outputPath);

            IList<Metric> metrics = NvidiaSmiResultsParser.ParseQueryResults(exampleResults);

            Assert.AreEqual(248, metrics.Count);
            MetricAssert.Exists(metrics, "gpu0_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu0_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu0_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu0_memory_temperature", 35, "celsius");
            MetricAssert.Exists(metrics, "gpu0_power_draw_average", 70.89, "watts");
            MetricAssert.Exists(metrics, "gpu0_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu0_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu0_instant_power_draw", 70.68, "watts");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu0_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu0_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu1_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu1_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu1_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu1_memory_temperature", 34, "celsius");
            MetricAssert.Exists(metrics, "gpu1_power_draw_average", 71.71, "watts");
            MetricAssert.Exists(metrics, "gpu1_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu1_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu1_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu1_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu1_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu1_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu1_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu1_instant_power_draw", 72.05, "watts");
            MetricAssert.Exists(metrics, "gpu1_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu1_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu1_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu2_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu2_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu2_temperature", 25, "celsius");
            MetricAssert.Exists(metrics, "gpu2_memory_temperature", 33, "celsius");
            MetricAssert.Exists(metrics, "gpu2_power_draw_average", 70.78, "watts");
            MetricAssert.Exists(metrics, "gpu2_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu2_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu2_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu2_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu2_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu2_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu2_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu2_instant_power_draw", 70.97, "watts");
            MetricAssert.Exists(metrics, "gpu2_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu2_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu2_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu3_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu3_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu3_temperature", 25, "celsius");
            MetricAssert.Exists(metrics, "gpu3_memory_temperature", 33, "celsius");
            MetricAssert.Exists(metrics, "gpu3_power_draw_average", 72.17, "watts");
            MetricAssert.Exists(metrics, "gpu3_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu3_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu3_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu3_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu3_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu3_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu3_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu3_instant_power_draw", 71.44, "watts");
            MetricAssert.Exists(metrics, "gpu3_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu3_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu3_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu4_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu4_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu4_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu4_memory_temperature", 35, "celsius");
            MetricAssert.Exists(metrics, "gpu4_power_draw_average", 70.89, "watts");
            MetricAssert.Exists(metrics, "gpu4_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu4_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu4_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu4_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu4_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu4_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu4_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu4_instant_power_draw", 70.68, "watts");
            MetricAssert.Exists(metrics, "gpu4_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu4_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu4_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu5_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu5_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu5_temperature", 26, "celsius");
            MetricAssert.Exists(metrics, "gpu5_memory_temperature", 34, "celsius");
            MetricAssert.Exists(metrics, "gpu5_power_draw_average", 71.71, "watts");
            MetricAssert.Exists(metrics, "gpu5_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu5_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu5_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu5_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu5_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu5_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu5_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu5_instant_power_draw", 72.05, "watts");
            MetricAssert.Exists(metrics, "gpu5_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu5_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu5_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu6_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu6_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu6_temperature", 25, "celsius");
            MetricAssert.Exists(metrics, "gpu6_memory_temperature", 33, "celsius");
            MetricAssert.Exists(metrics, "gpu6_power_draw_average", 70.78, "watts");
            MetricAssert.Exists(metrics, "gpu6_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu6_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu6_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu6_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu6_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu6_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu6_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu6_instant_power_draw", 70.97, "watts");
            MetricAssert.Exists(metrics, "gpu6_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu6_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu6_ecc_aggregate_total_uncorrected_errors", 0, "count");

            MetricAssert.Exists(metrics, "gpu7_%_utilization", 0);
            MetricAssert.Exists(metrics, "gpu7_%_memory_utilization", 0);
            MetricAssert.Exists(metrics, "gpu7_temperature", 25, "celsius");
            MetricAssert.Exists(metrics, "gpu7_memory_temperature", 33, "celsius");
            MetricAssert.Exists(metrics, "gpu7_power_draw_average", 72.17, "watts");
            MetricAssert.Exists(metrics, "gpu7_graphics_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu7_streaming_multiprocessor_clock_speed", 345, "megahertz");
            MetricAssert.Exists(metrics, "gpu7_video_clock_speed", 765, "megahertz");
            MetricAssert.Exists(metrics, "gpu7_memory_clock_speed", 2619, "megahertz");
            MetricAssert.Exists(metrics, "gpu7_memory_total", 81559, "mebibytes");
            MetricAssert.Exists(metrics, "gpu7_memory_free", 81007, "mebibytes");
            MetricAssert.Exists(metrics, "gpu7_memory_used", 0, "mebibytes");
            MetricAssert.Exists(metrics, "gpu7_instant_power_draw", 71.44, "watts");
            MetricAssert.Exists(metrics, "gpu7_pcie_link_gen_current", 5, "amps");
            MetricAssert.Exists(metrics, "gpu7_pcie_link_width_current", 16, "amps");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_total_corrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_device_memory_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_dram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_sram_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_total_corrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_volatile_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_total_uncorrected_volatile_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_device_memory_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_dram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_sram_uncorrected_errors", 0, "count");
            MetricAssert.Exists(metrics, "gpu7_ecc_aggregate_total_uncorrected_errors", 0, "count");
        }
    }
}
