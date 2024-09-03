// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LMbenchMetricsParserTests
    {
        private static string Examples = MockFixture.GetDirectory(typeof(LMbenchExecutorTests), "Examples", "LMbench");
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_1.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(30, metrics.Count);

            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/0K", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/16K", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/64K", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/16K", 14.4, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/64K", 13.8, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/16K", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/64K", 15, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_2p/0K", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_UDP", 35.7, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP", 40.3, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP_Conn", 71, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Create", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Delete", 9.8942, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Create", 21.9, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Delete", 12, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Mmap_Latency", 111500, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Pipe", 1869, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_AF_Unix", 3267, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_TCP", 3398, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_File_Reread", 5524.9, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mmap_Reread", 10200, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(libc)", 4644.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(hand)", 5324.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Reread", 9146, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Write", 7359, "megabytes/sec");
            MetricAssert.Exists(metrics, "Memory_Latency_Mhz", 1344, "megahertz");
            MetricAssert.Exists(metrics, "Memory_Latency_L1", 1.452, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_L2", 5.061, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Main_Mem", 27.1, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Random_Mem", 142.8, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_2.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(59, metrics.Count);

            MetricAssert.Exists(metrics, "Processor_Time_Mhz", 2998, "megahertz");
            MetricAssert.Exists(metrics, "Processor_Time_Null_Call", 0.28, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Null_I/O", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Stat", 0.92, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Open_Close", 1.82, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Slct_TCP", 2.61, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sig_Inst", 0.31, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sig_Hndl", 1.19, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Fork_Proc", 341, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Exec_Proc", 857, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sh_Proc", 2059, "microseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Bit", 0.25, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Add", 0.17, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Multiply", 0.68, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Mod", 2.76, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Bogo", 0.92, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Divide", 4.01, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Bogo", 1.8, "nanoseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/0K", 9.68, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/16K", 9.12, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/64K", 8.72, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/16K", 9.6, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/64K", 10, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/16K", 10.5, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/64K", 10.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_2p/0K", 9.68, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_Pipe", 20.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_AF_Unix", 18.3, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_UDP", 29.8, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP", 33.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP_Conn", 25, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Create", 8.2764, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Delete", 5.4662, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Create", 17, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Delete", 9.7852, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Mmap_Latency", 11000, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Prot_Fault", 0.189, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Page_Fault", 1.6182, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_100fd_Select", 1.32, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Pipe", 1857, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_AF_Unix", 6503, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_TCP", 4401, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_File_Reread", 5086.6, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mmap_Reread", 11700, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(libc)", 11800, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(hand)", 8952.2, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Reread", 10000, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Write", 23500, "megabytes/sec");
            MetricAssert.Exists(metrics, "Memory_Latency_Mhz", 2998, "megahertz");
            MetricAssert.Exists(metrics, "Memory_Latency_L1", 1.335, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_L2", 2.206, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Main_Mem", 9.256, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Random_Mem", 132, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_3.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(59, metrics.Count);

            MetricAssert.Exists(metrics, "Processor_Time_Mhz", 2998, "megahertz");
            MetricAssert.Exists(metrics, "Processor_Time_Null_Call", 0.28, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Null_I/O", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Stat", 0.92, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Open_Close", 1.8, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Slct_TCP", 2.61, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sig_Inst", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sig_Hndl", 1.21, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Fork_Proc", 310, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Exec_Proc", 843, "microseconds");
            MetricAssert.Exists(metrics, "Processor_Time_Sh_Proc", 1996, "microseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Bit", 0.25, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Add", 0.17, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Multiply", 0.68, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Integer_Operations_Time_Mod", 2.76, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Float_Operations_Time_Bogo", 0.92, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Divide", 4.01, "nanoseconds");
            MetricAssert.Exists(metrics, "Double_Operations_Time_Bogo", 1.8, "nanoseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/0K", 7.74, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/16K", 9.07, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/64K", 8.89, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/16K", 10.1, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/64K", 9.38, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/16K", 9.88, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/64K", 10.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_2p/0K", 7.74, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_Pipe", 19.6, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_AF_Unix", 20.3, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_UDP", 30.1, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP", 34.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP_Conn", 24, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Create", 8.2808, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Delete", 5.287, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Create", 16.3, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Delete", 8.6565, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Mmap_Latency", 11000, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Prot_Fault", 0.178, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Page_Fault", 1.6219, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_100fd_Select", 1.32, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Pipe", 1988, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_AF_Unix", 6484, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_TCP", 4311, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_File_Reread", 5350.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mmap_Reread", 12500, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(libc)", 12000, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(hand)", 9079.4, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Reread", 10000, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Write", 23600, "megabytes/sec");
            MetricAssert.Exists(metrics, "Memory_Latency_Mhz", 2998, "megahertz");
            MetricAssert.Exists(metrics, "Memory_Latency_L1", 1.335, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_L2", 2.247, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Main_Mem", 9.063, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Random_Mem", 131, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_4_RedHat_Scenario()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_rhel_1.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(31, metrics.Count);

            MetricAssert.Exists(metrics, "Processor_Time_Mhz", 1320, "megahertz");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/0K", 13, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/16K", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_2p/64K", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/16K", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_8p/64K", 15.4, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/16K", 15.3, "microseconds");
            MetricAssert.Exists(metrics, "Context_Switching_Time_16p/64K", 16, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_2p/0K", 13, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_UDP", 39.7, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP", 47.2, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Latency_TCP_Conn", 87, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Create", 19.6, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_0K_Delete", 15.5, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Create", 32.7, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_10K_Delete", 19.4, "microseconds");
            MetricAssert.Exists(metrics, "File_System_Latency_Mmap_Latency", 12500, "microseconds");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Pipe", 2395, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_AF_Unix", 5515, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_TCP", 3427, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_File_Reread", 5059.8, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mmap_Reread", 7510.9, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(libc)", 4839.3, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Bcopy(hand)", 4405.3, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Reread", 6832, "megabytes/sec");
            MetricAssert.Exists(metrics, "Communications_Bandwidth_Mem_Write", 5659, "megabytes/sec");
            MetricAssert.Exists(metrics, "Memory_Latency_Mhz", 1320, "megahertz");
            MetricAssert.Exists(metrics, "Memory_Latency_L1", 1.617, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_L2", 5.79, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Main_Mem", 38.4, "nanoseconds");
            MetricAssert.Exists(metrics, "Memory_Latency_Random_Mem", 112.5, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserLeavesOutTheRelatedMetricsWhenTheMemoryMegahertzCannotBeDetermined()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_1.txt");
            string results = File.ReadAllText(outputPath);
            results = results.Replace("1344", "-1  ");

            LMbenchMetricsParser parser = new LMbenchMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(29, metrics.Count);
            Assert.IsFalse(metrics.Any(m => m.Name == "Memory_Latency_Mhz"));
        }
    }
}