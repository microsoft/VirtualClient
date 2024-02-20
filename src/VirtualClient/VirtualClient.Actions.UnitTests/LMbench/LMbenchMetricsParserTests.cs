using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;
using System;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class LMbenchMetricsParserTests
    {
        private string rawText;
        private LMbenchMetricsParser testParser;

        [Test]
        public void LMbenchParserVerifyMetricsX64()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LMbenchX64Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LMbenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(37, metrics.Count);

            MetricAssert.Exists(metrics, "ContextSwitching-2p/0K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/16K ctxsw", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/64K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/16K ctxsw", 14.4, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/64K ctxsw", 13.8, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/16K ctxsw", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/64K ctxsw", 15, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationLatency-2p/0K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-Pipe", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-AF UNIX", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-UDP", 35.7, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/UDP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP", 40.3, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/TCP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP conn", 71, "microseconds");

            MetricAssert.Exists(metrics, "FileVmLatency-0K File Create", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-0K File Delete", 9.8942, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Create", 21.9, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Delete", 12, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Mmap Latency", 111500, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Prot Fault", 0, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-Page Fault", 0, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-100fd select", 0, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationBandwidth-Pipe", 1869, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-AF UNIX", 3267, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-TCP", 3398, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-File reread", 5524.9, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mmap reread", 10200, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (libc)", 4644.5, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (hand)", 5324.5, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem reread", 9146, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem write", 7359, "MB/s");

            MetricAssert.Exists(metrics, "MemoryLatency-Mhz", 1344, "Mhz");
            MetricAssert.Exists(metrics, "MemoryLatency-L1", 1.452, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-L2", 5.061, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Main mem", 27.1, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Rand mem", 142.8, "nanoseconds");
        }

        [Test]
        public void LMbenchParserVerifyMetricsARM64()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LMbenchARM64Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LMbenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(61, metrics.Count);

            MetricAssert.Exists(metrics, "ProcessorTimes-Mhz", 2494, "Mhz");
            MetricAssert.Exists(metrics, "ProcessorTimes-null call", 3.41, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-null I/O", 3.58, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-stat", 4.46, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-open clos", 9.54, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-slct TCP", 7.01, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-sig inst", 3.58, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-sig hndl", 9.62, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-fork proc", 370, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-exec proc", 946, "microseconds");
            MetricAssert.Exists(metrics, "ProcessorTimes-sh proc", 2281, "microseconds");

            MetricAssert.Exists(metrics, "BasicInt-intgr bit", 0.27, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicInt-intgr add", 0.1, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicInt-intgr mul", 0.01, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicInt-intgr div", 7.23, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicInt-intgr mod", 7.55, "nanoseconds");

            MetricAssert.Exists(metrics, "BasicFloat-float add", 2.41, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicFloat-float mul", 2.41, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicFloat-float div", 6.42, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicFloat-float bogo", 6.83, "nanoseconds");


            MetricAssert.Exists(metrics, "BasicDouble-double add", 2.41, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicDouble-double mul", 2.41, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicDouble-double div", 9.25, "nanoseconds");
            MetricAssert.Exists(metrics, "BasicDouble-double bogo", 9.67, "nanoseconds");

            MetricAssert.Exists(metrics, "ContextSwitching-2p/0K ctxsw", 15.8, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/16K ctxsw", 19.6, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/64K ctxsw", 13.1, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/16K ctxsw", 12.8, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/64K ctxsw", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/16K ctxsw", 16.5, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/64K ctxsw", 16.9, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationLatency-2p/0K ctxsw", 15.8, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-Pipe", 41.8, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-AF UNIX", 42.0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-UDP", 54.1, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/UDP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP", 62, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/TCP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP conn", 63, "microseconds");

            MetricAssert.Exists(metrics, "FileVmLatency-0K File Create", 23.5, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-0K File Delete", 15.4, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Create", 48.2, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Delete", 22.2, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Mmap Latency", 76700000, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Prot Fault", 3.545, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-Page Fault", 0, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-100fd select", 5.070, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationBandwidth-Pipe", 2159, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-AF UNIX", 4149, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-TCP", 2250, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-File reread", 2641.3, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mmap reread", 3202.3, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (libc)", 6442.9, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (hand)", 1505.7, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem reread", 1190, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem write", 8098, "MB/s");

            MetricAssert.Exists(metrics, "MemoryLatency-Mhz", 2494, "Mhz");
            MetricAssert.Exists(metrics, "MemoryLatency-L1", 1.6130, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-L2", 4.463, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Main mem", 107.7, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Rand mem", 74.4, "nanoseconds");
        }

        [Test]
        public void LMbenchParserVerifyMetricsWhenMhzReturnsNegative()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LMbenchX64Example.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LMbenchMetricsParser(this.rawText.Replace("1344", "  -1"));
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(36, metrics.Count);

            MetricAssert.Exists(metrics, "ContextSwitching-2p/0K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/16K ctxsw", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-2p/64K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/16K ctxsw", 14.4, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-8p/64K ctxsw", 13.8, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/16K ctxsw", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "ContextSwitching-16p/64K ctxsw", 15, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationLatency-2p/0K ctxsw", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-Pipe", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-AF UNIX", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-UDP", 35.7, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/UDP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP", 40.3, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-RPC/TCP", 0, "microseconds");
            MetricAssert.Exists(metrics, "CommunicationLatency-TCP conn", 71, "microseconds");

            MetricAssert.Exists(metrics, "FileVmLatency-0K File Create", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-0K File Delete", 9.8942, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Create", 21.9, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-10K File Delete", 12, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Mmap Latency", 111500, "microseconds");
            MetricAssert.Exists(metrics, "FileVmLatency-Prot Fault", 0, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-Page Fault", 0, "Count");
            MetricAssert.Exists(metrics, "FileVmLatency-100fd select", 0, "microseconds");

            MetricAssert.Exists(metrics, "CommunicationBandwidth-Pipe", 1869, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-AF UNIX", 3267, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-TCP", 3398, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-File reread", 5524.9, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mmap reread", 10200, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (libc)", 4644.5, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Bcopy (hand)", 5324.5, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem reread", 9146, "MB/s");
            MetricAssert.Exists(metrics, "CommunicationBandwidth-Mem write", 7359, "MB/s");

            MetricAssert.Exists(metrics, "MemoryLatency-L1", 1.452, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-L2", 5.061, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Main mem", 27.1, "nanoseconds");
            MetricAssert.Exists(metrics, "MemoryLatency-Rand mem", 142.8, "nanoseconds");
        }
    }
}