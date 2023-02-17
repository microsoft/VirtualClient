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
    public class DCGMIFieldGroupCommandParserTests
    {   
        [Test]
        public void DCGMIFieldGroupCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIFieldGroupsResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIFieldGroupCommandParser testParser = new DCGMIFieldGroupCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "fieldCount", 3);
        }

        [Test]
        public void DCGMIFieldGroupCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIFieldGroupCommandParser testParser = new DCGMIFieldGroupCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI FieldGroup output file has incorrect format for parsing", exception.Message);
        }
    }
}
