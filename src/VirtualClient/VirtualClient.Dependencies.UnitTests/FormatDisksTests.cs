// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FormatDisksTests
    {
        private MockFixture mockFixture;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true).Skip(1);
        }

        [Test]
        [TestCase(PlatformID.Unix, PartitionType.Gpt)]
        [TestCase(PlatformID.Win32NT, PartitionType.Gpt)]
        public async Task FormatDisksUsesTheExpectedPartitionTypeWhenInitializingDisksOnAGivenPlatform(PlatformID platform, PartitionType expectedPartitionType)
        {
            this.mockFixture.Setup(platform);
            this.disks = this.mockFixture.CreateDisks(platform, true);
            this.SetupTest();

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   expectedPartitionType,
                   It.IsAny<FileSystemType>(),
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.disks.Count(disk => !disk.Volumes.Any())));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, FileSystemType.Ext4)]
        [TestCase(PlatformID.Win32NT, FileSystemType.Ntfs)]
        public async Task FormatDisksUsesTheExpectedFileSystemTypeWhenFormattingDisksOnAGivenPlatform(PlatformID platform, FileSystemType expectedFileSystemType)
        {
            this.mockFixture.Setup(platform);
            this.disks = this.mockFixture.CreateDisks(platform, true);
            this.SetupTest();

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   It.IsAny<PartitionType>(),
                   expectedFileSystemType,
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.disks.Count(disk => !disk.Volumes.Any())));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, FileSystemType.Ext4)]
        public async Task FormatDisksWillParallelizeTheDiskFormattingOperationsWhenInstructedOnAGivenPlatform(PlatformID platform, FileSystemType expectedFileSystemType)
        {
            this.mockFixture.Setup(platform);
            this.disks = this.mockFixture.CreateDisks(platform, true);
            this.SetupTest();

            // Instruct the executor to format in-parallel
            this.mockFixture.Parameters[nameof(FormatDisks.InitializeDisksInParallel)] = true;

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                // It is difficult to evaluate whether the operations ran in pure parallel. We are simply evaluating
                // that the each of the format calls/per disk was made for now.
                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   It.IsAny<PartitionType>(),
                   expectedFileSystemType,
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.disks.Count(disk => !disk.Volumes.Any())));
            }
        }

        [Test]
        public async Task FormatDisksWillNotFormatTheOperatingSystemDisk_Scenario1()
        {
            this.SetupTest();

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);
            }

            this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                It.Is<Disk>(disk => disk.IsOperatingSystem()),
                It.IsAny<PartitionType>(),
                It.IsAny<FileSystemType>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        public async Task FormatDisksWillNotFormatTheOperatingSystemDisk_Scenario2()
        {
            this.SetupTest();

            // This is entirely an unexpected scenario. However, if for some reason the disk identified
            // as the OS disk itself is not showing any partitions, we should still NOT attempt to format
            // it.
            this.disks.Where(disk => disk.IsOperatingSystem()).ToList().ForEach(disk => (disk.Volumes as List<DiskVolume>).Clear());

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);
            }

            this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                It.Is<Disk>(disk => disk.IsOperatingSystem()),
                It.IsAny<PartitionType>(),
                It.IsAny<FileSystemType>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Test]
        [TestCase(PlatformID.Unix, FileSystemType.Ext4)]
        [TestCase(PlatformID.Win32NT, FileSystemType.Ntfs)]
        public async Task FormatDisksWillFormatExpectedDisksRegardlessOfStateWhenTheForceParameterIsUsed_Scenario1(PlatformID platform, FileSystemType expectedFileSystemType)
        {
            this.mockFixture.Setup(platform);

            // Scenario:
            // The disks are NOT already partitioned/formatted. We re-partition and format them anyway.
            this.disks = this.mockFixture.CreateDisks(platform, false);
            this.SetupTest();

            this.mockFixture.Parameters["Force"] = true;
            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   It.IsAny<PartitionType>(),
                   expectedFileSystemType,
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.disks.Count(disk => !disk.IsOperatingSystem())));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, FileSystemType.Ext4)]
        [TestCase(PlatformID.Win32NT, FileSystemType.Ntfs)]
        public async Task FormatDisksWillFormatExpectedDisksRegardlessOfStateWhenTheForceParameterIsUsed_Scenario2(PlatformID platform, FileSystemType expectedFileSystemType)
        {
            this.mockFixture.Setup(platform);

            // Scenario:
            // The disks are already partitioned/formatted. We re-partition and format them.
            this.disks = this.mockFixture.CreateDisks(platform, true);
            this.SetupTest();

            this.mockFixture.Parameters["Force"] = true;
            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   It.IsAny<PartitionType>(),
                   expectedFileSystemType,
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.disks.Count(disk => !disk.IsOperatingSystem())));
            }
        }

        [Test]
        public async Task FormatDisksExcludesNonFormattableDevices()
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            Disk cdRom1 = new Disk(4, "/dev/dvd");
            Disk cdRom2 = new Disk(5, "/dev/cdrom");
            this.disks = this.disks.Append(cdRom1).Append(cdRom2);

            this.SetupTest();

            this.mockFixture.Parameters["DiskFilter"] = "none";

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);
            }

            this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                It.IsAny<Disk>(),
                It.IsAny<PartitionType>(),
                It.IsAny<FileSystemType>(),
                It.IsAny<CancellationToken>()),
                Times.Exactly(this.disks.Count(disk => !disk.Volumes.Any()) - 2));
        }

        [Test]
        public async Task FormatDisksWillDoNothingIfAllDisksAreAlreadyFormatted()
        {
            this.SetupTest();

            this.disks.ToList().ForEach(disk =>
            {
                if (!disk.Volumes.Any())
                {
                    (disk.Volumes as ICollection<DiskVolume>).Add(new DiskVolume(0));
                }
            });

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.FormatDiskAsync(
                   It.IsAny<Disk>(),
                   It.IsAny<PartitionType>(),
                   It.IsAny<FileSystemType>(),
                   It.IsAny<CancellationToken>()),
                   Times.Never);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task FormatDisksWillAttemptToFormatOnlyDisksThatHaveNotAlreadyBeenFormatted()
        {
            this.SetupTest();

            List<Disk> disksFormatted = new List<Disk>();

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.FormatDiskAsync(
                    It.IsAny<Disk>(),
                    It.IsAny<PartitionType>(),
                    It.IsAny<FileSystemType>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Disk, PartitionType, FileSystemType, CancellationToken>((disk, partitionType, fileSystemType, token) =>
                {
                    Assert.IsFalse(disk.Volumes.Any());
                    Assert.IsFalse(disk.Volumes.Any());
                    disksFormatted.Add(disk);
                })
                .Returns(Task.CompletedTask);

            using (FormatDisks diskFormatter = new FormatDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                diskFormatter.WaitTime = TimeSpan.Zero;
                await diskFormatter.ExecuteAsync(CancellationToken.None);

                Assert.IsNotEmpty(disksFormatted);
                CollectionAssert.AreEquivalent(this.disks.Where(disk => !disk.Volumes.Any()), disksFormatted);
            }
        }

        private void SetupTest()
        {
            // The default mock behaviors are those that create a "happy path". In the default
            // path, there are disks that need to be formatted and are successfully. This ensures
            // the full set of code paths are run by default. The default behavior is then adjusted in
            // the unit tests above to validate different scenarios.

            this.disks.ToList().ForEach(disk =>
            {
                // Ensure the remote disks have no partitions and thus are in a ready-to-format
                // state.
                if (!disk.IsOperatingSystem())
                {
                    (disk.Volumes as ICollection<DiskVolume>).Clear();
                }
            });

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);
        }
    }
}
