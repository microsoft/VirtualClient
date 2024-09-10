// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
        /// Creates a mock <see cref="Disk"/> instance.
        /// </summary>
        public static IEnumerable<Disk> CreateDisks(this IFixture fixture, PlatformID platform, bool withVolume = false)
        {
            fixture.ThrowIfNull(nameof(fixture));

            return new List<Disk>
            {
                FixtureExtensions.CreateDisk(0, platform, withVolume, os: true),
                FixtureExtensions.CreateDisk(1, platform, withVolume, os: false),
                FixtureExtensions.CreateDisk(2, platform, withVolume, os: false),
                FixtureExtensions.CreateDisk(3, platform, withVolume, os: false)
            };
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

        public static Disk CreateDisk(int index, PlatformID platform = PlatformID.Unix, bool withVolume = false, string deviceName = null, bool os = false)
        {
            string diskPath = platform == PlatformID.Win32NT
                ? @$"\\.\PHYSICALDISK{index}"
                : $"/dev/sd{(char)('c' + (char)index)}";

            string devicePath = platform == PlatformID.Win32NT
                ? $"{(char)('C' + (char)index)}:\\"
                : $"{diskPath}1";

            List<string> accessPaths = new List<string>();
            if (platform == PlatformID.Win32NT)
            {
                accessPaths.Add(diskPath);
            }

            List<DiskVolume> volumes = new List<DiskVolume>();

            if (withVolume || os)
            {
                volumes = new List<DiskVolume>() { FixtureExtensions.CreateDiskVolume(devicePath, platform, os) };
            }

            return new Disk(index, deviceName ?? diskPath, volumes, properties: FixtureExtensions.CreateDiskProperties(index, platform, os));
        }

        public static DiskVolume CreateDiskVolume(string devicePath, PlatformID platform = PlatformID.Unix, bool os = false)
        {
            List<string> accessPaths = new List<string>();
            accessPaths.Add(devicePath);
            if (os)
            {
                accessPaths.Add("/");
            }

            return new DiskVolume(0, devicePath, accessPaths, properties: FixtureExtensions.CreateDiskProperties(1, platform, os));
        }

        private static Dictionary<string, IConvertible> CreateDiskProperties(int lun = 0, PlatformID platform = PlatformID.Unix, bool os = false)
        {
            Dictionary<string, IConvertible> properties = new Dictionary<string, IConvertible>();
            switch (platform)
            {
                case PlatformID.Win32NT:
                    properties = new Dictionary<string, IConvertible>
                    {
                        { "PartitionIndex", $"{lun}" },
                        { "Type", "Partition" },
                        { "Hidden", "No" },
                        { "Required", "No" },
                        { "Attrib", "0000000000000000" },
                        { "Offset in Bytes", "633202540544" },
                        { "Index", $"{lun}" },
                        { "Ltr", "S" },
                        { "Label", "Data" },
                        { "Fs", "NTFS" },
                        { "Size", "1234 GB" },
                        { "Status", "Healthy" },
                        { "Info", null }
                    };

                    if (os)
                    {
                        properties["Info"] = "Boot";
                        properties["Boot Disk"] = "Yes";
                        properties["Size"] = "123 GB";
                    }

                    break;

                case PlatformID.Unix:
                    properties = new Dictionary<string, IConvertible>
                    {
                        { "id", "volume" },
                        { "claimed", "true" },
                        { "class", "volume" },
                        { "handle", $"GUID:{Guid.NewGuid()}" },
                        { "description", "EXT4 volume" },
                        { "vendor", "Linux" },
                        { "physid", $"{lun}" },
                        { "businfo", $"scsi@1:0.0.1,{lun}" },
                        { "logicalname", "/dev/sdd1" },
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
                        properties["size"] = "1234567890";
                    }

                    break;
            }

            return properties;
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
