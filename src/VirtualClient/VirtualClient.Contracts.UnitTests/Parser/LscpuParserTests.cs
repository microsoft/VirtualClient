// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class LscpuParserTests
    {
        [Test]
        public void LscpuParserParsesTheExpectedResultsFromIntelSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.IsNull(info.Description);
            Assert.AreEqual(4, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsFromIntelSystems_Scenario2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_2.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.IsNull(info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
        }
    }
}
