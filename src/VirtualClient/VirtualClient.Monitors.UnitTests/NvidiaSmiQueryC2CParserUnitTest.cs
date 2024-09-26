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
    public class NvidiaSmiQueryC2CParserUnitTest
    {
        [Test]
        public void NvidiaSmiC2CParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "nvidia-smi", "query-c2c.txt");
            string rawText = File.ReadAllText(outputPath);

            NvidiaSmiC2CParser testParser = new NvidiaSmiC2CParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(10, metrics.Count); 
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 0 Speed", 44.712, "GB/s"); 
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 1 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 2 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 3 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 4 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 5 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 6 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 7 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 8 Speed", 44.712, "GB/s");
            MetricAssert.Exists(metrics, "GPU 0: C2C Link 9 Speed", 44.712, "GB/s");
        }
    }
}
