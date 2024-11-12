// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
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
        public async Task GetInstalledCompilerMetadataExtensionReturnsTheExpectedMetadataContractInformation()
        {
            this.SetupFixture(PlatformID.Win32NT);
            string newLine = Environment.NewLine;

            // Off of a Windows system
            string gccOutput =
                $"gcc (x86_64-posix-seh, Built by strawberryperl.com project) 10.3.0{newLine}" +
                $"Copyright (C) 2018 Free Software Foundation, Inc.{newLine}" +
                $"This is free software; see the source for copying conditions.  There is NO{newLine}" +
                $"warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.{newLine}";

            // Off of a Linux system
            string ccOutput =
                $"cc (Ubuntu 10.5.0-1ubuntu1~20.04) 10.5.0{newLine}" +
                $"Copyright (C) 2020 Free Software Foundation, Inc.{newLine}" +
                $"This is free software; see the source for copying conditions.  There is NO{newLine}" +
                $"warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.{newLine}";

            // Off of a Linux system
            string gfortranOutput =
                $"GNU Fortran (Ubuntu 10.5.0-1ubuntu1~20.04) 10.5.1{newLine}" +
                $"Copyright (C) 2020 Free Software Foundation, Inc.{newLine}" +
                $"This is free software; see the source for copying conditions.  There is NO{newLine}" +
                $"warranty; not even for MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.{newLine}";

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                if (process.FullCommand().StartsWith("cc"))
                {
                    process.StandardOutput.Append(ccOutput);
                }
                else if (process.FullCommand().StartsWith("gcc"))
                {
                    process.StandardOutput.Append(gccOutput);
                }
                else if (process.FullCommand().StartsWith("gfortran"))
                {
                    process.StandardOutput.Append(gfortranOutput);
                }
            };

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetInstalledCompilerMetadataAsync();
            Assert.IsTrue(metadata.Count == 3);

            object value;
            Assert.IsTrue(metadata.TryGetValue("compilerVersion_cc", out value) && value.ToString() == "10.5.0");
            Assert.IsTrue(metadata.TryGetValue("compilerVersion_gcc", out value) && value.ToString() == "10.3.0");
            Assert.IsTrue(metadata.TryGetValue("compilerVersion_gfortran", out value) && value.ToString() == "10.5.1");
        }

        [Test]
        public void GetInstalledCompilerMetadataExtensionHandlesProcessErrors()
        {
            this.SetupFixture(PlatformID.Win32NT);
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => (process as InMemoryProcess).ExitCode = 12345;
            Assert.DoesNotThrowAsync(() => this.mockFixture.SystemManagement.Object.GetInstalledCompilerMetadataAsync());
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task GetHostMetadataReturnsTheExpectedOperatingSystemMetadataContractInformation_Windows_Scenario(PlatformID platform, Architecture architecture)
        {
            this.SetupFixture(platform, architecture);

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetHostMetadataAsync();

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
        public async Task GetHostMetadataReturnsTheExpectedOperatingSystemMetadataContractInformation_Unix_Scenario(PlatformID platform, Architecture architecture)
        {
            this.SetupFixture(platform, architecture);

            this.mockFixture.SystemManagement.Setup(mgmt => mgmt.GetLinuxDistributionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LinuxDistributionInfo
                {
                    LinuxDistribution = LinuxDistribution.Ubuntu,
                    OperationSystemFullName = "Ubuntu 20.01 build 1234"
                });

            IDictionary<string, object> metadata = await mockFixture.SystemManagement.Object.GetHostMetadataAsync();

            object value;
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Unix");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Ubuntu 20.01 build 1234");
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == PlatformSpecifics.GetPlatformArchitectureName(platform, architecture));
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value));
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value));
        }

        [Test]
        public async Task GetHostMetadataAsyncExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_1()
        {
            this.SetupFixture(PlatformID.Win32NT, Architecture.X64);

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

            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 14);

            object value;

            // Host/OS Metadata
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value) && value.ToString() == Environment.OSVersion.VersionString);
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value) && value.ToString() == Environment.OSVersion.Version.ToString());
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == "win-x64");

            // CPU/Processor Metadata
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalProcessorCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalProcessorCountPerPhysicalCore).ToString());
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");

            // Memory Metadata
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == (memoryInfo.TotalMemory * 1024).ToString());
        }

        [Test]
        public async Task GetHostMetadataAsyncExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_2()
        {
            this.SetupFixture(PlatformID.Win32NT);

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

            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 20);

            object value;

            // Host/OS Metadata
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value) && value.ToString() == Environment.OSVersion.VersionString);
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value) && value.ToString() == Environment.OSVersion.Version.ToString());
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == "win-x64");

            // CPU/Processor Metadata
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalProcessorCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalProcessorCountPerPhysicalCore).ToString());
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1d", out value) && value.ToString() == "60000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1i", out value) && value.ToString() == "40000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L3", out value) && value.ToString() == "80000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "80000000");

            // Memory Metadata
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == (memoryInfo.TotalMemory * 1024).ToString());
        }

        [Test]
        public async Task GetHostMetadataAsyncExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario_3()
        {
            this.SetupFixture(PlatformID.Win32NT);

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

            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 18);

            object value;

            // Host/OS Metadata
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "Windows");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value) && value.ToString() == Environment.OSVersion.VersionString);
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value) && value.ToString() == Environment.OSVersion.Version.ToString());
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == "win-x64");

            // CPU/Processor Metadata
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalProcessorCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalProcessorCountPerPhysicalCore).ToString());
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L3", out value) && value.ToString() == "80000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "80000000");

            // Memory Metadata
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == (memoryInfo.TotalMemory * 1024).ToString());
        }

        [Test]
        public async Task GetHostMetadataAsyncExtensionExtensionReturnsTheExpectedMetadataContractInformation_AMD_Scenario_1()
        {
            this.SetupFixture(PlatformID.Unix);

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

            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 14);

            object value;

            // Host/OS Metadata
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Unix");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "TestUbuntu");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value));
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value));
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == "linux-x64");

            // CPU/Processor Metadata
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "X64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalProcessorCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalProcessorCountPerPhysicalCore).ToString());
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "1");

            // Memory Metadata
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == (memoryInfo.TotalMemory * 1024).ToString());
        }

        [Test]
        public async Task GetHostMetadataExtensionReturnsTheExpectedMetadataContractInformation_Ampere_Scenario_1()
        {
            this.SetupFixture(PlatformID.Unix, Architecture.Arm64);

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

            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IDictionary<string, object> metadata = await this.mockFixture.SystemManagement.Object.GetHostMetadataAsync();
            Assert.IsTrue(metadata.Count == 17);

            // Host/OS Metadata
            object value;
            Assert.IsTrue(metadata.TryGetValue("computerName", out value) && value.ToString() == Environment.MachineName);
            Assert.IsTrue(metadata.TryGetValue("osFamily", out value) && value.ToString() == "Unix");
            Assert.IsTrue(metadata.TryGetValue("osName", out value) && value.ToString() == "TestUbuntu");
            Assert.IsTrue(metadata.TryGetValue("osDescription", out value));
            Assert.IsTrue(metadata.TryGetValue("osVersion", out value));
            Assert.IsTrue(metadata.TryGetValue("osPlatformArchitecture", out value) && value.ToString() == "linux-arm64");

            // CPU/Processor Metadata
            Assert.IsTrue(metadata.TryGetValue("cpuArchitecture", out value) && value.ToString() == "ARM64");
            Assert.IsTrue(metadata.TryGetValue("cpuSockets", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCores", out value) && value.ToString() == cpuInfo.PhysicalCoreCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuPhysicalCoresPerSocket", out value) && value.ToString() == (cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount).ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessors", out value) && value.ToString() == cpuInfo.LogicalProcessorCount.ToString());
            Assert.IsTrue(metadata.TryGetValue("cpuLogicalProcessorsPerCore", out value) && value.ToString() == (cpuInfo.LogicalProcessorCountPerPhysicalCore).ToString());
            Assert.IsTrue(metadata.TryGetValue("numaNodes", out value) && value.ToString() == "2");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L1", out value) && value.ToString() == "100000");
            Assert.IsTrue(metadata.TryGetValue("cpuCacheBytes_L2", out value) && value.ToString() == "10000000");
            Assert.IsTrue(metadata.TryGetValue("cpuLastCacheBytes", out value) && value.ToString() == "10000000");

            // Memory Metadata
            Assert.IsTrue(metadata.TryGetValue("memoryBytes", out value) && value.ToString() == (memoryInfo.TotalMemory * 1024).ToString());
        }

        [Test]
        public async Task GetCpuPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_Intel_Scenario()
        {
            this.SetupFixture(PlatformID.Win32NT);

            CpuInfo cpuInfo = new CpuInfo(
                name: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                description: "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel",
                physicalCoreCount: 4,
                logicalCoreCount: 8,
                socketCount: 2,
                numaNodeCount: 1,
                true);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetCpuPartsMetadataAsync();
            Assert.IsNotEmpty(parts);
            Assert.IsTrue(parts.Count() == 1);

            object value;
            Assert.IsTrue(parts.ElementAt(0).Count == 6);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("type", out value) && value.ToString() == "CPU");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("vendor", out value) && value.ToString() == "Intel");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("description", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("family", out value) && value.ToString() == "6");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("stepping", out value) && value.ToString() == "2");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("model", out value) && value.ToString() == "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz");
        }

        [Test]
        public async Task GetCpuPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_AMD_Scenario()
        {
            this.SetupFixture(PlatformID.Win32NT);

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

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetCpuPartsMetadataAsync();
            Assert.IsNotEmpty(parts);
            Assert.IsTrue(parts.Count() == 1);

            object value;
            Assert.IsTrue(parts.ElementAt(0).Count == 6);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("type", out value) && value.ToString() == "CPU");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("vendor", out value) && value.ToString() == "AMD");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("description", out value) && value.ToString() == "AMD EPYC 7V12 64-Core Processor Family 6 Model 106 Stepping 2");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("family", out value) && value.ToString() == "6");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("stepping", out value) && value.ToString() == "2");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("model", out value) && value.ToString() == "AMD EPYC 7V12 64-Core Processor");
        }

        [Test]
        public async Task GetCpuPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_Ampere_Scenario()
        {
            this.SetupFixture(PlatformID.Win32NT);

            // No caches available
            CpuInfo cpuInfo = new CpuInfo(
                name: "Ampere(R) Altra(R) Processor",
                description: "ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)",
                physicalCoreCount: 64,
                logicalCoreCount: 64,
                socketCount: 2,
                numaNodeCount: 2,
                true);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(cpuInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetCpuPartsMetadataAsync();
            Assert.IsNotEmpty(parts);
            Assert.IsTrue(parts.Count() == 1);

            object value;
            Assert.IsTrue(parts.ElementAt(0).Count == 6);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("type", out value) && value.ToString() == "CPU");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("vendor", out value) && value.ToString() == "ARM");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("description", out value) && value.ToString() == "ARMv8 (64-bit) Family 8 Model D0C Revision 301, Ampere(R)");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("family", out value) && value.ToString() == "8");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("stepping", out value) && value == null);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("model", out value) && value.ToString() == "Ampere(R) Altra(R) Processor");
        }

        [Test]
        public async Task GetMemoryPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_1()
        {
            this.SetupFixture(PlatformID.Win32NT);

            // No memory chip sets available
            MemoryInfo memoryInfo = new MemoryInfo(123456789);

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetMemoryPartsMetadataAsync();
            Assert.IsEmpty(parts);
        }

        [Test]
        public async Task GetMemoryMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_2()
        {
            this.SetupFixture(PlatformID.Win32NT);

            // Physical memory chip information present.
            MemoryInfo memoryInfo = new MemoryInfo(
                123456789,
                new List<MemoryChipInfo>
                {
                    new MemoryChipInfo("Memory_1", "Memory", 123456789, 2166, "HK Hynix", "HM123456"),
                    new MemoryChipInfo("Memory_2", "Memory", 223344556, 2432, "Micron", "M987654")
                });

            this.mockFixture.SystemManagement.Setup(sys => sys.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(memoryInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetMemoryPartsMetadataAsync();
            Assert.IsNotEmpty(parts);
            Assert.IsTrue(parts.Count() == 2);

            object value;
            Assert.IsTrue(parts.ElementAt(0).Count == 6);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("type", out value) && value.ToString() == "Memory");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("vendor", out value) && value.ToString() == "HK Hynix");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("description", out value) && value.ToString() == "HK Hynix HM123456");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("bytes", out value) && value.ToString() == "123456789");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("speed", out value) && value.ToString() == "2166");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("partNumber", out value) && value.ToString() == "HM123456");

            Assert.IsTrue(parts.ElementAt(1).Count == 6);
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("type", out value) && value.ToString() == "Memory");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("vendor", out value) && value.ToString() == "Micron");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("description", out value) && value.ToString() == "Micron M987654");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("bytes", out value) && value.ToString() == "223344556");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("speed", out value) && value.ToString() == "2432");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("partNumber", out value) && value.ToString() == "M987654");
        }

        [Test]
        public async Task GetNetworkPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_1()
        {
            this.SetupFixture(PlatformID.Win32NT);

            // No network interfaces sets available
            NetworkInfo networkInfo = new NetworkInfo(Array.Empty<NetworkInterfaceInfo>());

            this.mockFixture.SystemManagement.Setup(sys => sys.GetNetworkInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(networkInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetNetworkPartsMetadataAsync();
            Assert.IsEmpty(parts);
        }

        [Test]
        public async Task GetNetworkPartsMetadataExtensionReturnsTheExpectedMetadataContractInformation_Scenario_2()
        {
            this.SetupFixture(PlatformID.Win32NT);

            NetworkInfo networkInfo = new NetworkInfo(new List<NetworkInterfaceInfo>
            {
                new NetworkInterfaceInfo(
                    "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)",
                    "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)"),

                new NetworkInterfaceInfo(
                    "Realtek Semiconductor Co., Ltd. RTL810xE PCI Express Fast Ethernet controller (rev 07)",
                    "Realtek Semiconductor Co., Ltd. RTL810xE PCI Express Fast Ethernet controller (rev 07)")
            });

            this.mockFixture.SystemManagement.Setup(sys => sys.GetNetworkInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(networkInfo);

            IEnumerable<IDictionary<string, object>> parts = await this.mockFixture.SystemManagement.Object.GetNetworkPartsMetadataAsync();
            Assert.IsNotEmpty(parts);
            Assert.IsTrue(parts.Count() == 2);

            object value;
            Assert.IsTrue(parts.ElementAt(0).Count == 3);
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("type", out value) && value.ToString() == "Network");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("vendor", out value) && value.ToString() == "Realtek");
            Assert.IsTrue(parts.ElementAt(0).TryGetValue("description", out value) && value.ToString() == "Realtek Semiconductor Co., Ltd. RTL8188EE Wireless Network Adapter (rev 01)");

            Assert.IsTrue(parts.ElementAt(1).Count == 3);
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("type", out value) && value.ToString() == "Network");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("vendor", out value) && value.ToString() == "Realtek");
            Assert.IsTrue(parts.ElementAt(1).TryGetValue("description", out value) && value.ToString() == "Realtek Semiconductor Co., Ltd. RTL810xE PCI Express Fast Ethernet controller (rev 07)");
        }
    }
}
