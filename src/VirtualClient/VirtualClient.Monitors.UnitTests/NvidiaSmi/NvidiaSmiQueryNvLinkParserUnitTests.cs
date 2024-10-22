// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
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
    public class NvidiaSmiQueryNvLinkParserUnitTest
    {
        [Test]
        public void NvidiaSmiNvLinkParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-nvlink.txt");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiQueryNvLinkParser testParser = new NvidiaSmiQueryNvLinkParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(192, metrics.Count);
            MetricAssert.Exists(metrics, "GPU 0: NvLink Rx 0 Throughput", 200, "KiB");
            MetricAssert.Exists(metrics, "GPU 1: NvLink Tx 11 Throughput", 800, "KiB");
            MetricAssert.Exists(metrics, "GPU 2: NvLink Rx 9 Throughput", 500, "KiB");
            MetricAssert.Exists(metrics, "GPU 3: NvLink Tx 5 Throughput", 1200, "KiB");
            MetricAssert.Exists(metrics, "GPU 4: NvLink Rx 1 Throughput", 2000, "KiB");
            MetricAssert.Exists(metrics, "GPU 5: NvLink Tx 3 Throughput", 400, "KiB");
            MetricAssert.Exists(metrics, "GPU 6: NvLink Rx 2 Throughput", 750, "KiB");
            MetricAssert.Exists(metrics, "GPU 7: NvLink Tx 10 Throughput", 600, "KiB");
        }
    }
}
