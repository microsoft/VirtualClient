// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LatteMetricsParserTests2
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public void LatteParserParsesExpectedMetricsFromValidResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = LatteMetricsParserTests2.GetFileContents("Latte_Results_Example.txt");

            LatteMetricsParser2 parser = new LatteMetricsParser2(results);
            IList<Metric> metrics = parser.Parse();

            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count == 14);
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-Avg" && m.Value == 224.79));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-Max" && m.Value == 5087));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-Min" && m.Value == 67));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P25" && m.Value == 90));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P50" && m.Value == 104));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P75" && m.Value == 252));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P90" && m.Value == 585));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P99" && m.Value == 1205));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P99.9" && m.Value == 2566));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P99.99" && m.Value == 3957));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Latency-P99.999" && m.Value == 4644));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "Interrupts/sec" && m.Value == 8125));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "SystemCalls/sec" && m.Value == 15482));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "ContextSwitches/sec" && m.Value == 2164));
        }


        [Test]
        public void LatteParserThrowsIfTheResultsAreInvalid()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = LatteMetricsParserTests2.GetFileContents("Latte_Results_Example.txt").Substring(0, 10);
            LatteMetricsParser2 parser = new LatteMetricsParser2(results);
            Assert.Throws<WorkloadResultsException>(() => parser.Parse());
        }

        [Test]
        public void LatteParserThrowsIfTheExpectedLatencyMeasurementsAreNotDefinedInTheResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = Regex.Replace(
                LatteMetricsParserTests2.GetFileContents("Latte_Results_Example.txt"),
                @"\s*(?<Interval>\d+)\s*(?<Frequency>\d+)",
                string.Empty,
                RegexOptions.Multiline);

            LatteMetricsParser2 parser = new LatteMetricsParser2(results);
            Assert.Throws<WorkloadResultsException>(() => parser.Parse());
        }

        [Test]
        public void LatteParserThrowsIfTheExpectedLatencyHistogramMeasurementsAreNotDefinedInTheResults()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            string results = Regex.Replace(
                LatteMetricsParserTests2.GetFileContents("Latte_Results_Example.txt"),
                @"(?<=\s*Interval\(usec\)\s*Frequency\s*)(\s*[0-9]+\s+[0-9]+\s*$)+",
                string.Empty,
                RegexOptions.Multiline);

            LatteMetricsParser2 parser = new LatteMetricsParser2(results);
            Assert.Throws<WorkloadResultsException>(() => parser.Parse());
        }

        private static string GetFileContents(string fileName)
        {
            string outputPath = Path.Combine(MockFixture.TestAssemblyDirectory, "Examples", "Latte", fileName);
            return File.ReadAllText(outputPath);
        }
    }
}
