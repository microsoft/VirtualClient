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

            this.teragenTestParser.Parse();
            this.terasortTestParser.Parse();
        }

        [Test]
        public void HadoopTerasortParserVerifySingleCoreResult()
        {
            this.teragenTestParser.JobCounters.PrintDataTableFormatted();
            this.terasortTestParser.JobCounters.PrintDataTableFormatted();

            Assert.AreEqual(2, this.teragenTestParser.JobCounters.Columns.Count);
            Assert.AreEqual(2, this.terasortTestParser.JobCounters.Columns.Count);
        }
    }
}