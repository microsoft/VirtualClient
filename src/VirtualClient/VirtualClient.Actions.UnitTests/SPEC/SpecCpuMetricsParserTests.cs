// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecCpuMetricsParserTests
    {
        private string rawText;
        private SpecCpuMetricsParser testParser;

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpRateResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(28, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 778, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 730, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 553, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 676, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 748, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 185, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 505, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 783, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 703, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 2680, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 1060, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 255, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 273, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-503.bwaves_r", 778, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-507.cactuBSSN_r", 735, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-508.namd_r", 649, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-510.parest_r", 680, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-511.povray_r", 849, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-519.lbm_r", 185, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-521.wrf_r", 505, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-526.blender_r", 916, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-527.cam4_r", 733, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-538.imagick_r", 2980, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-544.nab_r", 1330, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-549.fotonik3d_r", 256, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-554.roms_r", 294, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 622, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_peak", 666, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpRateCsvResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText, csv: true);
            
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(28, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 778.384, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 730.16448, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 552.521856, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 675.63456, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 747.969792, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 184.683776, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 505.152512, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 783.495808, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 703.044992, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 2675.130112, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 1062.333184, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 255.484032, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 273.042304, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-503.bwaves_r", 778.384, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-507.cactuBSSN_r", 734.649728, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-508.namd_r", 648.725248, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-510.parest_r", 680.160256, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-511.povray_r", 849.209344, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-519.lbm_r", 184.683776, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-521.wrf_r", 505.152512, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-526.blender_r", 916.208128, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-527.cam4_r", 732.529152, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-538.imagick_r", 2979.6096, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-544.nab_r", 1330.808832, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-549.fotonik3d_r", 255.60832, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-554.roms_r", 293.983104, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 609.928969, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_peak", 653.91811, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpRateBaseOnlyResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateBaseOnlyExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(14, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 1160, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 876, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 571, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 288, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 866, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 357, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 516, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 834, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 729, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 2290, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 1600, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 366, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 229, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 668, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpRateBaseOnlyCsvResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpRateBaseExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText, csv: true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(14, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-503.bwaves_r", 103.575608, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-507.cactuBSSN_r", 19.12632, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-508.namd_r", 21.355696, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-510.parest_r", 24.463184, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-511.povray_r", 24.021488, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-519.lbm_r", 13.741744, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-521.wrf_r", 31.1542, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-526.blender_r", 22.3982, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-527.cam4_r", 31.49204, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-538.imagick_r", 27.20268, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-544.nab_r", 27.566832, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-549.fotonik3d_r", 33.141112, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-554.roms_r", 20.841072, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_fp_base", 26.914269, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromIntRateBaseOnlyResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntRateBaseOnlyExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(11, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-500.perlbench_r", 178, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-502.gcc_r", 132, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 99.1, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-520.omnetpp_r", 78.2, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-523.xalancbmk_r", 122, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-525.x264_r", 183, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 188, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 172, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-548.exchange2_r", 312, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 114, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_base", 152, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpSpeedResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpSpeedExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-603.bwaves_s", 769, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-607.cactuBSSN_s", 394, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-619.lbm_s", 127, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-621.wrf_s", 170, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-627.cam4_s", 175, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-628.pop2_s", 76.5, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-638.imagick_s", 459, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-644.nab_s", 587, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-649.fotonik3d_s", 113, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-654.roms_s", 316, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-603.bwaves_s", 771, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-607.cactuBSSN_s", 398, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-619.lbm_s", 127, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-621.wrf_s", 170, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-627.cam4_s", 175, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-628.pop2_s", 78, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-638.imagick_s", 459, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-644.nab_s", 600, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-649.fotonik3d_s", 113, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-654.roms_s", 380, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_base", 255, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_peak", 268, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromFpSpeedCsvResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuFpSpeedExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText, csv: true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-603.bwaves_s", 768.831224, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-607.cactuBSSN_s", 393.80929, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-619.lbm_s", 126.697282, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-621.wrf_s", 169.830133, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-627.cam4_s", 174.864469, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-628.pop2_s", 76.498401, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-638.imagick_s", 458.810572, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-644.nab_s", 586.924345, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-649.fotonik3d_s", 112.86379, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-654.roms_s", 316.447243, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-603.bwaves_s", 770.76427, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-607.cactuBSSN_s", 398.433218, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-619.lbm_s", 126.697282, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-621.wrf_s", 169.830133, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-627.cam4_s", 174.864469, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-628.pop2_s", 77.966521, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-638.imagick_s", 458.810572, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-644.nab_s", 600.140555, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-649.fotonik3d_s", 112.86379, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-654.roms_s", 379.737643, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_base", 246.792776, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_fp_peak", 252.731489, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromIntRateResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntRateExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-500.perlbench_r", 538, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-502.gcc_r", 572, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 874, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-520.omnetpp_r", 361, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-523.xalancbmk_r", 900, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-525.x264_r", 1800, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 744, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 741, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-548.exchange2_r", 1750, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 484, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-500.perlbench_r", 587, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-502.gcc_r", 710, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-505.mcf_r", 982, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-520.omnetpp_r", 362, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-523.xalancbmk_r", 981, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-525.x264_r", 1800, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-531.deepsjeng_r", 748, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-541.leela_r", 748, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-548.exchange2_r", 1750, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-557.xz_r", 486, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_base", 777, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_peak", 812, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromIntRateCsvResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntRateExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText, csv: true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-500.perlbench_r", 538.076672, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-502.gcc_r", 571.518464, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 874.32576, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-520.omnetpp_r", 360.731136, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-523.xalancbmk_r", 900.179968, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-525.x264_r", 1800.746752, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 744.361472, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 740.663296, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-548.exchange2_r", 1753.447168, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 484.324608, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-500.perlbench_r", 587.497728, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-502.gcc_r", 709.671424, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-505.mcf_r", 981.908736, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-520.omnetpp_r", 362.158592, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-523.xalancbmk_r", 981.1392, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-525.x264_r", 1800.746752, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-531.deepsjeng_r", 747.652096, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-541.leela_r", 748.0576, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-548.exchange2_r", 1753.447168, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-557.xz_r", 486.0992, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_base", 770.361774, "score");
            MetricAssert.Exists(metrics, "SPECrate(R)2017_int_peak", 812.169896, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromIntSpeedResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntSpeedExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-600.perlbench_s", 6.84, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-602.gcc_s", 13.3, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-605.mcf_s", 20.5, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-620.omnetpp_s", 8.67, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-623.xalancbmk_s", 13.7, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-625.x264_s", 16.7, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-631.deepsjeng_s", 6.49, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-641.leela_s", 5.63, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-648.exchange2_s", 22.7, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-657.xz_s", 25.4, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-600.perlbench_s", 6.86, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-602.gcc_s", 13.3, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-605.mcf_s", 20.5, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-620.omnetpp_s", 8.67, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-623.xalancbmk_s", 13.8, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-625.x264_s", 16.7, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-631.deepsjeng_s", 6.49, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-641.leela_s", 5.63, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-648.exchange2_s", 22.7, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-657.xz_s", 25.4, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_base", 13.4, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_peak", 13.4, "score");
        }

        [Test]
        public void SpecCpuMetricsParserParsesExpectedMetricsFromIntSpeedCsvResults()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "SpecCpuIntSpeedExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText, csv: true);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(22, metrics.Count);
            MetricAssert.Exists(metrics, "SPECcpu-base-600.perlbench_s", 6.843991, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-602.gcc_s", 13.252091, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-605.mcf_s", 20.503121, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-620.omnetpp_s", 8.669764, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-623.xalancbmk_s", 13.722154, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-625.x264_s", 16.686116, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-631.deepsjeng_s", 6.49303, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-641.leela_s", 5.631354, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-648.exchange2_s", 22.735378, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-657.xz_s", 25.403967, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-600.perlbench_s", 6.856309, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-602.gcc_s", 13.293098, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-605.mcf_s", 20.503121, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-620.omnetpp_s", 8.669764, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-623.xalancbmk_s", 13.763672, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-625.x264_s", 16.730556, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-631.deepsjeng_s", 6.49303, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-641.leela_s", 5.634441, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-648.exchange2_s", 22.735378, "score");
            MetricAssert.Exists(metrics, "SPECcpu-peak-657.xz_s", 25.403967, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_base", 12.279658, "score");
            MetricAssert.Exists(metrics, "SPECspeed(R)2017_int_peak", 12.293316, "score");
        }

        [Test]
        public void SpecCpuParserVerifyMetricsIntRateBaseWinArm64Incomplete()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "SpecCpu", "intrate-base-win-arm64-1.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new SpecCpuMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);

            MetricAssert.Exists(metrics, "SPECcpu-base-505.mcf_r", 2.44, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-531.deepsjeng_r", 2.20, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-541.leela_r", 2.57, "score");
            MetricAssert.Exists(metrics, "SPECcpu-base-557.xz_r", 1.57, "score");
        }
    }
}