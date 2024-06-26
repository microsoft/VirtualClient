// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    class SysctlParserTests
    {
        private static readonly string ExamplesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(SysctlParserTests)).Location),
            "Examples",
            "NTttcp");

        [Test]
        public void SysctlResultsParserReadsTheExpectedResultsFromParsedOutput()
        {
            string results = File.ReadAllText(Path.Combine(SysctlParserTests.ExamplesDirectory, "sysctlExampleOutput.txt"));

            SysctlParser parser = new SysctlParser(results);
            string finalResults = parser.Parse();

            Assert.IsNotNull(finalResults);
            Assert.IsNotEmpty(finalResults);
        }

        [Test]
        public void SysctlResultsParserOutputsTheExpectedResults()
        {
            string results = File.ReadAllText(Path.Combine(SysctlParserTests.ExamplesDirectory, "sysctlExampleOutput.txt"));

            SysctlParser parser = new SysctlParser(results);
            string finalResults = parser.Parse();

            Assert.AreEqual(finalResults, "{\"net.ipv4.tcp_rmem \":\"4096 131072 6291456\",\"net.ipv4.tcp_wmem \":\"4096 16384 4194304\"}");
        }
    }
}
