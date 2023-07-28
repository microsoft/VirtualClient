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
    public class AmdSmiQueryGpuParserUnitTests
    {
        [Test]
        public void AmdSmiQueryGpuParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "amd-smi", "result.txt");
            string rawText = File.ReadAllText(outputPath);

            AmdSmiQueryGpuParser testParser = new AmdSmiQueryGpuParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(3, metrics.Count);
            MetricAssert.Exists(metrics, "utilization.gpu [%]", 98, "%");
            MetricAssert.Exists(metrics, "framebuffer.total [MB]", 14928, "MB");
            MetricAssert.Exists(metrics, "framebuffer.used [MB]", 363, "MB");
        }
    }
}