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
    public class DCGMIDiagCommandParserTests
    {
        [Test]
        public void DCGMIDiagCommandParseParsesMetricsCorrectly_Scenario_r1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r1_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIDiagCommandParser testParser = new DCGMIDiagCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(9, metrics.Count);
            MetricAssert.Exists(metrics, "Deployment_Denylist", 1);
            MetricAssert.Exists(metrics, "Deployment_NVML Library", 1);
            MetricAssert.Exists(metrics, "Deployment_CUDA Main Library", 1);
            MetricAssert.Exists(metrics, "Deployment_Permissions and OS Blocks", 1);
            MetricAssert.Exists(metrics, "Deployment_Persistence Mode", 1);
            MetricAssert.Exists(metrics, "Deployment_Environment Variables", 1);
            MetricAssert.Exists(metrics, "Deployment_Page Retirement/Row Remap", 1);
            MetricAssert.Exists(metrics, "Deployment_Graphics Processes", 1);
            MetricAssert.Exists(metrics, "Deployment_Inforom", 1);
        }

        [Test]
        public void DCGMIDiagCommandParseParsesMetricsCorrectly_Scenario_r2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r2_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIDiagCommandParser testParser = new DCGMIDiagCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(11, metrics.Count);
            MetricAssert.Exists(metrics, "Deployment_Denylist", 1);
            MetricAssert.Exists(metrics, "Deployment_NVML Library", 1);
            MetricAssert.Exists(metrics, "Deployment_CUDA Main Library", 1);
            MetricAssert.Exists(metrics, "Deployment_Permissions and OS Blocks", 1);
            MetricAssert.Exists(metrics, "Deployment_Persistence Mode", 1);
            MetricAssert.Exists(metrics, "Deployment_Environment Variables", 1);
            MetricAssert.Exists(metrics, "Deployment_Page Retirement/Row Remap", 1);
            MetricAssert.Exists(metrics, "Deployment_Graphics Processes", 1);
            MetricAssert.Exists(metrics, "Deployment_Inforom", 1);

            MetricAssert.Exists(metrics, "Integration_PCIe", 1);

            MetricAssert.Exists(metrics, "Hardware_GPU Memory", 1);

        }

        [Test]
        public void DCGMIDiagCommandParseParsesMetricsCorrectly_Scenario_r3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r3_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIDiagCommandParser testParser = new DCGMIDiagCommandParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(16, metrics.Count);
            MetricAssert.Exists(metrics, "Deployment_Denylist", 1);
            MetricAssert.Exists(metrics, "Deployment_NVML Library", 1);
            MetricAssert.Exists(metrics, "Deployment_CUDA Main Library", 1);
            MetricAssert.Exists(metrics, "Deployment_Permissions and OS Blocks", 1);
            MetricAssert.Exists(metrics, "Deployment_Persistence Mode", 1);
            MetricAssert.Exists(metrics, "Deployment_Environment Variables", 1);
            MetricAssert.Exists(metrics, "Deployment_Page Retirement/Row Remap", 1);
            MetricAssert.Exists(metrics, "Deployment_Graphics Processes", 1);
            MetricAssert.Exists(metrics, "Deployment_Inforom", 1);

            MetricAssert.Exists(metrics, "Integration_PCIe", 1);

            MetricAssert.Exists(metrics, "Hardware_GPU Memory", 1);

            MetricAssert.Exists(metrics, "Stress_Targeted Stress", 1);
            MetricAssert.Exists(metrics, "Stress_Targeted Power", 1);
            MetricAssert.Exists(metrics, "Stress_Memory Bandwidth", 1);
        }

        [Test]
        public void DCGMIDiagCommandParseThrowsExceptionForIncorrectMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIDiagCommandParser testParser = new DCGMIDiagCommandParser(rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI output file has incorrect format for parsing", exception.Message);
        }
    }
}
