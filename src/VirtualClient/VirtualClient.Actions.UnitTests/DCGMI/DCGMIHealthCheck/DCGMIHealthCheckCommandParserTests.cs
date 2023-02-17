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
    public class DCGMIHealthCheckCommandParserTests
    {   
        [Test]
        public void DCGMIHealthCheckCommandParseParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIHealthCheckResults.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIHealthCheckCommandParser testParser = new DCGMIHealthCheckCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "Health Monitor Report_overallHealthValue", 1);
        }

        [Test]
        public void DCGMIHealthCheckCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIHealthCheckCommandParser testParser = new DCGMIHealthCheckCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI HealthCheck output file has incorrect format for parsing", exception.Message);
        }
    }
}
