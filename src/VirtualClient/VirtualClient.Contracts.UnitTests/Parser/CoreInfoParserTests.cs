// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class CoreInfoParserTests
    {
        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromIntelSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_Intel.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.AreEqual("Intel64 Family 6 Model 106 Stepping 6, GenuineIntel", info.Description);
            Assert.AreEqual(4, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(0, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromIntelSystems_Scenario2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_Intel_2.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.AreEqual("Intel64 Family 6 Model 106 Stepping 6, GenuineIntel", info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(0, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromIntelSystems_Scenario3()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_Intel_3.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.AreEqual("Intel64 Family 6 Model 106 Stepping 6, GenuineIntel", info.Description);
            Assert.AreEqual(8, info.LogicalCoreCount);
            Assert.AreEqual(4, info.PhysicalCoreCount);
            Assert.AreEqual(2, info.SocketCount);
            Assert.AreEqual(2, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromAMDSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_AMD.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("AMD EPYC 7452 32-Core Processor", info.Name);
            Assert.AreEqual("AMD64 Family 23 Model 49 Stepping 0, AuthenticAMD", info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(1, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(0, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromAmpereSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_Ampere.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Ampere(R) Altra(R) Processor", info.Name);
            Assert.AreEqual("ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)", info.Description);
            Assert.AreEqual(4, info.LogicalCoreCount);
            Assert.AreEqual(4, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
        }

        [Test]
        public void CoreInfoParserParsesTheExpectedResultsFromAmpereSystems_Scenario2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "CoreInfo", "CoreInfo_Results_Ampere_2.txt"));
            CoreInfoParser parser = new CoreInfoParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Ampere(R) Altra(R) Processor", info.Name);
            Assert.AreEqual("ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)", info.Description);
            Assert.AreEqual(16, info.LogicalCoreCount);
            Assert.AreEqual(16, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(0, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
        }
    }
}
