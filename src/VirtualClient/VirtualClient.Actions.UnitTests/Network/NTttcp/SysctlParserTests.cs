// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Identity.Client;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class SysctlParserTests
    {
        private static readonly string ExamplesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(SysctlParserTests)).Location),
            "Examples",
            "Ntttcp");

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
