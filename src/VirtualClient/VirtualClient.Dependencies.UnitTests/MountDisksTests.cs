// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MountDisksTests
    {
        private MockFixture fixture;
        private IEnumerable<Disk> disks;
        private List<DiskVolume> diskVolumes;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public async Task MountDisksMountsOnExpectedPathForUnix()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                int index = 0;

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string mountPoint = this.fixture.PlatformSpecifics.Combine(this.fixture.PlatformSpecifics.CurrentDirectory, $"{this.fixture.Parameters[nameof(MountDisks.MountPointPrefix)]}_{index}");

                    this.fixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, mountPoint, It.IsAny<CancellationToken>()));
                    index++;
                }

                this.fixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(
                   It.IsAny<DiskVolume>(),
                   It.IsAny<string>(),
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.diskVolumes.Count));
            }
        }

        [Test]
        public async Task MountDisksMountsOnExpectedPathForWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            this.fixture.Parameters[nameof(MountDisks.MountPointPrefix)] = "mockmountpath";

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                int index = 0;

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    string mountPoint = this.fixture.PlatformSpecifics.Combine(this.fixture.PlatformSpecifics.CurrentDirectory, $"{this.fixture.Parameters[nameof(MountDisks.MountPointPrefix)]}_{index}");

                    this.fixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, mountPoint, It.IsAny<CancellationToken>()));
                    index++;
                }

                this.fixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(
                   It.IsAny<DiskVolume>(),
                   It.IsAny<string>(),
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.diskVolumes.Count));
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platformID)
        {
            this.diskVolumes = new List<DiskVolume>();

            this.fixture.Setup(platformID);
            this.disks = this.fixture.CreateDisks(platformID, true);

            this.disks.ToList().ForEach(disk =>
            {
                if (!disk.IsOperatingSystem())
                {
                    // (disk.Volumes as ICollection<DiskVolume>).Clear();
                    disk.Volumes.ToList().ForEach(volume =>
                        volume.AccessPaths = new List<string>());
                    this.diskVolumes.AddRange(disk.Volumes);
                }
            });

            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MountDisks.MountPointPrefix), "/mockmountpath" }
            };
        }
    }
}
