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
        private string rawText;
        private HadoopMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Hadoop\HadoopTeragenExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new HadoopMetricsParser(this.rawText);
            this.testParser.Parse();
        }

        [Test]
        public void HadoopTerasortParserVerifySingleCoreResult()
        {
            this.testParser.FileSystemCounters.PrintDataTableFormatted();

            Assert.AreEqual(4, this.testParser.FileSystemCounters.Columns.Count);
        }
    }
}