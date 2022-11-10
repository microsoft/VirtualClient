using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;
using System.Text.RegularExpressions;
using System;
using System.Xml.Serialization;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    [TestFixture]
    [Category("Unit")]
    public class LshwDiskParserUnitTests
    {
        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "lshw");
            }
        }

        [Test]
        public void UnixDiskManagerParsesLshwCommandDiskDriveResultsCorrectly_AzureVM_OSDisk()
        {
            // Scenario: Before Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);

            IDictionary<string, IConvertible> disk0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // This is the OS Disk
                { "id", "disk:0" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:8baf2489-b867-4532-bf70-b34611d9cd15" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@0:0.0.0" },
                { "logicalname", "/dev/sda" },
                { "dev", "8:0" },
                { "version", "1.0" },
                { "size", "32213303296" },
                { "ansiversion", "5" },
                { "guid", "8baf2489-b867-4532-bf70-b34611d9cd15" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Properties, disk0ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesLshwCommandDiskDriveResultsCorrectly_AzureVM_LocalTempDisk()
        {
            // Scenario: Before Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);

            IDictionary<string, IConvertible> disk1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // This is the local/temp disk
                { "id", "disk:1" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:00:00:00:01" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.1" },
                { "businfo", "scsi@0:0.0.1" },
                { "logicalname", "/dev/sdb" },
                { "dev", "8:16" },
                { "version", "1.0" },
                { "size", "34359738368" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "signature", "643e7360" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:dos", "MS-DOS partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(1).Properties, disk1ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesLshwCommandDiskDriveResultsCorrectly_AzureVM_RemoteDisk_BeforePartitioning()
        {
            // Scenario: Before Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_before_partitioning.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);

            IDictionary<string, IConvertible> remoteDisk1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Remote Disk 1
                { "id", "disk:0" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:01:00:00:01" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.1" },
                { "businfo", "scsi@1:0.0.1" },
                { "logicalname", "/dev/sdc" },
                { "dev", "8:32" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" }
            };

            this.VerifyDiskProperties(disks.ElementAt(2).Properties, remoteDisk1ExpectedProperties);

            IDictionary<string, IConvertible> remoteDisk2ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Remote Disk 2
                { "id", "disk:1" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:01:00:00:00" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@1:0.0.0" },
                { "logicalname", "/dev/sdd" },
                { "dev", "8:48" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" }
            };

            this.VerifyDiskProperties(disks.ElementAt(3).Properties, remoteDisk2ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesLshwCommandDiskDriveResultsCorrectly_AzureVM_RemoteDisk_AfterPartitioning()
        {
            // Scenario: After Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);

            IDictionary<string, IConvertible> remoteDisk1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Remote Disk 1
                { "id", "disk:0" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:a14b25e7-d808-4c63-8a66-e1b03c2cfbbe" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.1" },
                { "businfo", "scsi@1:0.0.1" },
                { "logicalname", "/dev/sdc" },
                { "dev", "8:32" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "guid", "a14b25e7-d808-4c63-8a66-e1b03c2cfbbe" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(2).Properties, remoteDisk1ExpectedProperties);

            IDictionary<string, IConvertible> remoteDisk2ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Remote Disk 2
                { "id", "disk:1" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:3f1620f7-3ea0-45d9-9ddd-71970c7b42d9" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@1:0.0.0" },
                { "logicalname", "/dev/sdd" },
                { "dev", "8:48" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "guid", "3f1620f7-3ea0-45d9-9ddd-71970c7b42d9" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(3).Properties, remoteDisk2ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerHandlesNvmeDiskNamespacesInLshwCommandDiskDriveResults()
        {
            // Scenario: Before Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_with_nvme_1.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.AreEqual(7, disks.Count());

            IDictionary<string, IConvertible> disk0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "namespace" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", string.Empty },
                { "description", "NVMe namespace" },
                { "physid", "1" },
                { "logicalname", "/dev/nvme0n1" },
                { "size", "960197124096" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "512" }
            };

            IDictionary<string, IConvertible> disk1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "namespace" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", string.Empty },
                { "description", "NVMe namespace" },
                { "physid", "1" },
                { "logicalname", "/dev/nvme1n1" },
                { "size", "960197124096" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "512" }
            };

            IDictionary<string, IConvertible> disk2ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:01:00:01:00" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.1.0" },
                { "businfo", "scsi@1:0.1.0" },
                { "logicalname", "/dev/sda" },
                { "dev", "8:0" },
                { "version", "1.0" },
                { "size", "515396075520" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "signature", "284b468a" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:dos", "MS-DOS partition table" }
            };

            IDictionary<string, IConvertible> disk3ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:1a56bd4f-b41f-4ed8-843d-9245d9176174" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@0:0.0.0" },
                { "logicalname", "/dev/sdb" },
                { "dev", "8:16" },
                { "version", "1.0" },
                { "size", "549755813888" },
                { "ansiversion", "5" },
                { "guid", "1a56bd4f-b41f-4ed8-843d-9245d9176174" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            IDictionary<string, IConvertible> disk4ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk:0" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:03:00:00:00" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@3:0.0.0" },
                { "logicalname", "/dev/sdc" },
                { "dev", "8:32" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" }
            };

            IDictionary<string, IConvertible> disk5ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk:1" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:03:00:00:01" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.1" },
                { "businfo", "scsi@3:0.0.1" },
                { "logicalname", "/dev/sdd" },
                { "dev", "8:48" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" }
            };

            IDictionary<string, IConvertible> disk6ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "cdrom" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:05:00:00:00" },
                { "description", "DVD reader" },
                { "product", "Virtual CD/ROM" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "logicalname", "/dev/cdrom,/dev/dvd,/dev/sr0" },
                { "dev", "11:0" },
                { "version", "1.0" },
                { "ansiversion", "5" },
                { "status", "nodisc" },
                { "removable", "support is removable" },
                { "audio", "Audio CD playback" },
                { "dvd", "DVD playback" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Properties, disk0ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(1).Properties, disk1ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(2).Properties, disk2ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(3).Properties, disk3ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(4).Properties, disk4ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(5).Properties, disk5ExpectedProperties);
            this.VerifyDiskProperties(disks.ElementAt(6).Properties, disk6ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerHandlesVolumesThatDoNotHaveLogicalNames()
        {
            // Scenario:
            // This issue was found on Gen8/ARM64 Linux systems. Some of the FAT32 boot volumes do not have a logical
            // name associated.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_with_volume_without_logicalname.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 3);

            IDictionary<string, IConvertible> disk0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:00:00:00:00" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@0:0.0.0" },
                { "logicalname", "/dev/sda" },
                { "dev", "8:0" },
                { "version", "1.0" },
                { "size", "1099511627776" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Properties, disk0ExpectedProperties);

            IDictionary<string, IConvertible> disk1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk:0" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:59df4001-6aaa-be44-82bc-827d050ce690" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.0" },
                { "businfo", "scsi@1:0.0.0" },
                { "logicalname", "/dev/sdb" },
                { "dev", "8:16" },
                { "version", "1.0" },
                { "size", "32212254720" },
                { "ansiversion", "5" },
                { "guid", "59df4001-6aaa-be44-82bc-827d050ce690" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(1).Properties, disk1ExpectedProperties);

            IDictionary<string, IConvertible> disk2ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "disk:1" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "SCSI:01:00:00:01" },
                { "description", "SCSI Disk" },
                { "product", "Virtual Disk" },
                { "vendor", "Msft" },
                { "physid", "0.0.1" },
                { "businfo", "scsi@1:0.0.1" },
                { "logicalname", "/dev/sdc" },
                { "dev", "8:32" },
                { "version", "1.0" },
                { "size", "161061273600" },
                { "ansiversion", "5" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "4096" },
                { "signature", "609166c7" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:dos", "MS-DOS partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(2).Properties, disk2ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerHandlesLargeArraysOfDisksInDiskDriveResults()
        {
            // Scenario: 
            // There are more than 10 disks on a system.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_with_duplicate_index_and_logicalunit.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 17);
            CollectionAssert.AreEquivalent(
                new List<string>
                {
                    "/dev/sda",
                    "/dev/sdb",
                    "/dev/sdc",
                    "/dev/sdd",
                    "/dev/sde",
                    "/dev/sdf",
                    "/dev/sdg",
                    "/dev/sdh",
                    "/dev/sdi",
                    "/dev/sdj",
                    "/dev/sdk",
                    "/dev/sdl",
                    "/dev/sdm",
                    "/dev/sdn",
                    "/dev/sdo",
                    "/dev/sdp",
                    "/dev/sdq"
                },
                disks.Select(disk => disk.Properties["logicalname"]));
        }

        [Test]
        public void UnixDiskManagerParsesVolumesFromLshwCommandDiskDriveResultsCorrectly_AzureVM_OSDisk()
        {
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);
            Assert.IsNotEmpty(disks.ElementAt(0).Volumes);
            Assert.IsTrue(disks.ElementAt(0).Volumes.Count() == 3);

            Disk osDisk = disks.ElementAt(0);

            IDictionary<string, IConvertible> disk0Volume0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "volume:0" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", "GUID:2dd3b4f5-5ec9-4db9-9f18-badf3ad098ea" },
                { "description", "EXT4 volume" },
                { "vendor", "Linux" },
                { "physid", "1" },
                { "businfo", "scsi@0:0.0.0,1" },
                { "logicalname", "/dev/sda1,/" },
                { "dev", "8:1" },
                { "version", "1.0" },
                { "serial", "2d1ac45f-9e31-48ec-95c8-4a0991cf6c0e" },
                { "size", "32096890880" },
                { "capacity", "32096893952" },
                { "created", "2021-05-01 15:53:23" },
                { "filesystem", "ext4" },
                { "label", "cloudimg-rootfs" },
                { "lastmountpoint", "/" },
                { "modified", "2021-05-05 16:15:05" },
                { "mount.fstype", "ext4" },
                { "mount.options", "rw,relatime,discard" },
                { "mounted", "2021-05-05 16:15:07" },
                { "state", "mounted" },
                { "journaled", string.Empty },
                { "extended_attributes", "Extended Attributes" },
                { "large_files", "4GB+ files" },
                { "huge_files", "16TB+ files" },
                { "dir_nlink", "directories with 65000+ subdirs" },
                { "recover", "needs recovery" },
                { "64bit", "64bit filesystem" },
                { "extents", "extent-based allocation" },
                { "ext4", string.Empty },
                { "ext2", "EXT2/EXT3" },
                { "initialized", "initialized volume" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Volumes.ElementAt(0).Properties, disk0Volume0ExpectedProperties);

            IDictionary<string, IConvertible> disk0Volume1ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "volume:1" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", "GUID:f81caca1-d869-42b4-8952-d27d491be272" },
                { "description", "BIOS Boot partition" },
                { "vendor", "EFI" },
                { "physid", "e" },
                { "businfo", "scsi@0:0.0.0,14" },
                { "logicalname", "/dev/sda14" },
                { "dev", "8:14" },
                { "serial", "f81caca1-d869-42b4-8952-d27d491be272" },
                { "capacity", "4193792" },
                { "nofs", "No filesystem" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Volumes.ElementAt(1).Properties, disk0Volume1ExpectedProperties);

            IDictionary<string, IConvertible> disk0Volume2ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "volume:2" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", "GUID:ac181da1-c3bb-4108-9035-bad59fe55717" },
                { "description", "Windows FAT volume" },
                { "vendor", "mkfs.fat" },
                { "physid", "f" },
                { "businfo", "scsi@0:0.0.0,15" },
                { "logicalname", "/dev/sda15,/boot/efi" },
                { "dev", "8:15" },
                { "version", "FAT32" },
                { "serial", "9808-7774" },
                { "size", "111132672" },
                { "capacity", "111148544" },
                { "FATs", "2" },
                { "filesystem", "fat" },
                { "label", "UEFI" },
                { "mount.fstype", "vfat" },
                { "mount.options", "rw,relatime,fmask=0077,dmask=0077,codepage=437,iocharset=iso8859-1,shortname=mixed,errors=remount-ro" },
                { "state", "mounted" },
                { "boot", "Contains boot code" },
                { "fat", "Windows FAT" },
                { "initialized", "initialized volume" },
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Volumes.ElementAt(2).Properties, disk0Volume2ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesVolumesFromLshwCommandDiskDriveResultsCorrectly_AzureVM_LocalTempDisk()
        {
            // Scenario:
            // Before Partitioning
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);
            Assert.IsNotEmpty(disks.ElementAt(1).Volumes);
            Assert.IsTrue(disks.ElementAt(1).Volumes.Count() == 1);

            IDictionary<string, IConvertible> disk1Volume0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                { "id", "volume" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", null },
                { "description", "EXT4 volume" },
                { "vendor", "Linux" },
                { "physid", "1" },
                { "businfo", "scsi@0:0.0.1,1" },
                { "logicalname", "/dev/sdb1,/mnt" },
                { "dev", "8:17" },
                { "version", "1.0" },
                { "serial", "9c293ef3-fcee-41cc-bec6-ba4fd8222810" },
                { "size", "34357641216" },
                { "capacity", "34357641216" },
                { "created", "2021-05-05 16:15:19" },
                { "filesystem", "ext4" },
                { "modified", "2021-05-05 16:15:20" },
                { "mount.fstype", "ext4" },
                { "mount.options", "rw,relatime" },
                { "mounted", "2021-05-05 16:15:20" },
                { "state", "mounted" },
                { "journaled", string.Empty },
                { "extended_attributes", "Extended Attributes" },
                { "large_files", "4GB+ files" },
                { "huge_files", "16TB+ files" },
                { "dir_nlink", "directories with 65000+ subdirs" },
                { "recover", "needs recovery" },
                { "64bit", "64bit filesystem" },
                { "extents", "extent-based allocation" },
                { "ext4", string.Empty },
                { "ext2", "EXT2/EXT3" },
                { "initialized", "initialized volume" }
            };

            this.VerifyDiskProperties(disks.ElementAt(1).Volumes.ElementAt(0).Properties, disk1Volume0ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesVolumesFromLshwCommandDiskDriveResultsCorrectly_AzureVM_RemoteDisk_BeforePartitioning()
        {
            // Scenario:
            // Before Partitioning, the remote disks will not have a partition nor a file system and thus
            // they will not show any volumes.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_before_partitioning.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);
            Assert.IsEmpty(disks.ElementAt(2).Volumes);
            Assert.IsEmpty(disks.ElementAt(3).Volumes);
        }

        [Test]
        public void UnixDiskManagerParsesVolumesFromLshwCommandDiskDriveResultsCorrectly_AzureVM_RemoteDisk_AfterPartitioning()
        {
            // Scenario:
            // After Partitioning, the remote disks will now have both a partition and a file system.
            string outputPath = Path.Combine(this.ExamplePath, "lshw.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);
            IEnumerable<Disk> disks = parser.Parse();

            Assert.IsNotNull(disks);
            Assert.IsTrue(disks.Count() == 4);
            Assert.IsNotEmpty(disks.ElementAt(2).Volumes);
            Assert.IsNotEmpty(disks.ElementAt(3).Volumes);
            Assert.IsTrue(disks.ElementAt(2).Volumes.Count() == 1);
            Assert.IsTrue(disks.ElementAt(3).Volumes.Count() == 1);

            IDictionary<string, IConvertible> disk2Volume0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Validate the use of an NTFS file system (vs. Ext4)
                { "id", "volume" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", "GUID:88896b9a-bc02-4290-89a6-f96532fcb0a3" },
                { "description", "Windows NTFS volume" },
                { "physid", "1" },
                { "businfo", "scsi@1:0.0.1,1" },
                { "logicalname", "/dev/sdc1" },
                { "dev", "8:33" },
                { "version", "3.1" },
                { "serial", "3768-a60f" },
                { "size", "1099508481536" },
                { "capacity", "1099509530112" },
                { "clustersize", "4096" },
                { "created", "2021-05-12 00:21:55" },
                { "filesystem", "ntfs" },
                { "mount.fstype", "fuseblk" },
                { "mount.options", "rw,relatime,user_id=0,group_id=0,allow_other,blksize=4096" },
                { "name", "primary" },
                { "state", "mounted" },
                { "ntfs", "Windows NTFS" },
                { "initialized", "initialized volume" }
            };

            this.VerifyDiskProperties(disks.ElementAt(2).Volumes.ElementAt(0).Properties, disk2Volume0ExpectedProperties);

            IDictionary<string, IConvertible> disk3Volume0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // Back to the typical/expected Ext4 volume
                { "id", "volume" },
                { "claimed", "true" },
                { "class", "volume" },
                { "handle", "GUID:dc428d6e-2ef9-42b4-af52-72a03c8cdb10" },
                { "description", "EXT4 volume" },
                { "vendor", "Linux" },
                { "physid", "1" },
                { "businfo", "scsi@1:0.0.0,1" },
                { "logicalname", "/dev/sdd1" },
                { "dev", "8:49" },
                { "version", "1.0" },
                { "serial", "b49ed2f3-fb19-4750-9f9c-de13de45cbb0" },
                { "size", "1099509530624" },
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

            this.VerifyDiskProperties(disks.ElementAt(3).Volumes.ElementAt(0).Properties, disk3Volume0ExpectedProperties);
        }

        [Test]
        public void UnixDiskManagerParsesLshwCommandDiskDriveResultsCorrectly_NVME_OSDisk()
        {
            // Scenario: Before Partitioning
            // Note that this test depends entirely on the schema/content of the resource XML file that contains
            // the "lshw -xml -c disk -c storage" results that is a part of this project. If that file
            // changes, this test will need to be updated to account for new property definitions.
            string outputPath = Path.Combine(this.ExamplePath, "lshw_nvme_osdisk.xml");
            string rawText = File.ReadAllText(outputPath);
            LshwDiskParser parser = new LshwDiskParser(rawText);

            IEnumerable<Disk> disks = parser.Parse();
            Assert.AreEqual(2, disks.Count());

            IDictionary<string, IConvertible> disk0ExpectedProperties = new Dictionary<string, IConvertible>
            {
                // This is the OS Disk
                { "id", "namespace" },
                { "claimed", "true" },
                { "class", "disk" },
                { "handle", "GUID:4284d0de-7575-4738-a5cc-12de3aa3b2ae" },
                { "description", "NVMe namespace" },
                { "physid", "1" },
                { "logicalname", "/dev/nvme0n1,/dev/test" },
                { "size", "68719476736" },
                { "guid", "4284d0de-7575-4738-a5cc-12de3aa3b2ae" },
                { "logicalsectorsize", "512" },
                { "sectorsize", "512" },
                { "gpt-1.00", "GUID Partition Table version 1.00" },
                { "partitioned", "Partitioned disk" },
                { "partitioned:gpt", "GUID partition table" }
            };

            this.VerifyDiskProperties(disks.ElementAt(0).Properties, disk0ExpectedProperties);
        }


        private void VerifyDiskProperties(IDictionary<string, IConvertible> actualProperties, IDictionary<string, IConvertible> expectedProperties)
        {
            Assert.IsNotEmpty(actualProperties);
            Assert.IsEmpty(expectedProperties
                .Select(entry => $"{entry.Key}={entry.Value}")
                .Except(actualProperties.Select(entry => $"{entry.Key}={entry.Value}")));
        }
    }
}