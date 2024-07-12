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
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "result2.txt");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiXGMIQueryGpuParser testParser = new AmdSmiXGMIQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(64, metrics.Count);
            MetricAssert.Exists(metrics, "xgmi.bidata.0", 0, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.1", 0, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.2", 2, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.3", 2, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.4", 2, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.5", 2, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.6", 2, "KB");
            MetricAssert.Exists(metrics, "xgmi.bidata.7", 2, "KB");

        }
    }
}