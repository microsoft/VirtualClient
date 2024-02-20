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
    public class DCGMIResultsParserTests
    {
        [Test]
        public void DCGMIResultsParseParsesDiagnosticsMetricsCorrectly_Scenario_r1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r1_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Diagnostics");
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
        public void DCGMIResultsParseParsesDaignosticsMetricsCorrectly_Scenario_r2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r2_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Diagnostics");
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
        public void DCGMIResultsParseParsesDiagnosticsMetricsCorrectly_Scenario_r3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiag_r3_results.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Diagnostics");
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
        public void DCGMIResultsParseThrowsExceptionForIncorrectDiagnosticsMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Diagnostics");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Diagnostics output file has incorrect format for parsing", exception.Message);
        }


        [Test]
        public void DCGMIResultsParserParsesDiscoveryMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDiscoveryResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Discovery");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "GPUCount", 1);
            MetricAssert.Exists(metrics, "NvSwitchCount", 0);
        }

        [Test]
        public void DCGMIREsultsParseThrowsExceptionForIncorrectDiscoveryMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Discovery");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Discovery output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesFieldGroupMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIFieldGroupsResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "FieldGroup");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "fieldCount", 3);
        }

        [Test]
        public void DCGMIResultsParseThrowsExceptionForIncorrectFieldGroupMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText,"FieldGroup");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI FieldGroup output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesGroupMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIGroupResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Group");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "GroupCount", 2);
        }

        [Test]
        public void DCGMIResultsParseThrowsExceptionForIncorrectGroupMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Group");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Group output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesHealthMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIHealthCheckResults.json");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Health");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "Health Monitor Report_overallHealthValue", 1);
        }

        [Test]
        public void DCGMIResultsParseThrowsExceptionForIncorrectHealthMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Health");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Health output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesModulesMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIModulesResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Modules");
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
        public void DCGMIResultsParseThrowsExceptionForIncorrectModuleMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "Modules");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Modules output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesProftesterMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIProftesterResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "CUDATestGenerator");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
        }

        [Test]
        public void DCGMIResultsParseThrowsExceptionForIncorrectProftesterMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "CUDATestGenerator");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI Proftester output file has incorrect format for parsing", exception.Message);
        }

        [Test]
        public void DCGMIResultsParserParsesProftesterDmonMetricsCorrectly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIDmonResults.txt");
            string rawText = File.ReadAllText(outputPath);

            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "CUDATestGeneratorDmon");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(45, metrics.Count);
        }

        [Test]
        public void DCGMIRsultsParseThrowsExceptionForIncorrectDmonMetrics()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectoutputPath = Path.Combine(workingDirectory, "Examples", "DCGMI", "DCGMIIncorrectresults.json");
            string rawText = File.ReadAllText(IncorrectoutputPath);
            DCGMIResultsParser testParser = new DCGMIResultsParser(rawText, "CUDATestGeneratorDmon");
            SchemaException exception = Assert.Throws<SchemaException>(() => testParser.Parse());
            StringAssert.Contains("The DCGMI dmon output file has incorrect format for parsing", exception.Message);
        }
    }
}
