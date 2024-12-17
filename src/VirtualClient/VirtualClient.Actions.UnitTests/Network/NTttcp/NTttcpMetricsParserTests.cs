using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class NTttcpMetricsParserTests
    {
        [Test]
        [TestCase("ClientOutput.xml")]
        public void NTttcpMetricsParserParsesClientSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: true);
            parser.Parse();
            Assert.Pass();
        }

        [Test]
        [TestCase("ServerOutput.xml")]
        public void NTttcpMetricsParserParsesServerSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);
            parser.Parse();
            Assert.Pass();
        }

        [Test]
        [TestCase("ClientOutput.xml")]
        public void NTttcpMetricsParserParsesExpectedClientSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: true);

            IList<Metric> metrics = parser.Parse();
            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 11);
            MetricAssert.Exists(metrics, "TotalBytesMB", 2047288.901632);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 0);
            MetricAssert.Exists(metrics, "AvgFrameSize", 0);
            MetricAssert.Exists(metrics, "ThroughputMbps", 55521.821);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 0);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 0);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 522885);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "TcpAverageRtt", 1886);
            MetricAssert.Exists(metrics, "CyclesPerByte", 2.594);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 326.99);
        }

        [Test]
        [TestCase("ServerOutput.xml")]
        public void NTttcpMetricsParserParsesExpectedServerSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);

            IList<Metric> metrics = parser.Parse();
            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 10);
            MetricAssert.Exists(metrics, "TotalBytesMB", 109552.625);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 65523.477);
            MetricAssert.Exists(metrics, "AvgFrameSize", 21842.77);
            MetricAssert.Exists(metrics, "ThroughputMbps", 30622.494);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 2.074);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 84480.28);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 0);
            MetricAssert.Exists(metrics, "Errors", 12);
            MetricAssert.Exists(metrics, "CyclesPerByte", 4.026);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 91.205);
        }

        [Test]
        [TestCase("ClientOutput-v1.4.0.xml")]
        public void NTttcpMetricsParserHandlesOutputOfNewToolsetVersionForClientSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: true);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 15);
            MetricAssert.Exists(metrics, "TotalBytesMB", 89095.376896);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 0);
            MetricAssert.Exists(metrics, "AvgFrameSize", 0);
            MetricAssert.Exists(metrics, "ThroughputMbps", 11808.681);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 0);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 0);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 5194);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "CyclesPerByte", 1.32);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 35.69);
            MetricAssert.Exists(metrics, "IdleCpuPercent", 95.29);
            MetricAssert.Exists(metrics, "IowaitCpuPercent", 0.07);
            MetricAssert.Exists(metrics, "SoftirqCpuPercent", 0.13);
            MetricAssert.Exists(metrics, "SystemCpuPercent", 2.65);
            MetricAssert.Exists(metrics, "UserCpuPercent", 0.85);
        }

        [Test]
        [TestCase("ServerOutput-v1.4.0.xml")]
        public void NTttcpMetricsParserHandlesOutputOfNewToolsetVersionForServerSideMetrics(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 15);
            MetricAssert.Exists(metrics, "TotalBytesMB", 88588.63077);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 0);
            MetricAssert.Exists(metrics, "AvgFrameSize", 0);
            MetricAssert.Exists(metrics, "ThroughputMbps", 11808.252);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 0);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 0);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 0);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "CyclesPerByte", 1.42);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 68.25);
            MetricAssert.Exists(metrics, "IdleCpuPercent", 94.95);
            MetricAssert.Exists(metrics, "IowaitCpuPercent", 0.04);
            MetricAssert.Exists(metrics, "SoftirqCpuPercent", 0.68);
            MetricAssert.Exists(metrics, "SystemCpuPercent", 3.98);
            MetricAssert.Exists(metrics, "UserCpuPercent", 0.35);
        }

        [Test]
        [TestCase("ClientOutput-v1.4.0-1.xml")]
        public void NTttcpMetricsParserHandlesOutputOfNewToolsetVersionForClientSideMetrics_Scenario2(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: true);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 16);
            MetricAssert.Exists(metrics, "TotalBytesMB", 89412.182016);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 0);
            MetricAssert.Exists(metrics, "AvgFrameSize", 0);
            MetricAssert.Exists(metrics, "ThroughputMbps", 11875.233);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 0);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 0);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 3494);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "TcpAverageRtt", 934);
            MetricAssert.Exists(metrics, "CyclesPerByte", 1.08);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 38.48);
            MetricAssert.Exists(metrics, "IdleCpuPercent", 96.13);
            MetricAssert.Exists(metrics, "IowaitCpuPercent", 0.07);
            MetricAssert.Exists(metrics, "SoftirqCpuPercent", 0.24);
            MetricAssert.Exists(metrics, "SystemCpuPercent", 2.76);
            MetricAssert.Exists(metrics, "UserCpuPercent", 0.8);
        }

        [Test]
        [TestCase("ServerOutput-v1.4.0-1.xml")]
        public void NTttcpMetricsParserHandlesOutputOfNewToolsetVersionForServerSideMetrics_Scenario2(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 15);
            MetricAssert.Exists(metrics, "TotalBytesMB", 89102.432956);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 0);
            MetricAssert.Exists(metrics, "AvgFrameSize", 0);
            MetricAssert.Exists(metrics, "ThroughputMbps", 11875.198);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 0);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 0);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 0);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "CyclesPerByte", 1.47);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 69.13);
            MetricAssert.Exists(metrics, "IdleCpuPercent", 94.76);
            MetricAssert.Exists(metrics, "IowaitCpuPercent", 0.05);
            MetricAssert.Exists(metrics, "SoftirqCpuPercent", 0.65);
            MetricAssert.Exists(metrics, "SystemCpuPercent", 4.08);
            MetricAssert.Exists(metrics, "UserCpuPercent", 0.47);
        }

        [Test]
        [TestCase("ClientOutput-v1.4.0-1.xml")]
        public void NTttcpMetricsParserParsesExpectedMetadataFromClientSideResults(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: true);
            parser.Parse();

            Assert.IsNotEmpty(parser.Metadata);
            Assert.AreEqual(parser.Metadata["cpuCores"], 16);
            Assert.AreEqual(parser.Metadata["cpuSpeed"], 2593.906);
        }

        [Test]
        [TestCase("ServerOutput-v1.4.0-1.xml")]
        public void NTttcpMetricsParserParsesExpectedMetadataFromServerSideResults(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);
            parser.Parse();

            Assert.IsNotEmpty(parser.Metadata);
            Assert.AreEqual(parser.Metadata["cpuCores"], 16);
            Assert.AreEqual(parser.Metadata["cpuSpeed"], 2593.906);
        }

        [Test]
        [TestCase("ServerOutput_NTttcp_Anomaly_1.xml")]
        public void NTttcpMetricsParserParsesExpectedMetrics_Anomaly_1_Very_Large_BufferCount_Values(string file)
        {
            string contents = NTttcpMetricsParserTests.GetFileContents(file);
            NTttcpMetricsParser parser = new NTttcpMetricsParser(contents, isClient: false);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            Assert.IsTrue(metrics.Count == 10);
            MetricAssert.Exists(metrics, "TotalBytesMB", 38471692.489753);
            MetricAssert.Exists(metrics, "AvgBytesPerCompl", 150843.719);
            MetricAssert.Exists(metrics, "AvgFrameSize", 3799.802);
            MetricAssert.Exists(metrics, "ThroughputMbps", 89645.202);
            MetricAssert.Exists(metrics, "AvgPacketsPerInterrupt", 8.201);
            MetricAssert.Exists(metrics, "InterruptsPerSec", 359577.17);
            MetricAssert.Exists(metrics, "PacketsRetransmitted", 14);
            MetricAssert.Exists(metrics, "Errors", 0);
            MetricAssert.Exists(metrics, "CyclesPerByte", 1.598);
            MetricAssert.Exists(metrics, "AvgCpuPercentage", 16.398);
        }

        private static string GetFileContents(string fileName)
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "NTttcp", fileName);
            return File.ReadAllText(outputPath);
        }
    }
}
