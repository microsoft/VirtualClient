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

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IList<Metric> metrics = parser.ParseResults(results);

            Assert.AreEqual(30, metrics.Count);
            MetricAssert.Exists(metrics, "context_switching_time_2p/0k", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/16k", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/64k", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/16k", 14.4, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/64k", 13.8, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/16k", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/64k", 15, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_2p/0k", 11.5, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_udp", 35.7, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp", 40.3, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp_conn", 71, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_create", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_delete", 9.8942, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_create", 21.9, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_delete", 12, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_mmap_latency", 111500, "microseconds");
            MetricAssert.Exists(metrics, "communications_bandwidth_pipe", 1869, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_af_unix", 3267, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_tcp", 3398, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_file_reread", 5524.9, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mmap_reread", 10200, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(libc)", 4644.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(hand)", 5324.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_reread", 9146, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_write", 7359, "megabytes/sec");
            MetricAssert.Exists(metrics, "memory_latency_mhz", 1344, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l1", 1.452, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l2", 5.061, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_main_mem", 27.1, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_random_mem", 142.8, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_2.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IList<Metric> metrics = parser.ParseResults(results);

            Assert.AreEqual(59, metrics.Count);
            MetricAssert.Exists(metrics, "processor_time_mhz", 2998, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_null_call", 0.28, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_null_i/o", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_stat", 0.92, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_open_close", 1.82, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_slct_tcp", 2.61, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sig_inst", 0.31, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sig_hndl", 1.19, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_fork_proc", 341, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_exec_proc", 857, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sh_proc", 2059, "microseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_bit", 0.25, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_add", 0.17, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_multiply", 0.68, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_mod", 2.76, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_bogo", 0.92, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_divide", 4.01, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_bogo", 1.8, "nanoseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/0k", 9.68, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/16k", 9.12, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/64k", 8.72, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/16k", 9.6, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/64k", 10, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/16k", 10.5, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/64k", 10.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_2p/0k", 9.68, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_pipe", 20.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_af_unix", 18.3, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_udp", 29.8, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp", 33.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp_conn", 25, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_create", 8.2764, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_delete", 5.4662, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_create", 17, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_delete", 9.7852, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_mmap_latency", 11000, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_prot_fault", 0.189, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_page_fault", 1.6182, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_100fd_select", 1.32, "microseconds");
            MetricAssert.Exists(metrics, "communications_bandwidth_pipe", 1857, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_af_unix", 6503, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_tcp", 4401, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_file_reread", 5086.6, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mmap_reread", 11700, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(libc)", 11800, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(hand)", 8952.2, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_reread", 10000, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_write", 23500, "megabytes/sec");
            MetricAssert.Exists(metrics, "memory_latency_mhz", 2998, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l1", 1.335, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l2", 2.206, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_main_mem", 9.256, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_random_mem", 132, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_3.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IList<Metric> metrics = parser.ParseResults(results);

            Assert.AreEqual(59, metrics.Count);
            MetricAssert.Exists(metrics, "processor_time_mhz", 2998, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_null_call", 0.28, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_null_i/o", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_stat", 0.92, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_open_close", 1.8, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_slct_tcp", 2.61, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sig_inst", 0.32, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sig_hndl", 1.21, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_fork_proc", 310, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_exec_proc", 843, "microseconds");
            MetricAssert.Exists(metrics, "processor_time_sh_proc", 1996, "microseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_bit", 0.25, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_add", 0.17, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_multiply", 0.68, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "integer_operations_time_mod", 2.76, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_divide", 2.67, "nanoseconds");
            MetricAssert.Exists(metrics, "float_operations_time_bogo", 0.92, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_add", 0.67, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_multiply", 1, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_divide", 4.01, "nanoseconds");
            MetricAssert.Exists(metrics, "double_operations_time_bogo", 1.8, "nanoseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/0k", 7.74, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/16k", 9.07, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/64k", 8.89, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/16k", 10.1, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/64k", 9.38, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/16k", 9.88, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/64k", 10.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_2p/0k", 7.74, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_pipe", 19.6, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_af_unix", 20.3, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_udp", 30.1, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp", 34.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp_conn", 24, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_create", 8.2808, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_delete", 5.287, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_create", 16.3, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_delete", 8.6565, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_mmap_latency", 11000, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_prot_fault", 0.178, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_page_fault", 1.6219, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_100fd_select", 1.32, "microseconds");
            MetricAssert.Exists(metrics, "communications_bandwidth_pipe", 1988, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_af_unix", 6484, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_tcp", 4311, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_file_reread", 5350.5, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mmap_reread", 12500, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(libc)", 12000, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(hand)", 9079.4, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_reread", 10000, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_write", 23600, "megabytes/sec");
            MetricAssert.Exists(metrics, "memory_latency_mhz", 2998, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l1", 1.335, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l2", 2.247, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_main_mem", 9.063, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_random_mem", 131, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_4_RedHat_Scenario()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_rhel_1.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IList<Metric> metrics = parser.ParseResults(results);

            Assert.AreEqual(31, metrics.Count);
            MetricAssert.Exists(metrics, "processor_time_mhz", 1320, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/0k", 13, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/16k", 12.7, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_2p/64k", 11.3, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/16k", 14.7, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_8p/64k", 15.4, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/16k", 15.3, "microseconds");
            MetricAssert.Exists(metrics, "context_switching_time_16p/64k", 16, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_2p/0k", 13, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_udp", 39.7, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp", 47.2, "microseconds");
            MetricAssert.Exists(metrics, "communications_latency_tcp_conn", 87, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_create", 19.6, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_0k_delete", 15.5, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_create", 32.7, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_10k_delete", 19.4, "microseconds");
            MetricAssert.Exists(metrics, "file_system_latency_mmap_latency", 12500, "microseconds");
            MetricAssert.Exists(metrics, "communications_bandwidth_pipe", 2395, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_af_unix", 5515, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_tcp", 3427, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_file_reread", 5059.8, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mmap_reread", 7510.9, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(libc)", 4839.3, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_bcopy(hand)", 4405.3, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_reread", 6832, "megabytes/sec");
            MetricAssert.Exists(metrics, "communications_bandwidth_mem_write", 5659, "megabytes/sec");
            MetricAssert.Exists(metrics, "memory_latency_mhz", 1320, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l1", 1.617, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_l2", 5.79, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_main_mem", 38.4, "nanoseconds");
            MetricAssert.Exists(metrics, "memory_latency_random_mem", 112.5, "nanoseconds");
        }

        [Test]
        public void LMbenchMetricsParserLeavesOutTheRelatedMetricsWhenTheMemoryMegahertzCannotBeDetermined()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "lmbench_example_results_1.txt");
            string results = File.ReadAllText(outputPath);
            results = results.Replace("1344", "-1  ");

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IList<Metric> metrics = parser.ParseResults(results);

            Assert.AreEqual(29, metrics.Count);
            Assert.IsFalse(metrics.Any(m => m.Name == "memory_latency_mhz"));
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_32()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_32_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 4096);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 35);
                MetricAssert.Exists(metrics, "memory_latency_512b", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.188, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.188, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.215, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 2.649, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 2.649, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 2.648, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 2.647, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 2.645, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 2.647, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 2.677, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 2.748, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 2.761, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 2.762, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 2.763, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 2.771, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 2.775, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 2.828, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 4.432, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 5.199, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 5.31, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 5.33, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 5.332, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 5.333, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 5.346, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 5.334, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 5.324, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 5.342, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 5.345, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 5.352, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "4096mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_64()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_64_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 2048);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 35);
                MetricAssert.Exists(metrics, "memory_latency_512b", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.188, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.189, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.235, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 4.108, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 4.108, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 4.108, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 4.108, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 4.109, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 4.11, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 4.154, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 4.292, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 4.394, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 4.389, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 4.401, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 4.409, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 4.456, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 4.624, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 8.013, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 8.929, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 9.15, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 9.001, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 9.142, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 9.153, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 9.122, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 9.204, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 9.209, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 9.126, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 9.188, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 9.143, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "2048mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_128()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_128_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 8192);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 35);
                MetricAssert.Exists(metrics, "memory_latency_512b", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.189, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 3.92, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 4.152, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 4.152, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 4.152, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 4.151, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 4.234, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 4.254, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 6.022, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 7.488, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 7.533, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 7.531, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 7.58, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 7.514, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 7.858, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 28.626, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 32.136, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 32.523, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 32.737, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 32.652, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 32.852, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 32.832, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 32.921, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 32.88, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 32.848, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 32.844, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 32.861, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "8192mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_256()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_256_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 4096);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 35);
                MetricAssert.Exists(metrics, "memory_latency_512b", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.185, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.185, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 4.148, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 4.15, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 4.149, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 4.151, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 4.15, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 4.321, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 5.322, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 9.691, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 21.383, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 21.043, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 21.053, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 21.434, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 21.602, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 23.366, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 84.172, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 101.579, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 101.957, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 102.328, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 102.561, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 102.537, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 102.594, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 102.591, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 102.469, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 102.451, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 102.432, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 102.455, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "4096mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_512()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_512_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 2048);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 35);
                MetricAssert.Exists(metrics, "memory_latency_512b", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.188, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 4.15, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 4.151, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 4.152, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 4.154, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 4.154, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 4.489, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 5.645, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 10.472, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 22.05, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 22.117, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 22.571, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 22.646, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 22.304, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 22.886, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 45.03, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 67.375, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 74.11, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 92.88, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 95.197, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 95.849, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 95.894, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 96.015, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 96.089, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 96.195, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 96.092, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 96.088, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "2048mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Stride_1024()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_1024_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 2048);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                Assert.IsTrue(metrics.Count == 34);
                MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16kb", 1.186, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48kb", 4.151, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64kb", 4.153, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128kb", 4.15, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192kb", 4.154, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256kb", 4.153, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512kb", 4.82, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768kb", 7.314, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1mb", 11.57, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2mb", 22.248, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3mb", 22.631, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4mb", 22.456, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_8mb", 22.057, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_12mb", 22.495, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_16mb", 22.733, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_32mb", 36.332, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_48mb", 74.374, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_64mb", 78.31, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_128mb", 82.487, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_192mb", 84.274, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_256mb", 84.463, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_512mb", 84.504, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_768mb", 84.762, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_1024mb", 84.878, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_2048mb", 84.918, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_3072mb", 85.264, "nanoseconds");
                MetricAssert.Exists(metrics, "memory_latency_4096mb", 85.393, "nanoseconds");

                Assert.IsTrue(metrics.All(m => m.Metadata.TryGetValue("strideSizeBytes", out IConvertible strideSize) && strideSize.Equals(entry.Key)));
                Assert.IsTrue(metrics.All(m => m.Metadata.ContainsKey("arraySizeBytes")));
                Assert.IsTrue(metrics.All(m => m.Categorization == "2048mb memory"));
            }
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetricsForLatencyMemReadResults_Multiple_Strides()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LMbenchMetricsParserTests.Examples, "latmemrd_stride_128_256_512_1024_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LMbenchMetricsParser parser = new LMbenchMetricsParser();
            IDictionary<int, IList<Metric>> strideMetrics = parser.ParseLatencyMemReadResults(results, 4096);

            foreach (var entry in strideMetrics)
            {
                IList<Metric> metrics = entry.Value;

                switch (entry.Key)
                {
                    case 128:
                        Assert.IsTrue(metrics.Count == 35);

                        // Stride = 128
                        MetricAssert.Exists(metrics, "memory_latency_512b", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32kb", 1.189, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48kb", 3.92, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64kb", 4.152, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128kb", 4.152, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192kb", 4.152, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256kb", 4.151, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512kb", 4.234, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768kb", 4.254, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1mb", 6.022, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2mb", 7.488, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3mb", 7.533, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4mb", 7.531, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8mb", 7.58, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12mb", 7.514, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16mb", 7.858, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32mb", 28.626, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48mb", 32.136, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64mb", 32.523, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128mb", 32.737, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192mb", 32.652, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256mb", 32.852, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512mb", 32.832, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768mb", 32.921, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1024mb", 32.88, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2048mb", 32.848, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3072mb", 32.844, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4096mb", 32.861, "nanoseconds");
                        break;

                    case 256:
                        Assert.IsTrue(metrics.Count == 35);

                        // Stride = 256
                        MetricAssert.Exists(metrics, "memory_latency_512b", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2kb", 1.185, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3kb", 1.185, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48kb", 4.148, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64kb", 4.15, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128kb", 4.149, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192kb", 4.151, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256kb", 4.15, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512kb", 4.321, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768kb", 5.322, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1mb", 9.691, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2mb", 21.383, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3mb", 21.043, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4mb", 21.053, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8mb", 21.434, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12mb", 21.602, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16mb", 23.366, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32mb", 84.172, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48mb", 101.579, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64mb", 101.957, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128mb", 102.328, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192mb", 102.561, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256mb", 102.537, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512mb", 102.594, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768mb", 102.591, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1024mb", 102.469, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2048mb", 102.451, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3072mb", 102.432, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4096mb", 102.455, "nanoseconds");
                        break;

                    case 512:
                        Assert.IsTrue(metrics.Count == 35);

                        // Stride = 512
                        MetricAssert.Exists(metrics, "memory_latency_512b", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4kb", 1.188, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48kb", 4.15, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64kb", 4.151, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128kb", 4.152, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192kb", 4.154, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256kb", 4.154, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512kb", 4.489, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768kb", 5.645, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1mb", 10.472, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2mb", 22.05, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3mb", 22.117, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4mb", 22.571, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8mb", 22.646, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12mb", 22.304, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16mb", 22.886, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32mb", 45.03, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48mb", 67.375, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64mb", 74.11, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128mb", 92.88, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192mb", 95.197, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256mb", 95.849, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512mb", 95.894, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768mb", 96.015, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1024mb", 96.089, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2048mb", 96.195, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3072mb", 96.092, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4096mb", 96.088, "nanoseconds");
                        break;

                    case 1024:
                        Assert.IsTrue(metrics.Count == 34);

                        // Stride = 1024
                        MetricAssert.Exists(metrics, "memory_latency_1kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16kb", 1.186, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32kb", 1.187, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48kb", 4.151, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64kb", 4.153, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128kb", 4.15, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192kb", 4.154, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256kb", 4.153, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512kb", 4.82, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768kb", 7.314, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1mb", 11.57, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2mb", 22.248, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3mb", 22.631, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4mb", 22.456, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_8mb", 22.057, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_12mb", 22.495, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_16mb", 22.733, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_32mb", 36.332, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_48mb", 74.374, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_64mb", 78.31, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_128mb", 82.487, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_192mb", 84.274, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_256mb", 84.463, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_512mb", 84.504, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_768mb", 84.762, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_1024mb", 84.878, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_2048mb", 84.918, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_3072mb", 85.264, "nanoseconds");
                        MetricAssert.Exists(metrics, "memory_latency_4096mb", 85.393, "nanoseconds");
                        break;
                }
            }
        }
    }
}