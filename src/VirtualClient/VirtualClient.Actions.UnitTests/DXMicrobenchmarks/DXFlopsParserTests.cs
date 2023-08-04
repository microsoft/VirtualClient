// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
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
    using VirtualClient.Monitors;

    [TestFixture]
    [Category("Unit")]
    public class DXFlopsParserTests
    {
        [Test]
        public void DXFlopsParserTestsCorrectly_ScenarioFLOPS()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DXMicrobenchmarks", "result.txt");
            string rawText = File.ReadAllText(outputPath);

            DXFLOPSParser testParser = new DXFLOPSParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "performance.gpu [TFLOPs]", 14.501, "TFLOPs");
        }

    }
}