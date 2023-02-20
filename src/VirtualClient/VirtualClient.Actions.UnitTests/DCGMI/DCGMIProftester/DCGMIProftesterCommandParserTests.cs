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
    public class DCGMIProftesterCommandParserTests
    {   
        [Test]
        public void DCGMIProftesterCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIProftesterResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIProftesterCommandParser testParser = new DCGMIProftesterCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
        }

        [Test]
        public void DCGMIProftesterCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIProftesterCommandParser testParser = new DCGMIProftesterCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Proftester output file has incorrect format for parsing", exception.Message);
        }
    }
}
