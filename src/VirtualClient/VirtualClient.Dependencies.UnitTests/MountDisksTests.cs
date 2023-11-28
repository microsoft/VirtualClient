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
        public async Task MountDisksMountsOnExpectedPathForUnix()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                int index = 0;

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, $"{this.mockFixture.Parameters[nameof(MountDisks.MountPointPrefix)]}{index}", It.IsAny<CancellationToken>()));
                    index++;
                }

                this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(
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
            this.mockFixture.Parameters[nameof(MountDisks.MountPointPrefix)] = "C:\\mockmountpath";

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                int index = 0;

                foreach (DiskVolume diskVolume in this.diskVolumes)
                {
                    this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(diskVolume, $"{this.mockFixture.Parameters[nameof(MountDisks.MountPointPrefix)]}{index}", It.IsAny<CancellationToken>()));
                    index++;
                }

                this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(
                   It.IsAny<DiskVolume>(),
                   It.IsAny<string>(),
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.diskVolumes.Count));
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platformID)
        {
            this.diskVolumes = new List<DiskVolume>();

            this.mockFixture.Setup(platformID);
            this.disks = this.mockFixture.CreateDisks(platformID, true);

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

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);
            
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MountDisks.MountPointPrefix), "/mockmountpath" }
            };
        }
    }
}
