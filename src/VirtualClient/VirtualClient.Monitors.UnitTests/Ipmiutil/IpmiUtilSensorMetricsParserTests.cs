// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class IpmiUtilSensorMetricsParserTests : MockFixture
    {
        private MockFixture mockFixture;
        private string examplesDirectory;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.examplesDirectory = MockFixture.GetDirectory(typeof(IpmiUtilSensorMetricsParserTests), "test_examples", "ipmiutil");
        }

        [Test]
        public void IpmiUtilSensorParserVerifyMetricsExample1()
        {
            string outputPath = this.mockFixture.Combine(this.examplesDirectory, "ipmiutil_sensor_example_1.txt");
            string text = System.IO.File.ReadAllText(outputPath);
            IpmiUtilSensorMetricsParser testParser = new IpmiUtilSensorMetricsParser();
            IList<Metric> metrics = testParser.Parse(text);

            Assert.AreEqual(103, metrics.Count);
            MetricAssert.Exists(metrics, "CX7_0_Temp", 44, "celcius");
            MetricAssert.Exists(metrics, "CX7_1_Temp", 42, "celcius");
            MetricAssert.Exists(metrics, "CX7_2_Temp", 41, "celcius");
            MetricAssert.Exists(metrics, "CX7_3_Temp", 40, "celcius");
            MetricAssert.Exists(metrics, "E1S_1_TMP", 27, "celcius");
            MetricAssert.Exists(metrics, "E1S_2_TMP", 26, "celcius");
            MetricAssert.Exists(metrics, "E1S_3_TMP", 27, "celcius");
            MetricAssert.Exists(metrics, "E1S_4_TMP", 26, "celcius");
            MetricAssert.Exists(metrics, "E1S_5_TMP", 27, "celcius");
            MetricAssert.Exists(metrics, "E1S_TMP_MAX", 27, "celcius");
            MetricAssert.Exists(metrics, "FAN_0A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_0B", 20655, "rpm");
            MetricAssert.Exists(metrics, "FAN_1A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_1B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_2A", 21756, "rpm");
            MetricAssert.Exists(metrics, "FAN_2B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_3A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_3B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_4A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_4B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_5A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_5B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_6A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_6B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FAN_7A", 21904, "rpm");
            MetricAssert.Exists(metrics, "FAN_7B", 20385, "rpm");
            MetricAssert.Exists(metrics, "FANS_SWB_PWM", 66, "percentage");
            MetricAssert.Exists(metrics, "GPU_DRAM_PWR_SUM", 100, "watts");
            MetricAssert.Exists(metrics, "GPU_DRAM_TMP_MAX", 37, "celcius");
            MetricAssert.Exists(metrics, "GPU_PWR_SUM", 880, "watts");
            MetricAssert.Exists(metrics, "GPU_TLIMIT_MIN", 55, "celcius");
            MetricAssert.Exists(metrics, "GPU0_DRAM0_PWR0", 25, "watts");
            MetricAssert.Exists(metrics, "GPU0_DRAM0_TMP0", 37, "celcius");
            MetricAssert.Exists(metrics, "GPU0_PWR0", 215, "watts");
            MetricAssert.Exists(metrics, "GPU0_TLIMIT_TMP1", 55, "celcius");
            MetricAssert.Exists(metrics, "GPU1_DRAM0_PWR0", 25, "watts");
            MetricAssert.Exists(metrics, "GPU1_DRAM0_TMP0", 35, "celcius");
            MetricAssert.Exists(metrics, "GPU1_PWR0", 240, "watts");
            MetricAssert.Exists(metrics, "GPU1_TLIMIT_TMP1", 55, "celcius");
            MetricAssert.Exists(metrics, "GPU2_DRAM0_PWR0", 20, "watts");
            MetricAssert.Exists(metrics, "GPU2_DRAM0_TMP0", 36, "celcius");
            MetricAssert.Exists(metrics, "GPU2_PWR0", 220, "watts");
            MetricAssert.Exists(metrics, "GPU2_TLIMIT_TMP1", 55, "celcius");
            MetricAssert.Exists(metrics, "GPU3_DRAM0_PWR0", 20, "watts");
            MetricAssert.Exists(metrics, "GPU3_DRAM0_TMP0", 35, "celcius");
            MetricAssert.Exists(metrics, "GPU3_PWR0", 210, "watts");
            MetricAssert.Exists(metrics, "GPU3_TLIMIT_TMP1", 56, "celcius");
            MetricAssert.Exists(metrics, "HMC_TMP", 35, "celcius");
            MetricAssert.Exists(metrics, "HSC_PWR_SUM", 1342, "watts");
            MetricAssert.Exists(metrics, "HSC0_INPUT_PWR", 115.36, "watts");
            MetricAssert.Exists(metrics, "HSC0_INPUT_VOLT", 51.03, "volts");
            MetricAssert.Exists(metrics, "HSC0_OUTPUT_CUR", 2.21, "amps");
            MetricAssert.Exists(metrics, "HSC0_TMP", 29, "celcius");
            MetricAssert.Exists(metrics, "HSC1_INPUT_PWR", 594, "watts");
            MetricAssert.Exists(metrics, "HSC1_INPUT_VOLT", 51.03, "volts");
            MetricAssert.Exists(metrics, "HSC1_OUTPUT_CUR", 12.35, "amps");
            MetricAssert.Exists(metrics, "HSC1_TMP", 24, "celcius");
            MetricAssert.Exists(metrics, "HSC2_INPUT_PWR", 594, "watts");
            MetricAssert.Exists(metrics, "HSC2_INPUT_VOLT", 51.34, "volts");
            MetricAssert.Exists(metrics, "HSC2_OUTPUT_CUR", 11.4, "amps");
            MetricAssert.Exists(metrics, "HSC2_TMP", 29, "celcius");
            MetricAssert.Exists(metrics, "LEAK_L_COLDPLATE", 1.74, "volts");
            MetricAssert.Exists(metrics, "LEAK_L_MANIFOLD", 1.74, "volts");
            MetricAssert.Exists(metrics, "LEAK_R_COLDPLATE", 1.74, "volts");
            MetricAssert.Exists(metrics, "LEAK_R_MANIFOLD", 1.73, "volts");
            MetricAssert.Exists(metrics, "M0_CPU_PWR0", 92, "watts");
            MetricAssert.Exists(metrics, "M0_CPU_TMPAVG0", 35, "celcius");
            MetricAssert.Exists(metrics, "M0_CPU_TMPLIMIT0", 52, "celcius");
            MetricAssert.Exists(metrics, "M0_VREG_CPUPWR0", 44, "watts");
            MetricAssert.Exists(metrics, "M0_VREG_CPUVOLT0", 0.8, "volts");
            MetricAssert.Exists(metrics, "M0_VREG_SOCPWR0", 6.16, "watts");
            MetricAssert.Exists(metrics, "M0_VREG_SOCVOLT0", 0.8, "volts");
            MetricAssert.Exists(metrics, "M1_CPU_PWR0", 90, "watts");
            MetricAssert.Exists(metrics, "M1_CPU_TMPAVG0", 35, "celcius");
            MetricAssert.Exists(metrics, "M1_CPU_TMPLIMIT0", 52, "celcius");
            MetricAssert.Exists(metrics, "M1_VREG_CPUPWR0", 42, "watts");
            MetricAssert.Exists(metrics, "M1_VREG_CPUVOLT0", 0.8, "volts");
            MetricAssert.Exists(metrics, "M1_VREG_SOCPWR0", 5.6, "watts");
            MetricAssert.Exists(metrics, "M1_VREG_SOCVOLT0", 0.8, "volts");
            MetricAssert.Exists(metrics, "M2_TMP", 30, "celcius");
            MetricAssert.Exists(metrics, "OL_S1_AMB_TMP", 38.5, "celcius");
            MetricAssert.Exists(metrics, "OL_S1_FPGA_TMP", 45, "celcius");
            MetricAssert.Exists(metrics, "OL_S1_HSC_TMP", 22.5, "celcius");
            MetricAssert.Exists(metrics, "OL_S1_POWER", 63.13, "watts");
            MetricAssert.Exists(metrics, "OL_S1_SOC_TMP", 48, "celcius");
            MetricAssert.Exists(metrics, "P12V_CB1_CUR", 33, "amps");
            MetricAssert.Exists(metrics, "P12V_CB1_TMP", 33, "celcius");
            MetricAssert.Exists(metrics, "P12V_CB1_VOLT", 11.96, "volts");
            MetricAssert.Exists(metrics, "P12V_CB2_CUR", 30, "amps");
            MetricAssert.Exists(metrics, "P12V_CB2_TMP", 36, "celcius");
            MetricAssert.Exists(metrics, "P12V_CB2_VOLT", 11.96, "volts");
            MetricAssert.Exists(metrics, "P12V_SCM_VOLT", 12.13, "volts");
            MetricAssert.Exists(metrics, "P12V_STBY_CUR", 6, "amps");
            MetricAssert.Exists(metrics, "P12V_STBY_TMP", 36, "celcius");
            MetricAssert.Exists(metrics, "P12V_STBY_VOLT", 11.96, "volts");
            MetricAssert.Exists(metrics, "P3V_BAT_VOLT", 3.18, "volts");
            MetricAssert.Exists(metrics, "P3V3_SCM_VOLT", 3.31, "volts");
            MetricAssert.Exists(metrics, "P5V_SCM_VOLT", 5.05, "volts");
            MetricAssert.Exists(metrics, "PCIESW_DIE_TEMP", 37, "celcius");
            MetricAssert.Exists(metrics, "PDB_OUTLET_TMP", 33.5, "celcius");
            MetricAssert.Exists(metrics, "SBP_INLET_TMP", 26.5, "celcius");
            MetricAssert.Exists(metrics, "SCM_INLET_TMP", 24, "celcius");
            MetricAssert.Exists(metrics, "SWB_INLET_TMP", 27.5, "celcius");
        }

        [Test]
        public void IpmiUtilSensorParserVerifyMetricsExample2()
        {
            string outputPath = this.mockFixture.Combine(this.examplesDirectory, "ipmiutil_sensor_example_2.txt");
            string text = System.IO.File.ReadAllText(outputPath);
            IpmiUtilSensorMetricsParser testParser = new IpmiUtilSensorMetricsParser();
            IList<Metric> metrics = testParser.Parse(text);

            Assert.AreEqual(122, metrics.Count); // there are 122 SDRType = Full records in this example

            // Verify all 122 metrics
            var okTag = new List<string>() { "OK" };
            var initTag = new List<string>() { "Init" };
            
            MetricAssert.Exists(metrics, "FANS_SWB_PWM", 26, "percentage", okTag);
            MetricAssert.Exists(metrics, "P12V_SCM_VOLT", 12.15, "volts", okTag);
            MetricAssert.Exists(metrics, "P5V_SCM_VOLT", 5.07, "volts", okTag);
            MetricAssert.Exists(metrics, "P3V3_SCM_VOLT", 3.32, "volts", okTag);
            MetricAssert.Exists(metrics, "P3V_BAT_VOLT", 3.34, "volts", okTag);
            MetricAssert.Exists(metrics, "SCM_INLET_TMP", 26.5, "celcius", okTag);
            MetricAssert.Exists(metrics, "SWB_INLET_TMP", 31, "celcius", okTag);
            MetricAssert.Exists(metrics, "SBP_INLET_TMP", 31, "celcius", okTag);
            MetricAssert.Exists(metrics, "M2_TMP", 42, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_1_TMP", 33, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_2_TMP", 32, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_3_TMP", 33, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_4_TMP", 33, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_5_TMP", 32, "celcius", okTag);
            MetricAssert.Exists(metrics, "E1S_TMP_MAX", 33, "celcius", okTag);
            MetricAssert.Exists(metrics, "PDB_OUTLET_TMP", 42, "celcius", okTag);
            MetricAssert.Exists(metrics, "HSC0_INPUT_PWR", 120, "watts", okTag);
            MetricAssert.Exists(metrics, "HSC0_INPUT_VOLT", 51.34, "volts", okTag);
            MetricAssert.Exists(metrics, "HSC0_OUTPUT_CUR", 2.2, "amps", okTag);
            MetricAssert.Exists(metrics, "HSC0_TMP", 37, "celcius", okTag);
            MetricAssert.Exists(metrics, "HSC1_INPUT_PWR", 550, "watts", okTag);
            MetricAssert.Exists(metrics, "HSC1_INPUT_VOLT", 51.34, "volts", okTag);
            MetricAssert.Exists(metrics, "HSC1_OUTPUT_CUR", 11, "amps", okTag);
            MetricAssert.Exists(metrics, "HSC1_TMP", 33, "celcius", okTag);
            MetricAssert.Exists(metrics, "HSC2_INPUT_PWR", 550, "watts", okTag);
            MetricAssert.Exists(metrics, "HSC2_INPUT_VOLT", 51.03, "volts", okTag);
            MetricAssert.Exists(metrics, "HSC2_OUTPUT_CUR", 9.9, "amps", okTag);
            MetricAssert.Exists(metrics, "HSC2_TMP", 38, "celcius", okTag);
            MetricAssert.Exists(metrics, "HSC_PWR_SUM", 1236, "watts", okTag);
            MetricAssert.Exists(metrics, "P12V_STBY_VOLT", 11.96, "volts", okTag);
            MetricAssert.Exists(metrics, "P12V_STBY_CUR", 8.5, "amps", okTag);
            MetricAssert.Exists(metrics, "P12V_STBY_TMP", 46, "celcius", okTag);
            MetricAssert.Exists(metrics, "P12V_CB1_VOLT", 12.51, "volts", okTag);
            MetricAssert.Exists(metrics, "P12V_CB1_CUR", 45, "amps", okTag);
            MetricAssert.Exists(metrics, "P12V_CB1_TMP", 43, "celcius", okTag);
            MetricAssert.Exists(metrics, "P12V_CB2_VOLT", 12.51, "volts", okTag);
            MetricAssert.Exists(metrics, "P12V_CB2_CUR", 37.5, "amps", okTag);
            MetricAssert.Exists(metrics, "P12V_CB2_TMP", 46, "celcius", okTag);
            MetricAssert.Exists(metrics, "FAN_0A", 8436, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_0B", 7290, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_1A", 8288, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_1B", 7290, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_2A", 8288, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_2B", 7155, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_3A", 8288, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_3B", 7155, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_4A", 8140, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_4B", 7290, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_5A", 8140, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_5B", 7155, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_6A", 8288, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_6B", 7155, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_7A", 8288, "rpm", okTag);
            MetricAssert.Exists(metrics, "FAN_7B", 7155, "rpm", okTag);
            MetricAssert.Exists(metrics, "GPU0_DRAM0_TMP0", 37, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU1_DRAM0_TMP0", 37, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU2_DRAM0_TMP0", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU3_DRAM0_TMP0", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU_DRAM_TMP_MAX", 37, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU0_PWR0", 180, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU1_PWR0", 185, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU2_PWR0", 190, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU3_PWR0", 185, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU_PWR_SUM", 740, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU0_TLIMIT_TMP1", 50, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU1_TLIMIT_TMP1", 50, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU2_TLIMIT_TMP1", 50, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU3_TLIMIT_TMP1", 51, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU_TLIMIT_MIN", 50, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU0_DRAM0_PWR0", 35, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU1_DRAM0_PWR0", 35, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU2_DRAM0_PWR0", 35, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU3_DRAM0_PWR0", 30, "watts", okTag);
            MetricAssert.Exists(metrics, "GPU_DRAM_PWR_SUM", 140, "watts", okTag);
            MetricAssert.Exists(metrics, "M0_CPU_PWR0", 98, "watts", okTag);
            MetricAssert.Exists(metrics, "M0_VREG_CPUPWR0", 46.8, "watts", okTag);
            MetricAssert.Exists(metrics, "M0_VREG_SOCPWR0", 6.6, "watts", okTag);
            MetricAssert.Exists(metrics, "M0_CPU_TMPAVG0", 41, "celcius", okTag);
            MetricAssert.Exists(metrics, "M0_CPU_TMPLIMIT0", 47, "celcius", okTag);
            MetricAssert.Exists(metrics, "M0_VREG_CPUVOLT0", 0.8, "volts", okTag);
            MetricAssert.Exists(metrics, "M0_VREG_SOCVOLT0", 0.8, "volts", okTag);
            MetricAssert.Exists(metrics, "M1_CPU_PWR0", 96, "watts", okTag);
            MetricAssert.Exists(metrics, "M1_VREG_CPUPWR0", 45.6, "watts", okTag);
            MetricAssert.Exists(metrics, "M1_VREG_SOCPWR0", 6, "watts", okTag);
            MetricAssert.Exists(metrics, "M1_CPU_TMPAVG0", 41, "celcius", okTag);
            MetricAssert.Exists(metrics, "M1_CPU_TMPLIMIT0", 47, "celcius", okTag);
            MetricAssert.Exists(metrics, "M1_VREG_CPUVOLT0", 0.8, "volts", okTag);
            MetricAssert.Exists(metrics, "M1_VREG_SOCVOLT0", 0.8, "volts", okTag);
            MetricAssert.Exists(metrics, "OL_S1_HSC_TMP", 32, "celcius", okTag);
            MetricAssert.Exists(metrics, "OL_S1_FPGA_TMP", 76, "celcius", okTag);
            MetricAssert.Exists(metrics, "OL_S1_AMB_TMP", 64, "celcius", okTag);
            MetricAssert.Exists(metrics, "OL_S1_SOC_TMP", 88, "celcius", okTag);
            MetricAssert.Exists(metrics, "OL_S1_POWER", 70.8, "watts", okTag);
            MetricAssert.Exists(metrics, "PCIESW_DIE_TEMP", 44, "celcius", okTag);
            MetricAssert.Exists(metrics, "HMC_TMP", 39, "celcius", okTag);
            MetricAssert.Exists(metrics, "LEAK_L_COLDPLATE", 1.72, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_L_MANIFOLD", 1.73, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_R_COLDPLATE", 1.73, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_R_MANIFOLD", 1.7, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_L_TRAY", 2.27, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_R_TRAY", 2.23, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_Reserved_0", 3.29, "volts", okTag);
            MetricAssert.Exists(metrics, "LEAK_Reserved_1", 3.26, "volts", okTag);
            MetricAssert.Exists(metrics, "CX8_0_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_1_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_2_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_3_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_0_OSFP_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_1_OSFP_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_2_OSFP_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "CX8_3_OSFP_Temp", 0, "celcius", initTag);
            MetricAssert.Exists(metrics, "GPU0_AVG_TMP", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU1_AVG_TMP", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU2_AVG_TMP", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU3_AVG_TMP", 35, "celcius", okTag);
            MetricAssert.Exists(metrics, "GPU_AVG_TMP_MAX", 36, "celcius", okTag);
            MetricAssert.Exists(metrics, "M0_inlet_Temp0", 38, "celcius", okTag);
            MetricAssert.Exists(metrics, "M0_inlet_Temp1", 46, "celcius", okTag);
            MetricAssert.Exists(metrics, "M0_Exhaust_Temp0", 45, "celcius", okTag);
            MetricAssert.Exists(metrics, "M1_inlet_Temp0", 43, "celcius", okTag);
            MetricAssert.Exists(metrics, "M1_inlet_Temp1", 47, "celcius", okTag);
            MetricAssert.Exists(metrics, "M1_Exhaust_Temp0", 43, "celcius", okTag);


            var descriptions = metrics.Select(x => x.Description);
            Assert.AreEqual(122, descriptions.Distinct().Count(), "All metrics should have unique descriptions");

            Assert.IsTrue(descriptions.Contains("003a | Full    | Fan             | 3a | FAN_0A | OK   | 8436.00 RPM"));
            Assert.IsTrue(descriptions.Contains("0001 | Full    | Fan             | 01 | FANS_SWB_PWM | OK   | 26.00 %"));
            Assert.IsTrue(descriptions.Contains("005a | Full    | Temperature     | 5a | GPU0_DRAM0_TMP0 | OK   | 37.00 C"));
            Assert.IsTrue(descriptions.Contains("001e | Full    | Temperature     | 1e | PDB_OUTLET_TMP | OK   | 42.00 C"));

            foreach (Metric metric in metrics)
            {
                var arr = metric.Description.Split('|');

                Assert.AreEqual(metric.Metadata["id"], arr[0].Trim());
                Assert.AreEqual(metric.Metadata["sdrType"], arr[1].Trim());
                Assert.AreEqual(metric.Metadata["type"], arr[2].Trim());
                Assert.AreEqual(metric.Metadata["snum"], arr[3].Trim());
                Assert.AreEqual(metric.Metadata["name"], arr[4].Trim());
                Assert.AreEqual(metric.Metadata["status"], arr[5].Trim());
            }
        }

        [Test]
        public void IpmiUtilSensorParserVerifyInvalidEntriesExample3()
        {
            string outputPath = this.mockFixture.Combine(this.examplesDirectory, "ipmiutil_sensor_example_3.txt");
            string text = System.IO.File.ReadAllText(outputPath);
            IpmiUtilSensorMetricsParser testParser = new IpmiUtilSensorMetricsParser();
            IList<Metric> metrics = testParser.Parse(text);

            // Verify that metrics with valid numerical readings were parsed correctly
            Assert.IsNotNull(metrics);
            Assert.Greater(metrics.Count, 0);
            
            // Verify specific metrics that should have been parsed successfully
            MetricAssert.Exists(metrics, "P12V_SCM_VOLT", 12.13, "volts");
            MetricAssert.Exists(metrics, "P5V_SCM_VOLT", 5.05, "volts");
            MetricAssert.Exists(metrics, "P3V_BAT_VOLT", 3.18, "volts");
            MetricAssert.Exists(metrics, "M2_TMP", 30, "celcius");
            MetricAssert.Exists(metrics, "HSC0_TMP", 29, "celcius");

            // Verify that invalid sensor entries were captured (header line is now skipped)
            Assert.IsNotNull(testParser.InvalidIpmiSensorEntries);
            Assert.AreEqual(17, testParser.InvalidIpmiSensorEntries.Count);

            // Verify specific invalid sensor entries with different malformed patterns
            var fansPwmEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "FANS_SWB_PWM");
            Assert.IsNotNull(fansPwmEvent);
            Assert.AreEqual("0001", fansPwmEvent["id"]);
            Assert.AreEqual("Fan", fansPwmEvent["type"]);
            Assert.AreEqual("NA", fansPwmEvent["reading"]);

            var p3v3ScmVoltEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "P3V3_SCM_VOLT");
            Assert.IsNotNull(p3v3ScmVoltEvent);
            Assert.AreEqual("0011", p3v3ScmVoltEvent["id"]);
            Assert.AreEqual("Voltage", p3v3ScmVoltEvent["type"]);
            Assert.AreEqual("...", p3v3ScmVoltEvent["reading"]);

            var sbpInletTmpEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "SBP_INLET_TMP");
            Assert.IsNotNull(sbpInletTmpEvent);
            Assert.AreEqual("0016", sbpInletTmpEvent["id"]);
            Assert.AreEqual("Temperature", sbpInletTmpEvent["type"]);
            Assert.AreEqual("..", sbpInletTmpEvent["reading"]);

            var e1s1TmpEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "E1S_1_TMP");
            Assert.IsNotNull(e1s1TmpEvent);
            Assert.AreEqual("0018", e1s1TmpEvent["id"]);
            Assert.AreEqual("Na", e1s1TmpEvent["reading"]);

            var hsc0InputPwrEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "HSC0_INPUT_PWR");
            Assert.IsNotNull(hsc0InputPwrEvent);
            Assert.AreEqual("001f", hsc0InputPwrEvent["id"]);
            Assert.AreEqual("-", hsc0InputPwrEvent["reading"]);

            var hsc0OutputCurEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "HSC0_OUTPUT_CUR");
            Assert.IsNotNull(hsc0OutputCurEvent);
            Assert.AreEqual("0021", hsc0OutputCurEvent["id"]);
            Assert.AreEqual("N/A", hsc0OutputCurEvent["reading"]);

            var hsc1TmpEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "HSC1_TMP");
            Assert.IsNotNull(hsc1TmpEvent);
            Assert.AreEqual("0026", hsc1TmpEvent["id"]);
            Assert.AreEqual("ERR", hsc1TmpEvent["reading"]);

            var fan6aEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "FAN_6A");
            Assert.IsNotNull(fan6aEvent);
            Assert.AreEqual("0046", fan6aEvent["id"]);
            Assert.AreEqual("UNKNOWN", fan6aEvent["reading"]);

            var invalidVoltEvent = testParser.InvalidIpmiSensorEntries.FirstOrDefault(e => e["name"]?.ToString() == "M1_VREG_CPUVOLT0");
            Assert.IsNotNull(invalidVoltEvent);
            Assert.AreEqual("008e", invalidVoltEvent["id"]);
            Assert.AreEqual("INVALID", invalidVoltEvent["reading"]);
        }
    }
}