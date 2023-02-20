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
    public class DCGMIDmonCommandParserTests
    {   
        [Test]
        public void DCGMIDmonCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDmonResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIDmonCommandParser testParser = new DCGMIDmonCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(45, metrics.Count);
        }

        [Test]
        public void DCGMIDmonCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIDmonCommandParser testParser = new DCGMIDmonCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI dmon output file has incorrect format for parsing", exception.Message);
        }
    }
}
