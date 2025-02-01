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
    public class AmdSmiXGMIQueryGpuParserUnitTests
    {
        [Test]
        public void AmdSmiXGMIQueryGpuParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "xgmi-8xMI300X.json");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiXGMIQueryGpuParser testParser = new AmdSmiXGMIQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(8, metrics.Count);
            MetricAssert.Exists(metrics, "xgmi_0_data", 14, "KB");
            MetricAssert.Exists(metrics, "xgmi_1_data", 12, "KB");
            MetricAssert.Exists(metrics, "xgmi_2_data", 10, "KB");
            MetricAssert.Exists(metrics, "xgmi_3_data", 9, "KB");
            MetricAssert.Exists(metrics, "xgmi_4_data", 9, "KB");
            MetricAssert.Exists(metrics, "xgmi_5_data", 8, "KB");
            MetricAssert.Exists(metrics, "xgmi_6_data", 6, "KB");
            MetricAssert.Exists(metrics, "xgmi_7_data", 6, "KB");
        }
    }
}