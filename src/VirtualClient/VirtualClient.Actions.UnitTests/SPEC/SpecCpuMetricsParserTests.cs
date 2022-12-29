using System.Collections.Generic;
using System.IO;
using System.Reflection;
using global::VirtualClient.Contracts;
using NUnit.Framework;
using VirtualClient;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class SpecCpuMetricsParserTests
    {
        private string rawText;
        private SpecCpuMetricsParser testParser;

        [Test]
        public void SpecCpuParserVerifyMetricsFpRate()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(28, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 778, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 730, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 553, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 676, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 748, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 185, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 505, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 783, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 703, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 2680, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 1060, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 255, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 273, "Score");

            MetricAssert.Exists(metrics, "SPECcpu-peak-503.bwaves_r", 778, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-507.cactuBSSN_r", 735, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-508.namd_r", 649, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-510.parest_r", 680, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-511.povray_r", 849, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-519.lbm_r", 185, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-521.wrf_r", 505, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-526.blender_r", 916, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-527.cam4_r", 733, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-538.imagick_r", 2980, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-544.nab_r", 1330, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-549.fotonik3d_r", 256, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-554.roms_r", 294, "Score");

            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 622, "Score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_peak", 666, "Score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsFpRateBaseOnly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateBaseOnlyExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(14, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 1160, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 876, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 571, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 288, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 866, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 357, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 516, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 834, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 729, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 2290, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 1600, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 366, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 229, "Score");

            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 668, "Score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsIntRateBaseOnly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntRateBaseOnlyExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(11, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-500.perlbench_r", 178, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-502.gcc_r", 132, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 99.1, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-520.omnetpp_r", 78.2, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-523.xalancbmk_r", 122, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-525.x264_r", 183, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 188, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 172, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-548.exchange2_r", 312, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 114, "Score");

            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_base", 152, "Score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsFpSpeed()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpSpeedExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-603.bwaves_s", 769, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-607.cactuBSSN_s", 394, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-619.lbm_s", 127, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-621.wrf_s", 170, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-627.cam4_s", 175, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-628.pop2_s", 76.5, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-638.imagick_s", 459, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-644.nab_s", 587, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-649.fotonik3d_s", 113, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-654.roms_s", 316, "Score");

            MetricAssert.Exists(metrics, "SPECcpu-peak-603.bwaves_s", 771, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-607.cactuBSSN_s", 398, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-619.lbm_s", 127, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-621.wrf_s", 170, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-627.cam4_s", 175, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-628.pop2_s", 78, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-638.imagick_s", 459, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-644.nab_s", 600, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-649.fotonik3d_s", 113, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-654.roms_s", 380, "Score");

            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_base", 255, "Score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_peak", 268, "Score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsIntRate()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntRateExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-500.perlbench_r", 538, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-502.gcc_r", 572, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 874, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-520.omnetpp_r", 361, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-523.xalancbmk_r", 900, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-525.x264_r", 1800, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 744, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 741, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-548.exchange2_r", 1750, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 484, "Score");

            MetricAssert.Exists(metrics, "SPECcpu-peak-500.perlbench_r", 587, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-502.gcc_r", 710, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-505.mcf_r", 982, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-520.omnetpp_r", 362, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-523.xalancbmk_r", 981, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-525.x264_r", 1800, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-531.deepsjeng_r", 748, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-541.leela_r", 748, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-548.exchange2_r", 1750, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-557.xz_r", 486, "Score");

            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_base", 777, "Score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_peak", 812, "Score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsIntSpeed()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntSpeedExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-600.perlbench_s", 6.84, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-602.gcc_s", 13.3, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-605.mcf_s", 20.5, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-620.omnetpp_s", 8.67, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-623.xalancbmk_s", 13.7, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-625.x264_s", 16.7, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-631.deepsjeng_s", 6.49, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-641.leela_s", 5.63, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-648.exchange2_s", 22.7, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-base-657.xz_s", 25.4, "Score");

            MetricAssert.Exists(metrics, "SPECcpu-peak-600.perlbench_s", 6.86, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-602.gcc_s", 13.3, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-605.mcf_s", 20.5, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-620.omnetpp_s", 8.67, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-623.xalancbmk_s", 13.8, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-625.x264_s", 16.7, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-631.deepsjeng_s", 6.49, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-641.leela_s", 5.63, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-648.exchange2_s", 22.7, "Score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-657.xz_s", 25.4, "Score");

            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_base", 13.4, "Score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_peak", 13.4, "Score");
        }
    }
}