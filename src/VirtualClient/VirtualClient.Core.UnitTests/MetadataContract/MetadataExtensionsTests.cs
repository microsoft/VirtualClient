// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MetadataExtensionsTests
    {
        private static readonly string LspciExamples = Path.Combine(MockFixture.TestResourcesDirectory, "Unix", "lspci");
        private MockFixture mockFixture;

        public void SetupFixture(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            mockFixture = new MockFixture();
            mockFixture.Setup(platform, architecture);
        }

        [Test]
        public async Task GetCpuMetadataExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_1()
        {
            SetupFixture(PlatformID.Win32NT);

            CpuInfo cpuInfo = new CpuInfo(
                name: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                description: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel",
                physicalCoreCount: 4,
                logicalCoreCount: 8,
                socketCount: 2,
                numaNodeCount: 1,
                true,
                // No CPU cache information
                caches: null);

            mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetCpuMetadataAsync();
            Assert.IsTrue(metadata.Count == 12);

            object value;
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuVendor", out value) && value.ToString() == "Intel");
            Assert.IsTrue(metadata.TryGetValue("cpuFamily", out value) && value.ToString() == "6");
            Assert.IsTrue(metadata.TryGetValue("cpuStepping", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuModel", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz");
            Assert.IsTrue(metadata.TryGetValue("cpuModelDescription", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel");
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
        }

        [Test]
        public async Task GetCpuMetadataExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_2()
        {
            SetupFixture(PlatformID.Win32NT);

            CpuInfo cpuInfo = new CpuInfo(
                name: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                description: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel",
                physicalCoreCount: 4,
                logicalCoreCount: 8,
                socketCount: 2,
                numaNodeCount: 1,
                true,
                new List<CpuCacheInfo>
                {
                    new CpuCacheInfo("L1", null, 100000),
                    new CpuCacheInfo("L1d", null, 60000),
                    new CpuCacheInfo("L1i", null, 40000),
                    new CpuCacheInfo("L2", null, 10000000),
                    new CpuCacheInfo("L3", null, 80000000)
                });

            mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetCpuMetadataAsync();
            Assert.IsTrue(metadata.Count == 18);

            object value;
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuVendor", out value) && value.ToString() == "Intel");
            Assert.IsTrue(metadata.TryGetValue("cpuFamily", out value) && value.ToString() == "6");
            Assert.IsTrue(metadata.TryGetValue("cpuStepping", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuModel", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz");
            Assert.IsTrue(metadata.TryGetValue("cpuModelDescription", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel");
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1d", out value) && value.ToString() == "60000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1i", out value) && value.ToString() == "40000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L3", out value) && value.ToString() == "80000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "80000000");
        }

        [Test]
        public async Task GetCpuMetadataExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_3()
        {
            SetupFixture(PlatformID.Win32NT);

            CpuInfo cpuInfo = new CpuInfo(
                name: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                description: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                physicalCoreCount: 4,
                logicalCoreCount: 8,
                socketCount: 2,
                numaNodeCount: 1,
                true,
                new List<CpuCacheInfo>
                {
                    new CpuCacheInfo("L1", null, 100000),
                    new CpuCacheInfo("L2", null, 10000000),
                    new CpuCacheInfo("L3", null, 80000000)
                });

            mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetCpuMetadataAsync();
            Assert.IsTrue(metadata.Count == 16);

            object value;
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuVendor", out value) && value.ToString() == "Intel");
            Assert.IsTrue(metadata.TryGetValue("cpuModel", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz");
            Assert.IsTrue(metadata.TryGetValue("cpuModelDescription", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz");
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L3", out value) && value.ToString() == "80000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "80000000");

            Assert.IsTrue(metadata.TryGetValue("cpuFamily", out value) && value == null);
            Assert.IsTrue(metadata.TryGetValue("cpuStepping", out value) && value == null);
        }

        [Test]
        public async Task GetCpuMetadataExtensionReturnsTheExpectedMetadataContractInformation_AMD_Scenario_1()
        {
            SetupFixture(PlatformID.Win32NT);

            CpuInfo cpuInfo = new CpuInfo(
                name: "AMD EPYC 7V12 64-Core Processor",
                description: "AMD EPYC 7V12 64-Core Processor Family 6 Model 106 Stepping 2",
                physicalCoreCount: 4,
                logicalCoreCount: 8,
                socketCount: 2,
                numaNodeCount: 1,
                true,
                // No CPU cache information
                caches: null);

            mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetCpuMetadataAsync();
            Assert.IsTrue(metadata.Count == 12);

            object value;
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuVendor", out value) && value.ToString() == "AMD");
            Assert.IsTrue(metadata.TryGetValue("cpuFamily", out value) && value.ToString() == "6");
            Assert.IsTrue(metadata.TryGetValue("cpuStepping", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuModel", out value) && value.ToString() == "AMD EPYC 7V12 64-Core Processor");
            Assert.IsTrue(metadata.TryGetValue("cpuModelDescription", out value) && value.ToString() == "AMD EPYC 7V12 64-Core Processor Family 6 Model 106 Stepping 2");
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
        }

        [Test]
        public async Task GetCpuMetadataExtensionReturnsTheExpectedMetadataContractInformation_Ampere_Scenario_1()
        {
            SetupFixture(PlatformID.Win32NT, Architecture.Arm64);

            CpuInfo cpuInfo = new CpuInfo(
                name: "Ampere(R) Altra(R) Processor",
                description: "ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)",
                physicalCoreCount: 64,
                logicalCoreCount: 64,
                socketCount: 2,
                numaNodeCount: 2,
                true,
                new List<CpuCacheInfo>
                {
                    new CpuCacheInfo("L1", null, 100000),
                    new CpuCacheInfo("L2", null, 10000000)
                });

            mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetCpuMetadataAsync();
            Assert.IsTrue(metadata.Count == 15);

            object value;
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "ARM64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuVendor", out value) && value.ToString() == "ARM");
            Assert.IsTrue(metadata.TryGetValue("cpuModel", out value) && value.ToString() == "Ampere(R) Altra(R) Processor");
            Assert.IsTrue(metadata.TryGetValue("cpuModelDescription", out value) && value.ToString() == "ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)");
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "10000000");

            Assert.IsTrue(metadata.TryGetValue("cpuFamily", out value) && value.ToString() == "8");
            Assert.IsTrue(metadata.TryGetValue("cpuStepping", out value) && value == null);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task GetHostMetadataReturnsTheExpectedMetadataContractInformation_Windows_Scenario(PlatformID platform, Architecture architecture)
        {
            SetupFixture(platform, architecture);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 6);

            object value;
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value) && value.ToString() == Environment.OSVersion.VersionString);
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value) && value.ToString() == Environment.OSVersion.Version.ToString());
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == PlatformSpecifics.GetPlatformArchitectureName(platform, architecture));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task GetHostMetadataReturnsTheExpectedMetadataContractInformation_Unix_Scenario(PlatformID platform, Architecture architecture)
        {
            SetupFixture(platform, architecture);

            this.mockFixture.SystemManagement.Setup(mgmt => mgmt.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LinuxDistributionInfo
                {
                    LinuxDistribution = LinuxDistribution.Ubuntu,
                    OperationSystemFullName = "Ubuntu 20.01 build 1234"
                });

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 6);

            object value;
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Unix");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Ubuntu");
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == PlatformSpecifics.GetPlatformArchitectureName(platform, architecture));

            // Cannot test these when running on a Windows system
            // Assert.IsTrue(metadata.TryGetValue("osDescription", out value) && value.ToString() == Environment.OSVersion.VersionString);
            // Assert.IsTrue(metadata.TryGetValue("osVersion", out value) && value.ToString() == Environment.OSVersion.Version.ToString());
        }

        [Test]
        public async Task GetMemoryMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_1()
        {
            SetupFixture(PlatformID.Win32NT);

            // No memory chip sets available
            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetMemoryMetadataAsync();
            Assert.IsTrue(metadata.Count == 1);

            object value;
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == "123456789");
        }

        [Test]
        public async Task GetMemoryMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_2()
        {
            SetupFixture(PlatformID.Win32NT);

            // Physical memory chip information present.
            MemoryInfo memoryInfo = new MemoryInfo(
                123456789,
                new List<MemoryChipInfo>
                {
                    new MemoryChipInfo("Memory_1", "Memory", 123456789, 2166, "HK Hynix", "HM123456"),
                    new MemoryChipInfo("Memory_2", "Memory", 223344556, 2432, "Micron", "M987654")
                });

            mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetMemoryMetadataAsync();
            Assert.IsTrue(metadata.Count == 9);

            object value;
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == "123456789");
            Assert.IsTrue(metadata.TryGetValue("memoryManufacturerChip1", out value) && value.ToString() == "HK Hynix");
            Assert.IsTrue(metadata.TryGetValue("memoryBytesChip1", out value) && value.ToString() == "123456789");
            Assert.IsTrue(metadata.TryGetValue("memorySpeedChip1", out value) && value.ToString() == "2166");
            Assert.IsTrue(metadata.TryGetValue("memoryPartNumberChip1", out value) && value.ToString() == "HM123456");
            Assert.IsTrue(metadata.TryGetValue("memoryManufacturerChip2", out value) && value.ToString() == "Micron");
            Assert.IsTrue(metadata.TryGetValue("memoryBytesChip2", out value) && value.ToString() == "223344556");
            Assert.IsTrue(metadata.TryGetValue("memorySpeedChip2", out value) && value.ToString() == "2432");
            Assert.IsTrue(metadata.TryGetValue("memoryPartNumberChip2", out value) && value.ToString() == "M987654");
        }

        [Test]
        public async Task GetNetworkInterfaceMetadataReturnsTheExpectedMetadataContractInformation_Unix_Scenario_1()
        {
            SetupFixture(PlatformID.Unix);

            string lspciOutput = File.ReadAllText(Path.Combine(MetadataExtensionsTests.LspciExamples, "lspci_physical_system_1.txt"));
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => process.StandardOutput.Append(lspciOutput);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetNetworkInterfaceMetadataAsync();
            Assert.IsTrue(metadata.Count == 3);

            object value;
            Assert.IsTrue(metadata.TryGetValue("networkInterface1", out value) && value.ToString() == "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)");
            Assert.IsTrue(metadata.TryGetValue("networkInterface2", out value) && value.ToString() == "Realtek Semiconductor Co., Ltd. RTL810xE PCI Express Fast Ethernet controller (rev 07)");
            Assert.IsTrue(metadata.TryGetValue("networkAccelerationEnabled", out value) && (bool)value == false);
        }

        [Test]
        public async Task GetNetworkInterfaceMetadataReturnsTheExpectedMetadataContractInformation_Unix_Scenario_2()
        {
            SetupFixture(PlatformID.Unix);

            string lspciOutput = File.ReadAllText(Path.Combine(MetadataExtensionsTests.LspciExamples, "lspci_physical_system_mellanox.txt"));
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => process.StandardOutput.Append(lspciOutput);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetNetworkInterfaceMetadataAsync();
            Assert.IsTrue(metadata.Count == 3);

            object value;
            Assert.IsTrue(metadata.TryGetValue("networkInterface1", out value) && value.ToString() == "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)");
            Assert.IsTrue(metadata.TryGetValue("networkInterface2", out value) && value.ToString() == "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)");
            Assert.IsTrue(metadata.TryGetValue("networkAccelerationEnabled", out value) && (bool)value == true);
        }

        [Test]
        public async Task GetNetworkInterfaceMetadataReturnsTheExpectedMetadataContractInformation_Unix_Scenario_3()
        {
            SetupFixture(PlatformID.Unix);

            string lspciOutput = File.ReadAllText(Path.Combine(MetadataExtensionsTests.LspciExamples, "lspci_vm_mellanox.txt"));
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => process.StandardOutput.Append(lspciOutput);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetNetworkInterfaceMetadataAsync();
            Assert.IsTrue(metadata.Count == 2);

            object value;
            Assert.IsTrue(metadata.TryGetValue("networkInterface1", out value) && value.ToString() == "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)");
            Assert.IsTrue(metadata.TryGetValue("networkAccelerationEnabled", out value) && (bool)value == true);
        }
    }
}
