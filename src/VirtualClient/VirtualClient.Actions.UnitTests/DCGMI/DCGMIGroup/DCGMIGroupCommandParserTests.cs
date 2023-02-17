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
    public class DCGMIGroupCommandParserTests
    {   
        [Test]
        public void DCGMIGroupCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIGroupResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIGroupCommandParser testParser = new DCGMIGroupCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "GroupCount", 2);
        }

        [Test]
        public void DCGMIGroupCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIGroupCommandParser testParser = new DCGMIGroupCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Group output file has incorrect format for parsing", exception.Message);
        }
    }
}
