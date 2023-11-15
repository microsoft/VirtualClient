// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Intrinsics.X86;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemtierMetricsParserTests
    {
        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromMemtierResultsCorrectlyWithoutPerProcessMetric_1()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_1.txt"));
            string results1 = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_2.txt"));
            resultsList.Add(results);
            resultsList.Add(results1);
            var parser = new MemtierMetricsParser(false, resultsList);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(138, metrics.Count);

            MetricAssert.Exists(metrics, "Throughput_Avg", 48156.565, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Min", 48041.840000000004, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Max", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Stdev", 114.72499999999854, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_P80", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Sum", 96313.13, MetricUnit.RequestsPerSec);

            MetricAssert.Exists(metrics, "Hits/sec_Avg", 43340.87125);
            MetricAssert.Exists(metrics, "Hits/sec_Min", 43237.6225);
            MetricAssert.Exists(metrics, "Hits/sec_Max", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Stdev", 103.24875000000247);
            MetricAssert.Exists(metrics, "Hits/sec_P80", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Sum", 86681.7425);

            MetricAssert.Exists(metrics, "Misses/sec_Avg", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Min", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Max", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Stdev", 0);
            MetricAssert.Exists(metrics, "Misses/sec_P80", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Sum", 0);

            MetricAssert.Exists(metrics, "Latency-Avg_Avg", 2.6292524999999998, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Min", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Max", 2.636375, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Stdev", 0.007122500000000143, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_P80", 2.636375, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P50_Avg", 2.7569999999999997, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Min", 2.751, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Max", 2.763, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Stdev", 0.006000000000000005, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_P80", 2.763, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P90_Avg", 3.917, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Min", 3.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Max", 3.931, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Stdev", 0.014000000000000012, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_P80", 3.931, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P95_Avg", 4.427, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Min", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Max", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Stdev", 0.01200000000000001, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_P80", 4.439, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P99_Avg", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Min", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Max", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_P80", 7.423, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P99.9_Avg", 29.294999999999998, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Min", 29.278999999999996, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Max", 29.311, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Stdev", 0.01600000000000179, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_P80", 29.311, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput_Avg", 43340.87125, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Min", 43237.6225, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Max", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Stdev", 103.24875000000247, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_P80", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Sum", 86681.7425, MetricUnit.RequestsPerSec);

            MetricAssert.Exists(metrics, "GET_Bandwidth_Avg", 3715.16, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Min", 3706.31, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Max", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Stdev", 8.850000000000136, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_P80", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Sum", 7430.32, MetricUnit.KilobytesPerSecond);

            MetricAssert.Exists(metrics, "GET_Latency-Avg_Avg", 2.62682875, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Min", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Max", 2.6338675, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Stdev", 0.0070387499999999825, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_P80", 2.6338675, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P50_Avg", 2.747, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Min", 2.735, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Max", 2.759, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Stdev", 0.01200000000000001, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_P80", 2.759, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P90_Avg", 3.903, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Min", 3.887, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Max", 3.919, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Stdev", 0.016000000000000014, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_P80", 3.919, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P95_Avg", 4.427, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Min", 4.415, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Max", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Stdev", 0.01200000000000001, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_P80", 4.439, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P99_Avg", 7.419, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Min", 7.414999999999999, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Max", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Stdev", 0.004000000000000448, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_P80", 7.423, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Avg", 29.262999999999998, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Min", 29.214999999999996, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Max", 29.311, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Stdev", 0.04800000000000182, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_P80", 29.311, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput_Avg", 4815.69375, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth_Avg", 328.6675, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg_Avg", 2.65108125, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50_Avg", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90_Avg", 3.947, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95_Avg", 4.487, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99_Avg", 7.503, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9_Avg", 29.583, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromMemtierResultsCorrectlyWithPerProcessMetric_1()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_1.txt"));
            resultsList.Add(results);
            var parser = new MemtierMetricsParser(true, resultsList);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(164, metrics.Count);

            MetricAssert.Exists(metrics, "Throughput", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Hits/sec", 43444.12);
            MetricAssert.Exists(metrics, "Misses/sec", 0);
            MetricAssert.Exists(metrics, "Latency-Avg", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50", 2.75100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90", 3.90300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9", 29.31100, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50", 2.73500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90", 3.88700, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9", 29.31100, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg", 2.64323, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50", 2.83100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90", 3.93500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95", 4.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99", 7.45500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9", 29.56700, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Throughput_Avg", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Min", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Max", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Stdev", 0, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_P80", 48271.29, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Throughput_Sum", 48271.29, MetricUnit.RequestsPerSec);

            MetricAssert.Exists(metrics, "Hits/sec_Avg", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Min", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Max", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Stdev", 0);
            MetricAssert.Exists(metrics, "Hits/sec_P80", 43444.12);
            MetricAssert.Exists(metrics, "Hits/sec_Sum", 43444.12);

            MetricAssert.Exists(metrics, "Misses/sec_Avg", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Min", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Max", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Stdev", 0);
            MetricAssert.Exists(metrics, "Misses/sec_P80", 0);
            MetricAssert.Exists(metrics, "Misses/sec_Sum", 0);

            MetricAssert.Exists(metrics, "Latency-Avg_Avg", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Min", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Max", 2.62213, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-Avg_P80", 2.62213, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P50_Avg", 2.75100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Min", 2.75100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Max", 2.75100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_P80", 2.75100, MetricUnit.Milliseconds);


            MetricAssert.Exists(metrics, "Latency-P90_Avg", 3.90300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Min", 3.90300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Max", 3.90300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_P80", 3.90300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P95_Avg", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Min", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Max", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_P80", 4.41500, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P99_Avg", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Min", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Max", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_P80", 7.42300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "Latency-P99.9_Avg", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Min", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Max", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_P80", 29.31100, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput_Avg", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Min", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Max", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Stdev", 0, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_P80", 43444.12, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Throughput_Sum", 43444.12, MetricUnit.RequestsPerSec);

            MetricAssert.Exists(metrics, "GET_Bandwidth_Avg", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Min", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Max", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Stdev", 0, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_P80", 3724.01, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Sum", 3724.01, MetricUnit.KilobytesPerSecond);

            MetricAssert.Exists(metrics, "GET_Latency-Avg_Avg", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Min", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Max", 2.61979, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_P80", 2.61979, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P50_Avg", 2.73500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Min", 2.73500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Max", 2.73500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_P80", 2.73500, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P90_Avg", 3.88700, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Min", 3.88700, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Max", 3.88700, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_P80", 3.88700, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P95_Avg", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Min", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Max", 4.41500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_P80", 4.41500, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P99_Avg", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Min", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Max", 7.42300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_P80", 7.42300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Avg", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Min", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Max", 29.31100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Stdev", 0, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_P80", 29.31100, MetricUnit.Milliseconds);


            MetricAssert.Exists(metrics, "SET_Throughput_Avg", 4827.17, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth_Avg", 329.45, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg_Avg", 2.64323, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50_Avg", 2.83100, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90_Avg", 3.93500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95_Avg", 4.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99_Avg", 7.45500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9_Avg", 29.56700, MetricUnit.Milliseconds);
        }
        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromMemtierResultsCorrectly_2()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_2.txt"));
            resultsList.Add(results);
            var parser = new MemtierMetricsParser(false, resultsList);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(138, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput_Avg", 48041.840000000004, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth_Avg", 4034.1975, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec_Avg", 43237.6225);
            MetricAssert.Exists(metrics, "Misses/sec_Avg", 0);
            MetricAssert.Exists(metrics, "Latency-Avg_Avg", 2.636375, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Avg", 2.763, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Avg", 3.931, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Avg", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Avg", 7.423, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Avg", 29.278999999999996, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput_Avg", 43237.6225, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Avg", 3706.31, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Avg", 2.6338675, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Avg", 2.759, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Avg", 3.919, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Avg", 4.439, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Avg", 7.4149999999999991, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Avg", 29.214999999999996, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput_Avg", 4804.2175000000007, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth_Avg", 327.885, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg_Avg", 2.6589324999999997, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50_Avg", 2.831, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90_Avg", 3.959, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95_Avg", 4.495, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99_Avg", 7.551, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9_Avg", 29.598999999999997, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromRedisResultsCorrectly_1()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_1.txt"));
            resultsList.Add(results);
            var parser = new MemtierMetricsParser(false, resultsList);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(138, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput_Avg", 355987.03, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth_Avg", 25860.83, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec_Avg", 320388.28);
            MetricAssert.Exists(metrics, "Misses/sec_Avg", 0);
            MetricAssert.Exists(metrics, "Latency-Avg_Avg", 0.34301, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput_Avg", 320388.28, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Avg", 23118.30, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Avg", 0.34300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput_Avg", 35598.74, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth_Avg", 2742.53, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg_Avg", 0.34304, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserParsesTheExpectedMetricsFromRedisForMoreThan1RedisServerInstancesResultsCorrectly_1()
        {
            List<string> resultsList = new List<string>();
            string results1 = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_1.txt"));
            resultsList.Add(results1);
            string results2 = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_3.txt"));
            resultsList.Add(results2);
            var parser = new MemtierMetricsParser(false, resultsList);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(138, metrics.Count);
            MetricAssert.Exists(metrics, "Throughput_Avg", 355987.03, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "Bandwidth_Avg", 25860.83, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "Hits/sec_Avg", 320388.28);
            MetricAssert.Exists(metrics, "Misses/sec_Avg", 0);
            MetricAssert.Exists(metrics, "Latency-Avg_Avg", 0.34301, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "GET_Throughput_Avg", 320388.28, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "GET_Bandwidth_Avg", 23118.30, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "GET_Latency-Avg_Avg", 0.34300, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "GET_Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);

            MetricAssert.Exists(metrics, "SET_Throughput_Avg", 35598.74, MetricUnit.RequestsPerSec);
            MetricAssert.Exists(metrics, "SET_Bandwidth_Avg", 2742.53, MetricUnit.KilobytesPerSecond);
            MetricAssert.Exists(metrics, "SET_Latency-Avg_Avg", 0.34304, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P50_Avg", 0.33500, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P90_Avg", 0.47900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P95_Avg", 0.55900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99_Avg", 0.83900, MetricUnit.Milliseconds);
            MetricAssert.Exists(metrics, "SET_Latency-P99.9_Avg", 1.54300, MetricUnit.Milliseconds);
        }

        [Test]
        public void MemtierMetricsParserAssociatesTheCorrectRelativityToTheMetrics()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\RedisResults_1.txt"));
            resultsList.Add(results);
            var parser = new MemtierMetricsParser(false, resultsList);

            IList<Metric> metrics = parser.Parse();

            if (metrics.Count != 138)
            {
                Assert.Inconclusive();
            }

            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Throughput") || m.Name.Contains("Bandwidth")).All(m => m.Relativity == MetricRelativity.HigherIsBetter));
            Assert.IsTrue(metrics.Where(m => m.Name.Contains("Latency")).All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void MemtierMetricsParserThrowIfInvalidResultsAreProvided()
        {
            List<string> resultsList = new List<string>();
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedInvalidResults_1.txt"));
            resultsList.Add(results);
            var parser = new MemtierMetricsParser(false, resultsList);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }
    }
}
