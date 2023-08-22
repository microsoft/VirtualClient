// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class DmiDecodeParserTests
    {
        private static readonly string DmiDecodeExamples = Path.Combine(MockFixture.TestResourcesDirectory, "Unix", "dmidecode");

        [Test]
        public void DmiDecodeMemoryParserParsesTheExpectedResultsFromBareMetalSystems_Scenario_1()
        {
            string results = File.ReadAllText(Path.Combine(DmiDecodeParserTests.DmiDecodeExamples, "dmidecode_memory_ubuntu_physical.txt"));
            DmiDecodeParser parser = new DmiDecodeParser();

            Assert.IsTrue(parser.TryParse(results, out IEnumerable <MemoryChipInfo> memoryChips));
            Assert.IsNotNull(memoryChips);
            Assert.IsTrue(memoryChips.Count() == 1);

            // Memory Chip #1
            MemoryChipInfo chip = memoryChips.ElementAt(0);
            Assert.AreEqual("Memory_1", chip.Name);
            Assert.AreEqual("Micron Memory Chip", chip.Description);
            Assert.AreEqual("Micron", chip.Manufacturer);
            Assert.AreEqual("16KTF1G64HZ-1G9P1", chip.PartNumber);
            Assert.AreEqual(8589934592, chip.Capacity);
            Assert.AreEqual(1866, chip.Speed);
        }

        [Test]
        public void DmiDecodeMemoryParserParsesTheExpectedResultsFromBareMetalSystems_Scenario_2()
        {
            string results = File.ReadAllText(Path.Combine(DmiDecodeParserTests.DmiDecodeExamples, "dmidecode_memory_ubuntu_physical_2.txt"));
            DmiDecodeParser parser = new DmiDecodeParser();

            Assert.IsTrue(parser.TryParse(results, out IEnumerable<MemoryChipInfo> memoryChips));
            Assert.IsNotNull(memoryChips);
            Assert.IsTrue(memoryChips.Count() == 2);

            // Memory Chip #1
            MemoryChipInfo chip = memoryChips.ElementAt(0);
            Assert.AreEqual("Memory_1", chip.Name);
            Assert.AreEqual("HK Hynix Memory Chip", chip.Description);
            Assert.AreEqual("HK Hynix", chip.Manufacturer);
            Assert.AreEqual("HMA81GS6JJR8N-VK", chip.PartNumber);
            Assert.AreEqual(4294967296, chip.Capacity);
            Assert.AreEqual(1868, chip.Speed);

            // Memory Chip #2
            chip = memoryChips.ElementAt(1);
            Assert.AreEqual("Memory_2", chip.Name);
            Assert.AreEqual("Micron Memory Chip", chip.Description);
            Assert.AreEqual("Micron", chip.Manufacturer);
            Assert.AreEqual("16KTF1G64HZ-1G9P1", chip.PartNumber);
            Assert.AreEqual(8589934592, chip.Capacity);
            Assert.AreEqual(1866, chip.Speed);
        }

        [Test]
        public void DmiDecodeMemoryParserParsesTheExpectedResultsFromVMSystems()
        {
            string results = File.ReadAllText(Path.Combine(DmiDecodeParserTests.DmiDecodeExamples, "dmidecode_memory_ubuntu_vm.txt"));
            DmiDecodeParser parser = new DmiDecodeParser();

            // VM systems do not present enough information to determine the details for the 
            // manufacturer of the chipset.
            Assert.IsFalse(parser.TryParse(results, out IEnumerable<MemoryChipInfo> memoryChips));
        }
    }
}
