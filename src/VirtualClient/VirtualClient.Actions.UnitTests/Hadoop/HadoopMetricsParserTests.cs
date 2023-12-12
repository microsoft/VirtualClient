// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class HadoopParserUnitTests
    {
        private string teragentResultRawText;
        private string terasortResultRawText;
        private HadoopMetricsParser teragenTestParser;
        private HadoopMetricsParser terasortTestParser;
        private IList<Metric> teragenMetrics;
        private IList<Metric> terasortMetrics;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string teragenOutputPath = Path.Combine(workingDirectory, @"Examples\Hadoop\HadoopTeragenExample.txt");
            string terasortOutputPath = Path.Combine(workingDirectory, @"Examples\Hadoop\HadoopTerasortExample.txt");

            this.teragentResultRawText = File.ReadAllText(teragenOutputPath);
            this.terasortResultRawText = File.ReadAllText(terasortOutputPath);

            this.teragenTestParser = new HadoopMetricsParser(this.teragentResultRawText);
            this.terasortTestParser = new HadoopMetricsParser(this.terasortResultRawText);
        }

        [Test]
        public void HadoopTerasortParserVerifySingleCoreResult()
        {
            this.teragenMetrics = this.teragenTestParser.Parse();
            this.terasortMetrics = this.terasortTestParser.Parse();

            this.teragenTestParser.JobCounters.PrintDataTableFormatted();
            this.terasortTestParser.JobCounters.PrintDataTableFormatted();

            Assert.AreEqual(4, this.teragenTestParser.JobCounters.Columns.Count);
            Assert.AreEqual(4, this.terasortTestParser.JobCounters.Columns.Count);
        }

        [Test]
        public void HadoopTeragenResultsParserCreatesTheExpectedMetricsFromResults()
        {
            this.teragenMetrics = this.teragenTestParser.Parse();
            MetricAssert.Exists(this.teragenMetrics, "FILE: Number of bytes read", 0, "bytes");
            MetricAssert.Exists(this.teragenMetrics, "FILE: Number of bytes written", 551354, "bytes");
            MetricAssert.Exists(this.teragenMetrics, "Launched map tasks", 2, "count");
            MetricAssert.Exists(this.teragenMetrics, "Total time spent by all maps in occupied slots (ms)", 7863, "ms");
            MetricAssert.Exists(this.teragenMetrics, "Total time spent by all reduces in occupied slots (ms)", 0, "ms");
            MetricAssert.Exists(this.teragenMetrics, "Map input records", 100, "count");
            MetricAssert.Exists(this.teragenMetrics, "GC time elapsed (ms)", 45, "ms");
            MetricAssert.Exists(this.teragenMetrics, "Physical memory (bytes) snapshot", 439705600, "bytes");
            MetricAssert.Exists(this.teragenMetrics, "Peak Map Virtual memory (bytes)", 2732146688, "bytes");
            MetricAssert.Exists(this.teragenMetrics, "CHECKSUM", 233519182817, "count");
            MetricAssert.Exists(this.teragenMetrics, "Bytes Read", 0, "bytes");
            MetricAssert.Exists(this.teragenMetrics, "Bytes Written", 10000, "bytes");
            MetricAssert.Equals(this.teragenMetrics.Count, 34);
        }

        [Test]
        public void HadoopTerasortResultsParserCreatesTheExpectedMetricsFromResults()
        {
            this.terasortMetrics = this.terasortTestParser.Parse();
            MetricAssert.Exists(this.terasortMetrics, "FILE: Number of bytes read", 245, "bytes");
            MetricAssert.Exists(this.terasortMetrics, "FILE: Number of bytes written", 2452, "bytes");
            MetricAssert.Exists(this.terasortMetrics, "FILE: Number of read operations", 0, "count");
            MetricAssert.Exists(this.terasortMetrics, "HDFS: Number of read operations", 21, "count");
            MetricAssert.Exists(this.terasortMetrics, "Launched map tasks", 2, "count");
            MetricAssert.Exists(this.terasortMetrics, "Total time spent by all maps in occupied slots (ms)", 13432, "ms");
            MetricAssert.Exists(this.terasortMetrics, "Total time spent by all reduces in occupied slots (ms)", 23423, "ms");
            MetricAssert.Exists(this.terasortMetrics, "Map input records", 122, "count");
            MetricAssert.Exists(this.terasortMetrics, "GC time elapsed (ms)", 1341, "ms");
            MetricAssert.Exists(this.terasortMetrics, "Physical memory (bytes) snapshot", 13412341, "bytes");
            MetricAssert.Exists(this.terasortMetrics, "Peak Map Virtual memory (bytes)", 24387234, "bytes");
            MetricAssert.Exists(this.terasortMetrics, "BAD_ID", 0, "count");
            MetricAssert.Exists(this.terasortMetrics, "Bytes Read", 284389, "bytes");
            MetricAssert.Exists(this.terasortMetrics, "Bytes Written", 18374, "bytes");
            MetricAssert.Equals(this.terasortMetrics.Count, 54);
        }
    }
}
