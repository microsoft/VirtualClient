// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LspciParserUnitTests
    {
        [Test]
        public void LspciParserParsesPciDevicesCorrectly_Scenario1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "lspci", "linux-1.txt");
            string rawText = File.ReadAllText(outputPath);

            LspciParser testParser = new LspciParser(rawText);
            IList<PciDevice> pciDevices = testParser.Parse();

            Assert.AreEqual(2, pciDevices.Count);

            Assert.AreEqual(pciDevices[0].Address, "0001:00:00.0");
            Assert.AreEqual(pciDevices[0].Name, "3D controller: NVIDIA Corporation TU104GL [Tesla T4] (rev a1)");
            Assert.AreEqual(pciDevices[0].Properties["Status"], "Cap+ 66MHz- UDF- FastB2B- ParErr- DEVSEL=fast >TAbort- <TAbort- <MAbort- >SERR- <PERR- INTx-");
            Assert.AreEqual(pciDevices[0].Capabilities[0].Name, "[60] Power Management version 3");
            Assert.AreEqual(pciDevices[0].Capabilities[0].Properties["Status"], "D0 NoSoftRst- PME-Enable- DSel=0 DScale=0 PME+");

            Assert.AreEqual(pciDevices[1].Address, "c93f:00:02.0");
            Assert.AreEqual(pciDevices[1].Name, "Ethernet controller: Mellanox Technologies MT27710 Family [ConnectX-4 Lx Virtual Function] (rev 80)");
            Assert.AreEqual(pciDevices[1].Properties["Region 0"], "Memory at ff2000000 (64-bit, prefetchable) [size=1M]");

        }

        [Test]
        public void LspciParserParsesPciDevicesCorrectly_Scenario2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "lspci", "linux-2.txt");
            string rawText = File.ReadAllText(outputPath);

            LspciParser testParser = new LspciParser(rawText);
            IList<PciDevice> pciDevices = testParser.Parse();

            Assert.AreEqual(31, pciDevices.Count);
        }

        [Test]
        public void LspciParserParsesPciDevicesCorrectly_Scenario3()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "lspci", "windows-1.txt");
            string rawText = File.ReadAllText(outputPath);

            LspciParser testParser = new LspciParser(rawText);
            IList<PciDevice> pciDevices = testParser.Parse();

            Assert.AreEqual(29, pciDevices.Count);

            Assert.AreEqual(pciDevices[1].Address, "00:04.0");
            Assert.AreEqual(pciDevices[1].Name, "System peripheral: Intel Corporation Sky Lake-E CBDMA Registers (rev 07)");
            Assert.AreEqual(pciDevices[1].Properties["Latency"], "0, Cache Line Size: 64 bytes");
            Assert.AreEqual(pciDevices[1].Capabilities[2].Name, "[e0] Power Management version 3");
            Assert.AreEqual(pciDevices[1].Capabilities[2].Properties["Flags"], "PMEClk- DSI- D1- D2- AuxCurrent=0mA PME(D0-,D1-,D2-,D3hot-,D3cold-)");
        }
    }
}