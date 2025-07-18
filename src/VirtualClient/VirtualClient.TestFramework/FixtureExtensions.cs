// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using AutoFixture;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="Fixture"/> instances and for general
    /// testing classes.
    /// </summary>
    /// <remarks>
    /// The <see cref="Fixture"/> class is part of a library called Autofixture
    /// which is used to help ease the creation of mock objects that are commonly
    /// used in VC project tests (e.g. unit, functional).
    ///
    /// Source Code:
    /// https://github.com/AutoFixture/AutoFixture"
    ///
    /// Cheat Sheet:
    /// https://github.com/AutoFixture/AutoFixture/wiki/Cheat-Sheet
    /// </remarks>
    public static class FixtureExtensions
    {
        private static Random randomGen = new Random();

        /// <summary>
        /// Creates a mock disk object.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="diskIndex">The disk index.</param>
        /// <param name="platform">The platform to use for representation of disk paths.</param>
        /// <param name="os">Optional. True if the disk should represent the operating system/boot disk.</param>
        /// <param name="devicePaths">
        /// Optional. Defines the device paths for both the disk as well as the volumes/partitions. The first path defined should be a disk/device path. 
        /// Each path thereafter should define a device/mount path for a volume/partition (e.g. \\.\PHYSICALDISK1, D:\, E:\ or /dev/sdc, /dev/sdc1, /dev/sdc2).
        /// </param>
        public static Disk CreateDisk(this IFixture fixture, int diskIndex, PlatformID platform = PlatformID.Unix, bool os = false, params string[] devicePaths)
        {
            string[] effectiveDevicePaths = devicePaths;
            if (devicePaths?.Any() != true)
            {
                switch (platform)
                {
                    case PlatformID.Win32NT:
                        if (os)
                        {
                            effectiveDevicePaths = new string[] { @"\\.\PHYSICALDISK0" };
                        }
                        else
                        {
                            effectiveDevicePaths = new string[] { @"\\.\PHYSICALDISK1" };
                        }

                        break;

                    case PlatformID.Unix:
                        if (os)
                        {
                            effectiveDevicePaths = new string[] { @"/dev/sda" };
                        }
                        else
                        {
                            effectiveDevicePaths = new string[] { @"/dev/sdc" };
                        }

                        break;
                }
            }

            string diskPath = effectiveDevicePaths.First();
            List<DiskVolume> volumes = new List<DiskVolume>();
            IEnumerable<string> volumeDevicePaths = effectiveDevicePaths.Skip(1);

            if (volumeDevicePaths?.Any() == true)
            {
                int lun = 0;
                foreach (string devicePath in volumeDevicePaths)
                {
                    volumes.Add(fixture.CreateDiskVolume(diskIndex, devicePath, platform, os, lun));
                    lun++;
                }
            }

            Dictionary<string, IConvertible> diskProperties = new Dictionary<string, IConvertible>();
            switch (platform)
            {
                case PlatformID.Win32NT:
                    diskProperties = new Dictionary<string, IConvertible>
                    {
                        { "Type", "SAS" },
                        { "Model", "Microsoft Virtual Disk" },
                        { "Index", diskIndex },
                        { "Disk ID", $"{{{Guid.NewGuid()}}}" },
                        { "Status", "Online" },
                        { "Path", diskIndex },
                        { "Target", diskIndex },
                        { "Location Path", $"ACPI(_SB_)#ACPI(VMOD)#ACPI(VMBS)#VMBUS({{{Guid.NewGuid()}}}#{{{Guid.NewGuid}}})#SAS(P00T00L00)" },
                        { "Current Read-Only State", "No" },
                        { "Read-Only", "No" },
                        { "Boot Disk", "No" },
                        { "Pagefile Disk", "No" },
                        { "Hibernation File Disk", "No" },
                        { "Crashdump Disk", "No" },
                        { "Clustered Disk", "No" }
                    };

                    if (os)
                    {
                        diskProperties["Boot Disk"] = "Yes";
                        diskProperties["Crashdump Disk"] = "Yes";
                    }

                    break;

                case PlatformID.Unix:
                    Guid handle = Guid.NewGuid();
                    diskProperties = new Dictionary<string, IConvertible>
                    {
                        { "id", "disk" },
                        { "claimed", "true" },
                        { "class", "disk" },
                        { "handle", $"GUID:{handle}" },
                        { "product", "Virtual Disk" },
                        { "vendor", "Linux" },
                        { "physid", $"0.0.{diskIndex}" },
                        { "businfo", $"scsi@0.0.0.{diskIndex}" },
                        { "logicalname", diskPath },
                        { "dev", "8:0" },
                        { "version", "1.0" },
                        { "size", "1234567890123" },
                        { "ansiversion", "5" },
                        { "guid", $"{handle}" },
                        { "logicalsectorsize", "512" },
                        { "sectorsize", "4096" },
                        { "gpt-1.00", "GUID Partition Table version 1.00" },
                        { "partitioned", "Partitioned disk" },
                        { "partitioned:gpt", "GUID partition table" }
                    };

                    if (os)
                    {
                        // Making the OS disk smaller for unit testing purpose.
                        diskProperties["size"] = "1234567890";
                    }

                    break;
            }

            return new Disk(diskIndex, diskPath, volumes, properties: diskProperties);
        }

        /// <summary>
        /// Creates a mock set of <see cref="Disk"/> instances.
        /// </summary>
        public static IEnumerable<Disk> CreateDisks(this IFixture fixture, PlatformID platform, bool withVolume = false)
        {
            fixture.ThrowIfNull(nameof(fixture));
            List<Disk> disks = new List<Disk>();

            if (platform == PlatformID.Unix)
            {
                List<string[]> devicePaths = new List<string[]>
                {
                    new string[] { @"/dev/sda", @"/dev/sda1" },
                    withVolume ? new string[] { @"/dev/sdc", @"/dev/sdc1" } : new string[] { @"/dev/sdc" },
                    withVolume ? new string[] { @"/dev/sdd", @"/dev/sdd1" } : new string[] { @"/dev/sdd" },
                    withVolume ? new string[] { @"/dev/sde", @"/dev/sde1" } : new string[] { @"/dev/sde" },
                };

                return new List<Disk>
                {
                    fixture.CreateDisk(0, platform, os: true, devicePaths[0]),
                    fixture.CreateDisk(1, platform, os: false, devicePaths[1]),
                    fixture.CreateDisk(2, platform, os: false, devicePaths[2]),
                    fixture.CreateDisk(3, platform, os: false, devicePaths[3])
                };
            }
            else
            {
                List<string[]> devicePaths = new List<string[]>
                {
                    new string[] { @"\\.\PHYSICALDISK0", @"C:\" },
                    withVolume ? new string[] { @"\\.\PHYSICALDISK1", @"D:\" } : new string[] { @"\\.\PHYSICALDISK1" },
                    withVolume ? new string[] { @"\\.\PHYSICALDISK2", @"E:\" } : new string[] { @"\\.\PHYSICALDISK2" },
                    withVolume ? new string[] { @"\\.\PHYSICALDISK3", @"F:\" } : new string[] { @"\\.\PHYSICALDISK3" },
                };

                return new List<Disk>
                {
                    fixture.CreateDisk(0, platform, os: true, devicePaths[0]),
                    fixture.CreateDisk(1, platform, os: false, devicePaths[1]),
                    fixture.CreateDisk(2, platform, os: false, devicePaths[2]),
                    fixture.CreateDisk(3, platform, os: false, devicePaths[3])
                };
            }
        }

        /// <summary>
        /// Creates a mock disk volume/partition object.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="diskIndex">The disk index.</param>
        /// <param name="devicePath">The disk volume/partition device path (e.g. D:\, E:\, /dev/sdc1, /dev/sdd1).</param>
        /// <param name="platform">The platform to use for representation of disk paths.</param>
        /// <param name="os">Optional. True if the disk should represent the operating system/boot disk.</param>
        /// <param name="lun">Optional. The logical unit for the disk volume/partition on the disk.</param>
        public static DiskVolume CreateDiskVolume(this IFixture fixture, int diskIndex, string devicePath, PlatformID platform = PlatformID.Unix, bool os = false, int lun = 0)
        {
            Dictionary<string, IConvertible> diskVolumeProperties = new Dictionary<string, IConvertible>();
            List<string> accessPaths = new List<string>();

            switch (platform)
            {
                case PlatformID.Win32NT:
                    diskVolumeProperties = new Dictionary<string, IConvertible>
                    {
                        { "PartitionIndex", $"{lun}" },
                        { "Type", "Partition" },
                        { "Hidden", "No" },
                        { "Required", "No" },
                        { "Attrib", "0000000000000000" },
                        { "Offset in Bytes", "633202540544" },
                        { "Index", $"{lun}" },
                        { "Ltr", devicePath.Substring(0, 1) },
                        { "Label", os ? "System" : "Data" },
                        { "Fs", "NTFS" },
                        { "Size", "1234 GB" },
                        { "Status", "Healthy" },
                        { "Info", null }
                    };

                    // Volume/access path for Windows (e.g. C:\, D:\).
                    accessPaths.Add(devicePath);

                    if (os)
                    {
                        diskVolumeProperties["Info"] = "Boot";
                        diskVolumeProperties["Boot Disk"] = "Yes";
                        diskVolumeProperties["Size"] = "123 GB";
                    }

                    break;

                case PlatformID.Unix:
                    diskVolumeProperties = new Dictionary<string, IConvertible>
                    {
                        { "id", "volume" },
                        { "claimed", "true" },
                        { "class", "volume" },
                        { "handle", $"GUID:{Guid.NewGuid()}" },
                        { "description", "EXT4 volume" },
                        { "vendor", "Linux" },
                        { "physid", $"{lun}" },
                        { "businfo", $"scsi@0:0.0.{diskIndex},{lun}" },
                        { "logicalname", devicePath },
                        { "dev", "8:49" },
                        { "version", "1.0" },
                        { "serial", $"{Guid.NewGuid()}" },
                        { "size", "1234567890123" },
                        { "capacity", null },
                        { "created", "2021-05-05 19:29:17" },
                        { "filesystem", "ext4" },
                        { "modified", "2021-05-11 22:48:47" },
                        { "mounted", "2021-05-05 19:29:29" },
                        { "name", "primary" },
                        { "state", "clean" },
                        { "journaled", string.Empty },
                        { "extended_attributes", "Extended Attributes" },
                        { "large_files", "4GB+ files" },
                        { "huge_files", "16TB+ files" },
                        { "dir_nlink", "directories with 65000+ subdirs" },
                        { "64bit", "64bit filesystem" },
                        { "extents", "extent-based allocation" },
                        { "ext4", string.Empty },
                        { "ext2", "EXT2/EXT3" },
                        { "initialized", "initialized volume" }
                    };

                    if (os)
                    {
                        // Making the OS disk smaller for unit testing purpose.
                        diskVolumeProperties["size"] = "1234567890";

                        // Default access path for Linux (e.g. /root, /home/user).
                        accessPaths.Add("/");
                    }
                    else
                    {
                        accessPaths.Add($"/home/user/mnt{devicePath.Replace('/', '_')}".ToLowerInvariant());
                    }

                    break;
            }

            return new DiskVolume(lun, devicePath, accessPaths, properties: diskVolumeProperties);
        }

        /// <summary>
        /// Creates a mock <see cref="HttpResponseMessage"/> instance.
        /// </summary>
        public static HttpResponseMessage CreateHttpResponse(this IFixture fixture, HttpStatusCode expectedStatusCode, object expectedContent = null)
        {
            fixture.ThrowIfNull(nameof(fixture));

            HttpResponseMessage mockResponse = new HttpResponseMessage(expectedStatusCode);

            if (expectedContent != null)
            {
                mockResponse.Content = new StringContent(expectedContent.ToJson());
            }

            return mockResponse;
        }

        /// <summary>
        /// Creates a mock/test <see cref="InMemoryProcess"/>. The process will be set
        /// to start and exit promptly.
        /// </summary>
        public static InMemoryProcess CreateProcess(this DependencyFixture fixture, string command, string arguments, string workingDir)
        {
            fixture.ThrowIfNull(nameof(fixture));

            InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDir
                }
            };

            process.OnHasExited = () => true;
            process.OnStart = () => true;

            return process;
        }

        /// <summary>
        /// Sets up the auto-fixture mock objects relevant to the Virtual Client
        /// solution.
        /// </summary>
        /// <param name="fixture">The auto-fixture to setup.</param>
        /// <param name="randomization">
        /// True if randomization should be applied to ensure objects created are not identical. False if the fixture should 
        /// create objects that are equal each time.
        /// </param>
        public static IFixture SetupMocks(this IFixture fixture, bool randomization = false)
        {
            fixture.ThrowIfNull(nameof(fixture));
            fixture.Register(() => FixtureExtensions.CreateEnvironmentLayout(randomization));
            fixture.Register(() => FixtureExtensions.CreateClientInstance(randomization));
            fixture.Register(() => FixtureExtensions.CreateMetric(randomization));
            fixture.Register(() => FixtureExtensions.CreateInstructions(randomization));
            fixture.Register(() => FixtureExtensions.CreateParameters(randomization));
            fixture.Register(() => FixtureExtensions.CreateState(randomization));
            fixture.Register(() => FixtureExtensions.CreateExecutionProfile(randomization));
            fixture.Register(() => FixtureExtensions.CreateExecutionProfileElement(randomization));

            return fixture;
        }

        private static ExecutionProfile CreateExecutionProfile(bool randomization)
        {
            return new ExecutionProfile(
                $"mock execution profile{(randomization ? randomGen.Next().ToString() : string.Empty)}",
                TimeSpan.FromSeconds(20),
                new List<ExecutionProfileElement>() { FixtureExtensions.CreateExecutionProfileElement(randomization) },
                new List<ExecutionProfileElement>() { FixtureExtensions.CreateExecutionProfileElement(randomization) },
                new List<ExecutionProfileElement>() { FixtureExtensions.CreateExecutionProfileElement(randomization) },
                new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase));
        }

        private static ExecutionProfileElement CreateExecutionProfileElement(bool randomization)
        {
            return new ExecutionProfileElement(
                $"executionProfileType{(randomization ? randomGen.Next().ToString() : string.Empty)}",
                parameters: new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234
                },
                metadata: new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Metadata1"] = "00:30:00",
                    ["Metadata2"] = false
                });
        }

        private static ClientInstance CreateClientInstance(bool randomization = false)
        {
            string role = "Client";
            if (randomization)
            {
                int roleInstance = FixtureExtensions.randomGen.Next(2);
                if (roleInstance == 1)
                {
                    role = "Server";
                }
            }

            return new ClientInstance((randomization ? $"VM{Guid.NewGuid()}" : "VM01"), "1.2.3.4", role);
        }

        private static EnvironmentLayout CreateEnvironmentLayout(bool randomization = false)
        {
            return new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance((randomization ? $"VM{Guid.NewGuid()}" : "VM01"), "1.2.3.4", "client"),
                new ClientInstance((randomization ? $"VM{Guid.NewGuid()}" : "VM02"), "1.2.3.5", "client"),
                new ClientInstance((randomization ? $"VM{Guid.NewGuid()}" : "VM03"), "1.2.3.6", "client"),
                new ClientInstance((randomization ? $"VM{Guid.NewGuid()}" : "VM04"), "1.2.3.7", "client")
            });
        }

        private static Instructions CreateInstructions(bool randomization = false)
        {
            Guid id = randomization ? Guid.NewGuid() : Guid.Empty;
            return new Instructions(InstructionsType.Profiling, new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = randomGen.Next(5000, 100000000),
                ["property3"] = randomGen.Next(0, 2) == 0 ? false : true
            });
        }

        private static Metric CreateMetric(bool randomization = false)
        {
            return new Metric(
                (randomization ? $"avg. request time{Guid.NewGuid()}" : "avg. request time"),
                1234.5,
                "msec",
                MetricRelativity.HigherIsBetter,
                tags: new List<string> { "Tag1", "Tag2" },
                description: "Measures the average request time.");
        }

        private static IDictionary<string, IConvertible> CreateParameters(bool randomization = false)
        {
            return new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                ["Parameter1"] = "AnyValue",
                ["Parameter2"] = (randomization ? FixtureExtensions.randomGen.Next(1000, 10000) : 123456),
                ["Parameter3"] = true,
                ["Parameter4"] = (randomization ? Guid.NewGuid().ToString() : Guid.Parse("98D52459-3967-434F-8BC7-ADF9EC43B1A2").ToString()),
                ["Parameter5"] = TimeSpan.FromMinutes(5).ToString(),
                ["Parameter6"] = (randomization ? DateTime.UtcNow : DateTime.Parse("2021-04-24T00:00:00.0000000Z"))
            };
        }

        private static State CreateState(bool randomization = false)
        {
            return new State(new Dictionary<string, IConvertible>
            {
                ["property1"] = "value1",
                ["property2"] = randomGen.Next(5000, 100000000),
                ["property3"] = randomGen.Next(0, 2) == 0 ? false : true,
                ["property4"] = randomization ? Guid.NewGuid().ToString() : Guid.Empty.ToString()
            });
        }
    }
}
