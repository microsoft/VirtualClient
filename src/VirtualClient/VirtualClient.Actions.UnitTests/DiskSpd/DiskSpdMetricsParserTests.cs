using System.IO;
using System.Reflection;
using NUnit.Framework;
using System.Collections.Generic;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class DiskSpdMetricsParserTests
    {
        private string rawText;
        private DiskSpdMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\DiskSpdExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new DiskSpdMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void DiskSpdParserViewDataTable()
        {
            this.testParser.CpuUsage.PrintDataTableFormatted();
            this.testParser.TotalIo.PrintDataTableFormatted();
            this.testParser.ReadIo.PrintDataTableFormatted();
            this.testParser.WriteIo.PrintDataTableFormatted();
            this.testParser.Latency.PrintDataTableFormatted();
        }

        [Test]
        public void DiskSpdParserViewMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();
            DataTableTestExtensions.PrintMetricList(metrics);
        }

        [Test]
        [Ignore("We do not want to output CPU usage metrics anymore with DiskSpd workloads.")]
        public void DiskSpdParserVerifyCPU()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "CPU-0", 3.02, "percentage");
            MetricAssert.Exists(metrics, "CPU-1", 0.05, "percentage");
            MetricAssert.Exists(metrics, "CPU-2", 0.05, "percentage");
            MetricAssert.Exists(metrics, "CPU-3", 0.16, "percentage");
            MetricAssert.Exists(metrics, "CPU-avg.", 0.82, "percentage");
            MetricAssert.Exists(metrics, "Total-bytes-0", 146169856, "bytes");
        }

        [Test]
        public void DiskSpdParserVerifyTotalIO()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "Total-bytes-0", 146169856, "bytes");
            MetricAssert.Exists(metrics, "Total-bytes-total", 146169856, "bytes");
            MetricAssert.Exists(metrics, "Total-I/Os-0", 35686, "I/Os");
            MetricAssert.Exists(metrics, "Total-I/Os-total", 35686, "I/Os");
            MetricAssert.Exists(metrics, "Total-MiB/s-0", 4.64, "MiB/s");
            MetricAssert.Exists(metrics, "Total-MiB/s-total", 4.64, "MiB/s");
            MetricAssert.Exists(metrics, "Total-I/O per s-0", 1189.03, "iops");
            MetricAssert.Exists(metrics, "Total-I/O per s-total", 1189.03, "iops");
            MetricAssert.Exists(metrics, "Total-AvgLat-0", 0.84, "ms");
            MetricAssert.Exists(metrics, "Total-AvgLat-total", 0.84, "ms");
        }

        [Test]
        public void DiskSpdParserVerifyReadIO()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "Read-bytes-0", 102645760, "bytes");
            MetricAssert.Exists(metrics, "Read-bytes-total", 102645760, "bytes");
            MetricAssert.Exists(metrics, "Read-I/Os-0", 25060, "I/Os");
            MetricAssert.Exists(metrics, "Read-I/Os-total", 25060, "I/Os");
            MetricAssert.Exists(metrics, "Read-MiB/s-0", 3.26, "MiB/s");
            MetricAssert.Exists(metrics, "Read-MiB/s-total", 3.26, "MiB/s");
            MetricAssert.Exists(metrics, "Read-I/O per s-0", 834.98, "iops");
            MetricAssert.Exists(metrics, "Read-I/O per s-total", 834.98, "iops");
            MetricAssert.Exists(metrics, "Read-AvgLat-0", 0.272, "ms");
            MetricAssert.Exists(metrics, "Read-AvgLat-total", 0.272, "ms");
        }

        [Test]
        public void DiskSpdParserVerifyWriteIO()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "Write-bytes-0", 43524096, "bytes");
            MetricAssert.Exists(metrics, "Write-bytes-total", 43524096, "bytes");
            MetricAssert.Exists(metrics, "Write-I/Os-0", 10626, "I/Os");
            MetricAssert.Exists(metrics, "Write-I/Os-total", 10626, "I/Os");
            MetricAssert.Exists(metrics, "Write-MiB/s-0", 1.38, "MiB/s");
            MetricAssert.Exists(metrics, "Write-MiB/s-total", 1.38, "MiB/s");
            MetricAssert.Exists(metrics, "Write-I/O per s-0", 354.05, "iops");
            MetricAssert.Exists(metrics, "Write-I/O per s-total", 354.05, "iops");
            MetricAssert.Exists(metrics, "Write-AvgLat-0", 2.179, "ms");
            MetricAssert.Exists(metrics, "Write-AvgLat-total", 2.179, "ms");
        }

        [Test]
        public void DiskSpdParserVerifyLatency()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "Read-Latency-min", 0.036, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-25th", 0.175, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-50th", 0.232, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-75th", 0.314, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-90th", 0.458, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-95th", 0.588, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-99th", 0.877, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-3-nines", 1.608, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-4-nines", 4.753, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-5-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-6-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-7-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-8-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-9-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "Read-Latency-max", 7.99, "ms");

            MetricAssert.Exists(metrics, "Write-Latency-min", 1.209, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-25th", 1.607, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-50th", 1.833, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-75th", 2.308, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-90th", 3.378, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-95th", 4.2, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-99th", 6.107, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-3-nines", 8.378, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-4-nines", 11.561, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-5-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-6-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-7-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-8-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-9-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Write-Latency-max", 35.257, "ms");

            MetricAssert.Exists(metrics, "Total-Latency-min", 0.036, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-25th", 0.198, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-50th", 0.297, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-75th", 1.538, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-90th", 2.065, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-95th", 2.752, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-99th", 4.713, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-3-nines", 7.432, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-4-nines", 10.489, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-5-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-6-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-7-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-8-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-9-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "Total-Latency-max", 35.257, "ms");
        }
    }
}