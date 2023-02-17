// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
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
    public class DCGMIDiscoveryCommandParserTests
    {   
        [Test]
        public void DCGMIDiscoveryCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiscoveryResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIDiscoveryCommandParser testParser = new DCGMIDiscoveryCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "GPUCount", 1);
            MetricAssert.Exists(metrics, "NvSwitchCount", 0);
        }

        [Test]
        public void DCGMIDiscoveryCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIDiscoveryCommandParser testParser = new DCGMIDiscoveryCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Discovery output file has incorrect format for parsing", exception.Message);
        }
    }
}
