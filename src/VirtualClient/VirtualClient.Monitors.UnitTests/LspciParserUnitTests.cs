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
    public class LspciParserUnitTests
    {
        [Test]
        public void LspciParserParsesMetricsCorrectly_Scenario1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "lspci", "linux-1.txt");
            string rawText = File.ReadAllText(outputPath);

            // Single distinct sample group
            LspciParser testParser = new LspciParser(rawText);
            IList<PciDevice> metrics = testParser.Parse();

            Assert.AreEqual(98, metrics.Count);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time", 2);
            MetricAssert.Exists(metrics, @"\Processor Information(_Total)\% System Time Min", 2);
        }
    }
}