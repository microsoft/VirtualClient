// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Intrinsics.X86;
    using System.Text;
    using Microsoft.Identity.Client;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemtierMetricsParserTests
    {
        private static string ExamplesDirectory = MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier");

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromResults_RawMetrics_1()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_1.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(29, metrics.Count);

            MetricAssert.Exists(metrics, "Throughput", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth", 4053.46, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec", 43444.12);
            MetricAssert.Exists(metrics, "Misses/sec", 1.2);

            MetricAssert.Exists(metrics, "Latency-Avg", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 2.751, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P80", 3.479, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 3.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 29.311, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET-Throughput", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET-Bandwidth", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET-Latency-Avg", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P50", 2.735, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P80", 3.455, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P90", 3.887, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P95", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P99", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P99.9", 29.311, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET-Throughput", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET-Bandwidth", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET-Latency-Avg", 2.64323, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P80", 3.503, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P50", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P90", 3.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P95", 4.479, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P99", 7.455, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P99.9", 29.567, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromResults_RawMetrics_2()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_2.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(29, metrics.Count);

            MetricAssert.Exists(metrics, "Throughput", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth", 4053.46, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec", 43444.12);
            MetricAssert.Exists(metrics, "Misses/sec", 0.75);

            MetricAssert.Exists(metrics, "Latency-Avg", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 2.751, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P80", 3.479, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 3.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 29.311, MetricUnit.Milliseconds);
            
            MetricAssert.Exists(metrics, "GET-Throughput", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET-Bandwidth", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET-Latency-Avg", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P50", 2.735, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P80", 3.455, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P90", 3.887, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P95", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P99", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET-Latency-P99.9", 29.311, MetricUnit.Milliseconds);
            
            MetricAssert.Exists(metrics, "SET-Throughput", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET-Bandwidth", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET-Latency-Avg", 2.64323, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P50", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P80", 3.503, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P90", 3.935, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P95", 4.479, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P99", 7.455, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET-Latency-P99.9", 29.567, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserAggregatesMetricsIntoTheExpectedSetGivenASetOfIndividualResults()
        {
            string results1 = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_1.txt"));
            string results2 = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_2.txt"));
            string results3 = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_3.txt"));
            string results4 = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_4.txt"));
            string results5 = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_5.txt"));

            var parser1 = new MemtierMetricsParser(results1);
            var parser2 = new MemtierMetricsParser(results2);
            var parser3 = new MemtierMetricsParser(results3);
            var parser4 = new MemtierMetricsParser(results4);
            var parser5 = new MemtierMetricsParser(results5);

            List<Metric> allMetrics = new List<Metric>();
            allMetrics.AddRange(parser1.Parse());
            allMetrics.AddRange(parser2.Parse());
            allMetrics.AddRange(parser3.Parse());
            allMetrics.AddRange(parser4.Parse());
            allMetrics.AddRange(parser5.Parse());

            Assert.AreEqual(145, allMetrics.Count);
            IList<Metric> aggregateMetrics = MemtierMetricsParser.Aggregate(allMetrics);
            Assert.AreEqual(140, aggregateMetrics.Count);

            MetricAssert.Exists(aggregateMetrics, "Throughput Avg", 48087.73, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput Min", 47062.77, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput Max", 48923.52, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput Stddev", 679.7320566590922, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput P20", 47401.574, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput P50", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput P80", 48662.628, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "Throughput Total", 240438.65000000002, MetricUnit.RequestsPerSec);

            MetricAssert.Exists(aggregateMetrics, "Bandwidth Avg", 4038.05, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth Min", 3951.99, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth Max", 4108.23, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth Stddev", 57.07384646228067, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth P20", 3980.438, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth P50", 4053.46, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth P80", 4086.3219999999997, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "Bandwidth Total", 20190.25, MetricUnit.KilobytesPerSecond);

            MetricAssert.Exists(aggregateMetrics, "Hits/sec Avg", 43278.922);
            MetricAssert.Exists(aggregateMetrics, "Hits/sec Min", 42356.45);
            MetricAssert.Exists(aggregateMetrics, "Hits/sec Max", 44031.14);
            MetricAssert.Exists(aggregateMetrics, "Hits/sec Stddev", 611.7623576357098);
            MetricAssert.Exists(aggregateMetrics, "Misses/sec Avg", 0.39);
            MetricAssert.Exists(aggregateMetrics, "Misses/sec Min", 0);
            MetricAssert.Exists(aggregateMetrics, "Misses/sec Max", 1.2);
            MetricAssert.Exists(aggregateMetrics, "Misses/sec Stddev", 0.55722526863020128);

            MetricAssert.Exists(aggregateMetrics, "Latency-Avg Avg", 2.6335260000000003, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-Avg Min", 2.58862, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-Avg Max", 2.69065, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-Avg Stddev", 0.037587389241606095, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P50 Avg", 2.7605999999999997, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P50 Min", 2.735, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P50 Max", 2.815, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P50 Stddev", 0.03118974190338871, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P80 Avg", 3.4981999999999998, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P80 Min", 3.447, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P80 Max", 3.599, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P80 Stddev", 0.058405479195020836, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P90 Avg", 3.9254, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P90 Min", 3.855, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P90 Max", 4.047, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P90 Stddev", 0.07208883408684041, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P95 Avg", 4.4342, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P95 Min", 4.351, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P95 Max", 4.543, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P95 Stddev", 0.07010848736066132, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99 Avg", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99 Min", 6.943, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99 Max", 7.999, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99 Stddev", 0.37795237795256725, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99.9 Avg", 29.2854, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99.9 Min", 24.703, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99.9 Max", 35.071, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "Latency-P99.9 Stddev", 3.743206753573732, MetricUnit.Milliseconds);

            MetricAssert.Exists(aggregateMetrics, "GET-Throughput Avg", 43278.922, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput Min", 42356.45, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput Max", 44031.14, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput Stddev", 611.7623576357098, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput P20", 42661.382, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput P50", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput P80", 43796.332, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Throughput Total", 216394.61, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth Avg", 3709.85, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth Min", 3630.78, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth Max", 3774.33, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth Stddev", 52.438677042808905, MetricUnit.KilobytesPerSecond);

            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth P20", 3656.916, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth P50", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth P80", 3754.202, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Bandwidth Total", 18549.25, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-Avg Avg", 2.631052, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-Avg Min", 2.58644, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-Avg Max", 2.68904, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-Avg Stddev", 0.0377166736338187, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P50 Avg", 2.7542, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P50 Min", 2.735, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P50 Max", 2.815, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P50 Stddev", 0.03468717342188611, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P80 Avg", 3.4742000000000006, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P80 Min", 3.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P80 Max", 3.583, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P80 Stddev", 0.06237948380677749, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P90 Avg", 3.9126, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P90 Min", 3.855, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P90 Max", 4.031, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P90 Stddev", 0.06844559883586386, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P95 Avg", 4.4342, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P95 Min", 4.351, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P95 Max", 4.543, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P95 Stddev", 0.07010848736066132, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99 Avg", 7.4166, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99 Min", 6.943, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99 Max", 7.967, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99 Stddev", 0.36583712222791204, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99.9 Avg", 29.234199999999998, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99.9 Min", 24.703, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99.9 Max", 34.815, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "GET-Latency-P99.9 Stddev", 3.644742405163909, MetricUnit.Milliseconds);

            MetricAssert.Exists(aggregateMetrics, "SET-Throughput Avg", 4808.808, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput Min", 4706.32, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput Max", 4892.38, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput Stddev", 67.96969964623979, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput P20", 4740.192, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput P50", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput P80", 4866.296, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Throughput Total", 24044.04, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth Avg", 328.198, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth Min", 321.21, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth Max", 333.9, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth Stddev", 4.635824629987614, MetricUnit.KilobytesPerSecond);

            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth P20", 323.518, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth P50", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth P80", 332.12, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Bandwidth Total", 1640.99, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-Avg Avg", 2.6557919999999995, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-Avg Min", 2.6082, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-Avg Max", 2.7051, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-Avg Stddev", 0.03728209851926241, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P50 Avg", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P50 Min", 2.799, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P50 Max", 2.863, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P50 Stddev", 0.02262741699796954, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P80 Avg", 3.5222, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P80 Min", 3.471, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P80 Max", 3.615, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P80 Stddev", 0.05472842040475867, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P90 Avg", 3.9542, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P90 Min", 3.887, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P90 Max", 4.063, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P90 Stddev", 0.06538501357344821, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P95 Avg", 4.4918, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P95 Min", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P95 Max", 4.575, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P95 Stddev", 0.05813088679867189, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99 Avg", 7.5318, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99 Min", 7.007, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99 Max", 8.383, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99 Stddev", 0.5102971683244968, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99.9 Avg", 29.592599999999997, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99.9 Min", 24.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99.9 Max", 35.839, MetricUnit.Milliseconds);
            MetricAssert.Exists(aggregateMetrics, "SET-Latency-P99.9 Stddev", 3.9920817626897374, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserAssociatesTheCorrectRelativityWithEachOfTheMetrics()
        {
            string results = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Memcached_Results_1.txt"));
            var parser = new MemtierMetricsParser(results);

            IList<Metric> metrics = parser.Parse();

            if (metrics.Count != 29)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Throughput") || m.Name.Contains("Bandwidth")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Latency")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void MemtierMetricsParserThrowsIfInvalidResultsAreProvided()
        {
            string invalidResults = File.ReadAllText(MockFixture.GetDirectory(typeof(MemtierMetricsParserTests), "Examples", "Memtier", "Memtier_Invalid_Results_1.txt"));
            var parser = new MemtierMetricsParser(invalidResults);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }
    }
}
