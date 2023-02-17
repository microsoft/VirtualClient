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
    public class DCGMIModuleCommandParserTests
    {   
        [Test]
        public void DCGMIModuleCommandParserParsesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIModulesResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIModulesCommandParser testParser = new DCGMIModulesCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(10, metrics.Count);
            MetricAssert.Exists(metrics, "Status", 1);
            MetricAssert.Exists(metrics, "Core", 1);
            MetricAssert.Exists(metrics, "NvSwitch", 1);
            MetricAssert.Exists(metrics, "VGPU", 0);
            MetricAssert.Exists(metrics, "Introspection", 0);
            MetricAssert.Exists(metrics, "Health", 1);
            MetricAssert.Exists(metrics, "Policy", 0);
            MetricAssert.Exists(metrics, "Config", 0);
            MetricAssert.Exists(metrics, "Diag", 0);
            MetricAssert.Exists(metrics, "Profiling", 1);
        }

        [Test]
        public void DCGMIGroupCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIModulesCommandParser testParser = new DCGMIModulesCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Modules output file has incorrect format for parsing", exception.Message);
        }
    }
}
