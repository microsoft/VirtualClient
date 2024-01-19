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
            Assert.AreEqual(4, info.LogicalProcessorCount);
            Assert.AreEqual(2, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
            Assert.AreEqual(double.NaN, info.MaxFrequencyMHz);
            Assert.AreEqual(double.NaN, info.MinFrequencyMHz);
            Assert.AreEqual(2793.438, info.FrequencyMHz);

            Assert.AreEqual(7, info.Flags.Count);
            Assert.AreEqual("x86_64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("46 bits physical, 48 bits virtual", info.Flags["Address sizes"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0-3", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("0-3", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("5586.87", info.Flags["BogoMIPS"]);

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
            Assert.AreEqual(2, info.LogicalProcessorCount);
            Assert.AreEqual(1, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
            Assert.AreEqual(double.NaN, info.MaxFrequencyMHz);
            Assert.AreEqual(double.NaN, info.MinFrequencyMHz);
            Assert.AreEqual(2793.438, info.FrequencyMHz);

            Assert.AreEqual(7, info.Flags.Count);
            Assert.AreEqual("x86_64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("46 bits physical, 48 bits virtual", info.Flags["Address sizes"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0-3", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("0-3", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("5586.87", info.Flags["BogoMIPS"]);

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
            Assert.AreEqual(2, info.LogicalProcessorCount);
            Assert.AreEqual(1, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
            Assert.AreEqual(double.NaN, info.MaxFrequencyMHz);
            Assert.AreEqual(double.NaN, info.MinFrequencyMHz);
            Assert.AreEqual(double.NaN, info.FrequencyMHz);

            Assert.AreEqual(6, info.Flags.Count);
            Assert.AreEqual("aarch64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0,1", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("0,1", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("50", info.Flags["BogoMIPS"]);

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
        public void LscpuParserParsesTheExpectedResultsFromAmpereSystems_Scenario2()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_ampere_2.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Neoverse-N1", info.Name);
            Assert.AreEqual("Neoverse-N1 Model 1 Stepping r3p1, ARM", info.Description);
            Assert.AreEqual(64, info.LogicalProcessorCount);
            Assert.AreEqual(1, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(64, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
            Assert.AreEqual(double.NaN, info.MaxFrequencyMHz);
            Assert.AreEqual(double.NaN, info.MinFrequencyMHz);
            Assert.AreEqual(double.NaN, info.FrequencyMHz);

            Assert.AreEqual(6, info.Flags.Count);
            Assert.AreEqual("aarch64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0-63", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("0-63", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("50", info.Flags["BogoMIPS"]);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 8388608));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 4194304));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 4194304));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 67108864));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 33554432));
        }

        [Test]
        public void LscpuParserParsesTheExpectedResultsFromAWSSystems_Scenario3()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_intel_3.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Model 1 Stepping r1p1, ARM", info.Name);
            Assert.AreEqual("Model 1 Stepping r1p1, ARM", info.Description);
            Assert.AreEqual(2, info.LogicalProcessorCount);
            Assert.AreEqual(1, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(2, info.PhysicalCoreCount);
            Assert.AreEqual(1, info.SocketCount);
            Assert.AreEqual(1, info.NumaNodeCount);
            Assert.IsFalse(info.IsHyperthreadingEnabled);
            Assert.AreEqual(double.NaN, info.MaxFrequencyMHz);
            Assert.AreEqual(double.NaN, info.MinFrequencyMHz);
            Assert.AreEqual(double.NaN, info.FrequencyMHz);

            Assert.AreEqual(6, info.Flags.Count);
            Assert.AreEqual("aarch64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0,1", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("0,1", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("2100", info.Flags["BogoMIPS"]);

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
        public void LscpuParserParsesTheExpectedResultsIntelLabSystems_Scenario4()
        {
            string results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "lscpu", "lscpu_results_intel_4.txt"));
            LscpuParser parser = new LscpuParser(results);
            CpuInfo info = parser.Parse();

            Assert.IsNotNull(info);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8480C", info.Name);
            Assert.AreEqual("Intel(R) Xeon(R) Platinum 8480C Family 6 Model 143 Stepping 8, GenuineIntel", info.Description);
            Assert.AreEqual(28, info.LogicalProcessorCount);
            Assert.AreEqual(2, info.LogicalProcessorCountPerPhysicalCore);
            Assert.AreEqual(14, info.PhysicalCoreCount);
            Assert.AreEqual(2, info.SocketCount);
            Assert.AreEqual(2, info.NumaNodeCount);
            Assert.IsTrue(info.IsHyperthreadingEnabled);
            Assert.AreEqual(380, info.MaxFrequencyMHz);
            Assert.AreEqual(80, info.MinFrequencyMHz);
            Assert.AreEqual(double.NaN, info.FrequencyMHz);

            Assert.AreEqual(8, info.Flags.Count);
            Assert.AreEqual("x86_64", info.Flags["Architecture"]);
            Assert.AreEqual("32-bit, 64-bit", info.Flags["CPU op-mode(s)"]);
            Assert.AreEqual("46 bits physical, 57 bits virtual", info.Flags["Address sizes"]);
            Assert.AreEqual("Little Endian", info.Flags["Byte Order"]);
            Assert.AreEqual("0-5,18-23", info.Flags["NUMA node0 CPU(s)"]);
            Assert.AreEqual("6-17,24-27", info.Flags["NUMA node1 CPU(s)"]);
            Assert.AreEqual("0-27", info.Flags["On-line CPU(s) list"]);
            Assert.AreEqual("4000", info.Flags["BogoMIPS"]);

            IConvertible cacheMemory = 0;
            Assert.IsNotEmpty(info.Caches);

            Assert.IsTrue(info.Caches.Count() == 5);
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1" && cache.SizeInBytes == 7130316));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1d" && cache.SizeInBytes == 4508876));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L1i" && cache.SizeInBytes == 2621440));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L2" && cache.SizeInBytes == 25165824));
            Assert.IsTrue(info.Caches.Any(cache => cache.Name == "L3" && cache.SizeInBytes == 20971520));
        }
    }
}
