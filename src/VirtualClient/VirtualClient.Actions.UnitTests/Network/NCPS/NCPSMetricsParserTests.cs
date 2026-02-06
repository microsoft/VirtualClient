// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NCPSMetricsParserTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public void NcpsParserParsesExpectedMetricsFromValidServerSideResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = NCPSMetricsParserTests.GetFileContents("NCPS_Example_Results_Server.txt");

            NCPSMetricsParser parser = new NCPSMetricsParser(results, 90, 5);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count == 30);  // 28 + 2 throughput metrics
            
            // Throughput metrics (new in NCPS)
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RxGbps" && m.Value == 0.24));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "TxGbps" && m.Value == 0.24));
            
            // CPS metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Cps" && m.Value == 55683));
            
            // SYN RTT metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMean" && m.Value == 15996));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMedian" && m.Value == 4441));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP25" && m.Value == 2342));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP75" && m.Value == 13249));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP90" && m.Value == 24443));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP95" && m.Value == 38198));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99" && m.Value == 79657));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_9" && m.Value == 1038000));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_99" && m.Value == 1057000));
            
            // Retransmit metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitConnPercentage" && m.Value == 1.7278));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitPerConn" && m.Value == 1.0138));
            
            // Statistical metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Min"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Max"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Med"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Avg"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P25"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P50"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P75"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P90"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99"));
        }

        [Test]
        public void NcpsParserParsesExpectedMetricsFromValidClientSideResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = NCPSMetricsParserTests.GetFileContents("NCPS_Example_Results_Client.txt");

            NCPSMetricsParser parser = new NCPSMetricsParser(results, 90, 30);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            // Client results have no periodic data after warmup (all 10 rows are within warmup period of 30s)
            // So we only get: 2 throughput + 1 CPS + 9 SYN RTT + 2 Retransmit = 14 metrics
            Assert.IsTrue(metrics.Count == 14, $"Expected 14 metrics but got {metrics.Count}");
            
            // Throughput metrics (new in NCPS)
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RxGbps" && m.Value == 0.65));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "TxGbps" && m.Value == 0.65));
            
            // CPS metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Cps" && m.Value == 24416));
            
            // SYN RTT metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMean" && m.Value == 18737));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMedian" && m.Value == 6221));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP25" && m.Value == 3424));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP75" && m.Value == 10893));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP90" && m.Value == 17628));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP95" && m.Value == 24607));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99" && m.Value == 54222));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_9" && m.Value == 2027000));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_99" && m.Value == 4057000));
            
            // Retransmit metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitConnPercentage" && m.Value == 3.2825));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitPerConn" && m.Value == 1.1436));
            
            // No statistical metrics because all periodic data falls within warmup period
            Assert.IsNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Min"));
        }

        [Test]
        public void NcpsParserHandlesMissingThroughputMetrics()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = @"
###ENDCPS 12345

###SYNRTT,25:100,Median:200,Mean:150,75:300,90:400,95:500,99:600,99.9:700,99.99:800

###REXMIT,rtconnpercentage:1.5,rtperconn:1.2
";

            NCPSMetricsParser parser = new NCPSMetricsParser(results, 90, 5);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            
            // Should have CPS, RTT (9 metrics), and Retransmit (2 metrics) = 12 total metrics
            Assert.AreEqual(12, metrics.Count);
            
            // Should have CPS, RTT, and Retransmit metrics, but no throughput metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Cps"));
            Assert.IsNull(metrics.FirstOrDefault(m => m.Name == "RxGbps"));
            Assert.IsNull(metrics.FirstOrDefault(m => m.Name == "TxGbps"));
            
            // Verify specific RTT metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMean"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMedian"));
            
            // Verify retransmit metrics
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitConnPercentage"));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitPerConn"));
        }

        [Test]
        public void NcpsParserThrowsOnInvalidResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = "Invalid results content";

            NCPSMetricsParser parser = new NCPSMetricsParser(results, 90, 5);

            // The parser should throw WorkloadResultsException when parsing invalid content
            Assert.Throws<WorkloadResultsException>(() => parser.Parse());
        }

        [Test]
        public void NcpsParserHandlesDifferentConfidenceLevels()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = NCPSMetricsParserTests.GetFileContents("NCPS_Example_Results_Server.txt");

            // Test with 95% confidence level
            NCPSMetricsParser parser95 = new NCPSMetricsParser(results, 95, 5);
            IList<Metric> metrics95 = parser95.Parse();

            // Test with 99% confidence level
            NCPSMetricsParser parser99 = new NCPSMetricsParser(results, 99, 5);
            IList<Metric> metrics99 = parser99.Parse();

            // Both should parse successfully with different confidence intervals
            Assert.IsNotEmpty(metrics95);
            Assert.IsNotEmpty(metrics99);
            
            // Confidence intervals should be different
            var lowerCI95 = metrics95.FirstOrDefault(m => m.Name == "ConnectsPerSec_LowerCI");
            var lowerCI99 = metrics99.FirstOrDefault(m => m.Name == "ConnectsPerSec_LowerCI");
            
            Assert.IsNotNull(lowerCI95);
            Assert.IsNotNull(lowerCI99);
            Assert.AreNotEqual(lowerCI95.Value, lowerCI99.Value);
        }

        private static string GetFileContents(string fileName)
        {
            string outputPath = Path.Combine(MockFixture.TestAssemblyDirectory, "Examples", "NCPS", fileName);
            return File.ReadAllText(outputPath);
        }
    }
}
