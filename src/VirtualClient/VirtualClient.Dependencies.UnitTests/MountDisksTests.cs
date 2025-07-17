// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MountDisksTests
    {
        private MockFixture mockFixture;
        private IEnumerable<Disk> disks;
        private List<DiskVolume> diskVolumes;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnUnix()
        {
            this.SetupTest(PlatformID.Unix);

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string expectedMountPoint = this.mockFixture.Combine(expectedMountDirectory, diskVolume.GetDefaultMountPointName());

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnUnixWhenMultipleVolumesArePresent()
        {
            // Expected:
            // 3 volumes/partitions on the disk == 3 mount points
            this.SetupTest(PlatformID.Unix, withMultipleVolumes: true);

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string expectedMountPoint = this.mockFixture.Combine(expectedMountDirectory, diskVolume.GetDefaultMountPointName());

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnUnixWhenAMountPrefixIsProvided()
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters["MountPointPrefix"] = "mnt_test";

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

                    string expectedMountPoint = this.mockFixture.Combine(
                        expectedMountDirectory, 
                        diskVolume.GetDefaultMountPointName(prefix: "mnt_test"));

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnUnixWhenAMountLocationIsProvided_Root()
        {
            this.SetupTest(PlatformID.Unix);
            this.mockFixture.Parameters["MountLocation"] = "Root";

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountPoint = this.mockFixture.Combine("/", diskVolume.GetDefaultMountPointName());
                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnWindows()
        {
            this.SetupTest(PlatformID.Win32NT);

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string expectedMountPoint = this.mockFixture.Combine(expectedMountDirectory, diskVolume.GetDefaultMountPointName());

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnWindowsWhenMultipleVolumesArePresent()
        {
            // Expected:
            // 3 volumes/partitions on the disk == 3 mount points
            this.SetupTest(PlatformID.Win32NT, withMultipleVolumes: true);

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string expectedMountPoint = this.mockFixture.Combine(expectedMountDirectory, diskVolume.GetDefaultMountPointName());

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        [Test]
        public async Task MountDisksMountsTheExpectedPathOnWindowsWhenAMountPrefixIsProvided()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.mockFixture.Parameters["MountPointPrefix"] = "mnt_test";

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string expectedMountDirectory = this.mockFixture.StandardizePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    string expectedMountPoint = this.mockFixture.Combine(expectedMountDirectory, diskVolume.GetDefaultMountPointName(prefix: "mnt_test"));

                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, expectedMountPoint, It.IsAny<CancellationToken>()));
                }
            }
        }

        private void SetupTest(PlatformID platformID, bool withMultipleVolumes = false)
        {
            this.diskVolumes = new List<DiskVolume>();

            this.mockFixture.Setup(platformID);
            this.disks = this.mockFixture.CreateDisks(platformID, true);

            if (withMultipleVolumes)
            {
                List<Disk> disksWithMultipleVolumes = new List<Disk>();

                if (platformID == PlatformID.Win32NT)
                {
                    disksWithMultipleVolumes.AddRange(new List<Disk>
                    {
                        // OS disk
                        this.mockFixture.CreateDisk(0, platformID, true, @"\\.\PHYSICALDISK0", "C:\\"),

                        // Data disks
                        this.mockFixture.CreateDisk(1, platformID, false, @"\\.\PHYSICALDISK1", "D:\\", "E:\\"),
                        this.mockFixture.CreateDisk(2, platformID, false, @"\\.\PHYSICALDISK2", "F:\\", "G:\\"),
                        this.mockFixture.CreateDisk(3, platformID, false, @"\\.\PHYSICALDISK3", "H:\\", "I:\\"),
                    });
                }
                else if (platformID == PlatformID.Unix)
                {
                    disksWithMultipleVolumes.AddRange(new List<Disk>
                    {
                        // OS disk
                        this.mockFixture.CreateDisk(0, platformID, true, "/dev/sda", "/dev/sda1"),

                        // Data disks
                        this.mockFixture.CreateDisk(1, platformID, false, "/dev/sdc", "/dev/sdc1", "/dev/sdc2"),
                        this.mockFixture.CreateDisk(2, platformID, false, "/dev/sdd", "/dev/sdd1", "/dev/sdd2"),
                        this.mockFixture.CreateDisk(3, platformID, false, "/dev/sde", "/dev/sde1", "/dev/sde2")
                    });
                }

                this.disks = disksWithMultipleVolumes;
            }

            this.disks.ToList().ForEach(disk =>
            {
                if (!disk.IsOperatingSystem())
                {
                    // Ensure none of the disks have existing mount points/access paths.
                    disk.Volumes.ToList().ForEach(volume => volume.AccessPaths = new List<string>());
                    this.diskVolumes.AddRange(disk.Volumes);
                }
            });

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
        }
    }
}
