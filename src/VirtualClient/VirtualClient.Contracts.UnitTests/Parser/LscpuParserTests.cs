// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class LscpuParserTests
    {
        [Test]
        public void LscpuParserParsesTheExpectedResultsFromIntelSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_intel_1.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 6, GenuineIntel", info.Description);
            Assert.AreEqual(4, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 163840));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 98304));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 65536));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 2621440));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 50331648));
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsFromIntelSystems_Scenario2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_intel_2.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz", info.Name);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 6, GenuineIntel", info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 229376));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 131072));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 98304));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 8650752));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 67108864));
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsFromAmpereSystems_Scenario1()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_ampere_1.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Neoverse-N1", info.Name);
            Assert.AreEqual("Neoverse-N1 Model 1 Stepping r3p1, ARM", info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 262144));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 131072));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 131072));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 2097152));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 33554432));
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsFromAWSSystems_Scenario3()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results3.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Model 1 Stepping r1p1, ARM", info.Name);
            Assert.AreEqual("Model 1 Stepping r1p1, ARM", info.Description);
            Assert.AreEqual(2, info.LogicalCoreCount);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 262144));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 131072));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 131072));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 2097152));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 33554432));
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsSystems_Scenario3()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_intel_multpleNUMAnodes.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8480C", info.Name);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8480C Family 6 Model 143 Stepping 8, GenuineIntel", info.Description);
            Assert.AreEqual(224, info.LogicalCoreCount);
            Assert.AreEqual(112, info.PhysicalCoreCount);
            Assert.AreEqual(2, info.SocketCount);
            Assert.AreEqual(2, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 9227468));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 5557452));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 3670016));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 234881024));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 220200960));
        }
    }
}
