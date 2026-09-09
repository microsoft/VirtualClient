// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;
    using VirtualClient;

    [TestFixture]
    [Category("Unit")]
    internal class MemoryLatencyCheckerMetricsParserTests
    {
        private MemoryLatencyCheckerMetricsParser testParser;
        private string workingDirectory;

        [SetUp]
        public void Setup()
        {
            this.workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForLatencyMatrixMultipleNumaNode()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-latency-matrix-multiple.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.LatencyMatrix);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(16, metrics.Count);
            MetricAssert.Exists(metrics, "latency-matrix_nodes_0_0", 142.7, "nanoseconds");
            MetricAssert.Exists(metrics, "latency-matrix_nodes_2_0", 163.2, "nanoseconds");
            MetricAssert.Exists(metrics, "latency-matrix_nodes_3_3", 133.2, "nanoseconds");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForLatencyMatrixSingleNumaNode()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-latency-matrix-single.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.LatencyMatrix);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "latency-matrix_nodes_0_0", 103.9, "nanoseconds");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForLoadedLatency()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-loaded-latency.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.LoadedLatency);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(38, metrics.Count);
            MetricAssert.Exists(metrics, "loaded_latency_at_delay_00000", 499.23, "nanoseconds");
            MetricAssert.Exists(metrics, "loaded_latency_at_delay_01000", 148.71, "nanoseconds");
            MetricAssert.Exists(metrics, "loaded_latency_at_delay_20000", 140.56, "nanoseconds");
            MetricAssert.Exists(metrics, "load_bandwidth_at_delay_00000", 107230.6, "MBps");
            MetricAssert.Exists(metrics, "load_bandwidth_at_delay_01300", 9813.4, "MBps");
            MetricAssert.Exists(metrics, "load_bandwidth_at_delay_20000", 1078.8, "MBps");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForIdleLatency()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-idle-latency.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.IdleLatency);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "base_frequency_clock_iterations", 200.5, string.Empty);
            MetricAssert.Exists(metrics, "time_elapsed_per_iteration", 95.7, "nanoseconds");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForPeakInjectionMemoryBandwidth()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-peak-injection-bandwidth.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.PeakInjectionBandwidth);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(5, metrics.Count);
            MetricAssert.Exists(metrics, "peak_injection_bandwidth_at_ALL Reads", 23374.2, "MBps");
            MetricAssert.Exists(metrics, "peak_injection_bandwidth_at_3:1 Reads-Writes", 28411.7, "MBps");
            MetricAssert.Exists(metrics, "peak_injection_bandwidth_at_2:1 Reads-Writes", 28951.8, "MBps");
            MetricAssert.Exists(metrics, "peak_injection_bandwidth_at_1:1 Reads-Writes", 33407.3, "MBps");
            MetricAssert.Exists(metrics, "peak_injection_bandwidth_at_Stream-triad like", 24379.2, "MBps");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForBandwidthMatrixSingleNumaNode()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-bandwidth-matrix-single.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.BandwidthMatrix);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "memory_bandwidth_nodes_0_0", 23146.6, "MBps");
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForBandwidthMatrixMultipleNumaNode()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-bandwidth-matrix-multiple.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.BandwidthMatrix);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(16, metrics.Count);
            MetricAssert.Exists(metrics, "memory_bandwidth_nodes_0_0", 22309.4, "MBps");
            MetricAssert.Exists(metrics, "memory_bandwidth_nodes_1_3", 14503.1, "MBps");
            MetricAssert.Exists(metrics, "memory_bandwidth_nodes_3_3", 29195.3, "MBps");
        }

        [Test]
        public void MemoryLatencyCheckerParserThrowsIfInvalidResultsProvided()
        {
            string InvalidOutputPath = Path.Combine(workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-invalid.txt");
            string rawText = File.ReadAllText(InvalidOutputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, string.Empty);
            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
            StringAssert.Contains("The Workload did not generate any valid metrics! ", exception.Message);
        }

        [Test]
        public void MemoryLatencyCheckerParserVerifyMetricsForC2CLatency()
        {
            string outputPath = Path.Combine(this.workingDirectory, "Examples", "MemoryLatencyChecker", "mlc-c2c-latency.txt");
            string rawText = File.ReadAllText(outputPath);
            this.testParser = new MemoryLatencyCheckerMetricsParser(rawText, MemoryLatencyCheckerMetricsParser.MemoryLatencyCheckerBenchmark.C2CLatency);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "Local Socket L2->L2 HIT  latency", 53.4, "nanoseconds");
            MetricAssert.Exists(metrics, "Local Socket L2->L2 HITM latency", 54.2, "nanoseconds");
        }
    }
}