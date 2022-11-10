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
    public class CPSMetricsParserTests2
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public void CpsParserParsesExpectedMetricsFromValidServerSideResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = CPSMetricsParserTests2.GetFileContents("CPS_Example_Results_Server.txt");

            CPSMetricsParser parser = new CPSMetricsParser(results, 90, 10);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count == 28);
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Cps" && m.Value == 14734));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMean" && m.Value == 2875));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMedian" && m.Value == 681));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP25" && m.Value == 431));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP75" && m.Value == 1891));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP90" && m.Value == 5316));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP95" && m.Value == 9723));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99" && m.Value == 52916));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_9" && m.Value == 73750));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_99" && m.Value == 98000));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitConnPercentage" && m.Value == 6.3051));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitPerConn" && m.Value == 1.1308));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Min" && m.Value == 11403));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Max" && m.Value == 17995));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Med" && m.Value == 14761.7));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Avg" && m.Value == 14735.306875000004));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P25" && m.Value == 14310.641666666666));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P50" && m.Value == 14761.7));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P75" && m.Value == 15188.416666666666));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P90" && m.Value == 15649.689999999999));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99" && m.Value == 17146.506666666664));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_9" && m.Value == 17995));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_99" && m.Value == 17995));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_999" && m.Value == 17995));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Mad" && m.Value == 439.64999999999964));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_StandardErrorMean" && m.Value == 48.498729848046267));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_LowerCI" && m.Value == 14655.533563306908));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_UpperCI" && m.Value == 14815.080186693103));
        }

        [Test]
        public void CpsParserParsesExpectedMetricsFromValidClientSideResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = CPSMetricsParserTests2.GetFileContents("CPS_Example_Results_Client.txt");

            CPSMetricsParser parser = new CPSMetricsParser(results, 90, 30);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count == 28);
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Cps" && m.Value == 24164));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMean" && m.Value == 18737));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttMedian" && m.Value == 6221));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP25" && m.Value == 3424));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP75" && m.Value == 10893));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP90" && m.Value == 17628));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP95" && m.Value == 24607));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99" && m.Value == 54222));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_9" && m.Value == 2027000));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SynRttP99_99" && m.Value == 4057000));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitConnPercentage" && m.Value == 3.2825));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "RexmitPerConn" && m.Value == 1.1436));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Min" && m.Value == 13494));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Max" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Med" && m.Value == 24246.9));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Avg" && m.Value == 21234.166666666668));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P25" && m.Value == 15286.150000000001));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P50" && m.Value == 24246.9));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P75" && m.Value == 25675.816666666666));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P90" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_9" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_99" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_P99_999" && m.Value == 25961.6));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_Mad" && m.Value == 1714.6999999999971));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_StandardErrorMean" && m.Value == 3901.6100968417873));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_LowerCI" && m.Value == 14816.58914792597));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ConnectsPerSec_UpperCI" && m.Value == 27651.744185407366));
        }

        private static string GetFileContents(string fileName)
        {
            string outputPath = Path.Combine(MockFixture.TestAssemblyDirectory, "Examples", "CPS", fileName);
            return File.ReadAllText(outputPath);
        }
    }
}
