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

        [Test]
        public void DiskSpdParserVerifyReadWrite()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\DiskSpd\DiskSpdExample-ReadWrite.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new DiskSpdMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();

            // cpu metrics
            MetricAssert.Exists(metrics, "cpu usage 0", 3.02, "percentage");
            MetricAssert.Exists(metrics, "cpu usage 1", 0.05, "percentage");
            MetricAssert.Exists(metrics, "cpu usage 2", 0.05, "percentage");
            MetricAssert.Exists(metrics, "cpu usage 3", 0.16, "percentage");
            MetricAssert.Exists(metrics, "cpu usage average", 0.82, "percentage");
            MetricAssert.Exists(metrics, "cpu user 0", 0.36, "percentage");
            MetricAssert.Exists(metrics, "cpu user 1", 0.05, "percentage");
            MetricAssert.Exists(metrics, "cpu user 2", 0, "percentage");
            MetricAssert.Exists(metrics, "cpu user 3", 0, "percentage");
            MetricAssert.Exists(metrics, "cpu user average", 0.1, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel 0", 2.65, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel 1", 0, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel 2", 0.05, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel 3", 0.16, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel average", 0.72, "percentage");

            // Total
            MetricAssert.Exists(metrics, "total bytes 0", 146169856, "bytes");
            MetricAssert.Exists(metrics, "total bytes total", 146169856, "bytes");
            MetricAssert.Exists(metrics, "total io operations 0", 35686, "I/Os");
            MetricAssert.Exists(metrics, "total io operations total", 35686, "I/Os");
            MetricAssert.Exists(metrics, "total throughput 0", 4.64, "MiB/s");
            MetricAssert.Exists(metrics, "total throughput total", 4.64, "MiB/s");
            MetricAssert.Exists(metrics, "total iops 0", 1189.03, "iops");
            MetricAssert.Exists(metrics, "total iops total", 1189.03, "iops");
            MetricAssert.Exists(metrics, "total latency average 0", 0.84, "ms");
            MetricAssert.Exists(metrics, "total latency average total", 0.84, "ms");

            // Read
            MetricAssert.Exists(metrics, "read bytes 0", 102645760, "bytes");
            MetricAssert.Exists(metrics, "read bytes total", 102645760, "bytes");
            MetricAssert.Exists(metrics, "read io operations 0", 25060, "I/Os");
            MetricAssert.Exists(metrics, "read io operations total", 25060, "I/Os");
            MetricAssert.Exists(metrics, "read throughput 0", 3.26, "MiB/s");
            MetricAssert.Exists(metrics, "read throughput total", 3.26, "MiB/s");
            MetricAssert.Exists(metrics, "read iops 0", 834.98, "iops");
            MetricAssert.Exists(metrics, "read iops total", 834.98, "iops");
            MetricAssert.Exists(metrics, "read latency average 0", 0.272, "ms");
            MetricAssert.Exists(metrics, "read latency average total", 0.272, "ms");

            // Write
            MetricAssert.Exists(metrics, "write bytes 0", 43524096, "bytes");
            MetricAssert.Exists(metrics, "write bytes total", 43524096, "bytes");
            MetricAssert.Exists(metrics, "write io operations 0", 10626, "I/Os");
            MetricAssert.Exists(metrics, "write io operations total", 10626, "I/Os");
            MetricAssert.Exists(metrics, "write throughput 0", 1.38, "MiB/s");
            MetricAssert.Exists(metrics, "write throughput total", 1.38, "MiB/s");
            MetricAssert.Exists(metrics, "write iops 0", 354.05, "iops");
            MetricAssert.Exists(metrics, "write iops total", 354.05, "iops");
            MetricAssert.Exists(metrics, "write latency average 0", 2.179, "ms");
            MetricAssert.Exists(metrics, "write latency average total", 2.179, "ms");

            // latency
            MetricAssert.Exists(metrics, "read latency min", 0.036, "ms");
            MetricAssert.Exists(metrics, "read latency 25th", 0.175, "ms");
            MetricAssert.Exists(metrics, "read latency 50th", 0.232, "ms");
            MetricAssert.Exists(metrics, "read latency 75th", 0.314, "ms");
            MetricAssert.Exists(metrics, "read latency 90th", 0.458, "ms");
            MetricAssert.Exists(metrics, "read latency 95th", 0.588, "ms");
            MetricAssert.Exists(metrics, "read latency 99th", 0.877, "ms");
            MetricAssert.Exists(metrics, "read latency 3-nines", 1.608, "ms");
            MetricAssert.Exists(metrics, "read latency 4-nines", 4.753, "ms");
            MetricAssert.Exists(metrics, "read latency 5-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "read latency 6-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "read latency 7-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "read latency 8-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "read latency 9-nines", 7.99, "ms");
            MetricAssert.Exists(metrics, "read latency max", 7.99, "ms");

            MetricAssert.Exists(metrics, "write latency min", 1.209, "ms");
            MetricAssert.Exists(metrics, "write latency 25th", 1.607, "ms");
            MetricAssert.Exists(metrics, "write latency 50th", 1.833, "ms");
            MetricAssert.Exists(metrics, "write latency 75th", 2.308, "ms");
            MetricAssert.Exists(metrics, "write latency 90th", 3.378, "ms");
            MetricAssert.Exists(metrics, "write latency 95th", 4.2, "ms");
            MetricAssert.Exists(metrics, "write latency 99th", 6.107, "ms");
            MetricAssert.Exists(metrics, "write latency 3-nines", 8.378, "ms");
            MetricAssert.Exists(metrics, "write latency 4-nines", 11.561, "ms");
            MetricAssert.Exists(metrics, "write latency 5-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "write latency 6-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "write latency 7-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "write latency 8-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "write latency 9-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "write latency max", 35.257, "ms");

            MetricAssert.Exists(metrics, "total latency min", 0.036, "ms");
            MetricAssert.Exists(metrics, "total latency 25th", 0.198, "ms");
            MetricAssert.Exists(metrics, "total latency 50th", 0.297, "ms");
            MetricAssert.Exists(metrics, "total latency 75th", 1.538, "ms");
            MetricAssert.Exists(metrics, "total latency 90th", 2.065, "ms");
            MetricAssert.Exists(metrics, "total latency 95th", 2.752, "ms");
            MetricAssert.Exists(metrics, "total latency 99th", 4.713, "ms");
            MetricAssert.Exists(metrics, "total latency 3-nines", 7.432, "ms");
            MetricAssert.Exists(metrics, "total latency 4-nines", 10.489, "ms");
            MetricAssert.Exists(metrics, "total latency 5-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "total latency 6-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "total latency 7-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "total latency 8-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "total latency 9-nines", 35.257, "ms");
            MetricAssert.Exists(metrics, "total latency max", 35.257, "ms");
        }

        [Test]
        public void DiskSpdParserVerifyWriteOnly()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\DiskSpd\DiskSpdExample-WriteOnly.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new DiskSpdMetricsParser(this.rawText);

            IList<Metric> metrics = this.testParser.Parse();

            // cpu metrics
            MetricAssert.Exists(metrics, "cpu usage 0", 5.55, "percentage");
            MetricAssert.Exists(metrics, "cpu usage average", 6.15, "percentage");
            MetricAssert.Exists(metrics, "cpu user 1", 0.56, "percentage");
            MetricAssert.Exists(metrics, "cpu user average", 0.33, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel 2", 13.81, "percentage");
            MetricAssert.Exists(metrics, "cpu kernel average", 5.82, "percentage");

            // Total
            MetricAssert.Exists(metrics, "total bytes 0", 23594541056, "bytes");
            MetricAssert.Exists(metrics, "total io operations 0", 2880193, "I/Os");
            MetricAssert.Exists(metrics, "total throughput 0", 25, "MiB/s");
            MetricAssert.Exists(metrics, "total iops 0", 3200.19, "iops");
            MetricAssert.Exists(metrics, "total latency average 0", 4.999, "ms");
            MetricAssert.Exists(metrics, "total bytes total", 563719340032, "bytes");
            MetricAssert.Exists(metrics, "total io operations total", 68813396, "I/Os");
            MetricAssert.Exists(metrics, "total throughput total", 597.33, "MiB/s");
            MetricAssert.Exists(metrics, "total iops total", 76458.71, "iops");
            MetricAssert.Exists(metrics, "total latency average total", 6.696, "ms");

            // Read
            MetricAssert.Exists(metrics, "read bytes 0", 0, "bytes");
            MetricAssert.Exists(metrics, "read bytes total", 0, "bytes");
            MetricAssert.Exists(metrics, "read io operations 0", 0, "I/Os");
            MetricAssert.Exists(metrics, "read io operations total", 0, "I/Os");
            MetricAssert.Exists(metrics, "read throughput 0", 0, "MiB/s");
            MetricAssert.Exists(metrics, "read throughput total", 0, "MiB/s");
            MetricAssert.Exists(metrics, "read iops 0", 0, "iops");
            MetricAssert.Exists(metrics, "read iops total", 0, "iops");
            MetricAssert.Exists(metrics, "read latency average 0", 0, "ms");
            MetricAssert.Exists(metrics, "read latency average total", 0, "ms");

            // Write
            MetricAssert.Exists(metrics, "write bytes 0", 23594541056, "bytes");
            MetricAssert.Exists(metrics, "write io operations 0", 2880193, "I/Os");
            MetricAssert.Exists(metrics, "write throughput 0", 25, "MiB/s");
            MetricAssert.Exists(metrics, "write iops 0", 3200.19, "iops");
            MetricAssert.Exists(metrics, "write latency average 0", 4.999, "ms");
            MetricAssert.Exists(metrics, "write bytes total", 563719340032, "bytes");
            MetricAssert.Exists(metrics, "write io operations total", 68813396, "I/Os");
            MetricAssert.Exists(metrics, "write throughput total", 597.33, "MiB/s");
            MetricAssert.Exists(metrics, "write iops total", 76458.71, "iops");
            MetricAssert.Exists(metrics, "write latency average total", 6.696, "ms");

            // latency
            MetricAssert.Exists(metrics, "write latency min", 0.074, "ms");
            MetricAssert.Exists(metrics, "write latency 25th", 0.832, "ms");
            MetricAssert.Exists(metrics, "write latency 50th", 0.943, "ms");
            MetricAssert.Exists(metrics, "write latency 75th", 1.16, "ms");
            MetricAssert.Exists(metrics, "write latency 90th", 41.609, "ms");
            MetricAssert.Exists(metrics, "write latency 95th", 42.093, "ms");
            MetricAssert.Exists(metrics, "write latency 99th", 57.658, "ms");
            MetricAssert.Exists(metrics, "write latency 3-nines", 58.623, "ms");
            MetricAssert.Exists(metrics, "write latency 4-nines", 93.636, "ms");
            MetricAssert.Exists(metrics, "write latency 5-nines", 143.883, "ms");
            MetricAssert.Exists(metrics, "write latency 6-nines", 158.908, "ms");
            MetricAssert.Exists(metrics, "write latency 7-nines", 159.155, "ms");
            MetricAssert.Exists(metrics, "write latency 8-nines", 159.270, "ms");
            MetricAssert.Exists(metrics, "write latency 9-nines", 159.270, "ms");
            MetricAssert.Exists(metrics, "write latency max", 159.270, "ms");

            MetricAssert.Exists(metrics, "total latency min", 0.074, "ms");
            MetricAssert.Exists(metrics, "total latency 25th", 0.832, "ms");
            MetricAssert.Exists(metrics, "total latency 50th", 0.943, "ms");
            MetricAssert.Exists(metrics, "total latency 75th", 1.16, "ms");
            MetricAssert.Exists(metrics, "total latency 90th", 41.609, "ms");
            MetricAssert.Exists(metrics, "total latency 95th", 42.093, "ms");
            MetricAssert.Exists(metrics, "total latency 99th", 57.658, "ms");
            MetricAssert.Exists(metrics, "total latency 3-nines", 58.623, "ms");
            MetricAssert.Exists(metrics, "total latency 4-nines", 93.636, "ms");
            MetricAssert.Exists(metrics, "total latency 5-nines", 143.883, "ms");
            MetricAssert.Exists(metrics, "total latency 6-nines", 158.908, "ms");
            MetricAssert.Exists(metrics, "total latency 7-nines", 159.155, "ms");
            MetricAssert.Exists(metrics, "total latency 8-nines", 159.270, "ms");
            MetricAssert.Exists(metrics, "total latency 9-nines", 159.270, "ms");
            MetricAssert.Exists(metrics, "total latency max", 159.270, "ms");
        }
    }
}