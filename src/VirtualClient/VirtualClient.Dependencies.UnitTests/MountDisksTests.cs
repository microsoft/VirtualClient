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
        private int diskVolumeCount = 0;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
        }

        [Test]
        public async Task MountDisksMountsTheExpectedNumberOfDisks()
        {
            this.SetupDefaultMockBehaviors();

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None);

                this.mockFixture.DiskManager.Verify(mgr => mgr.CreateMountPointAsync(
                   It.IsAny<DiskVolume>(),
                   It.IsAny<string>(),
                   It.IsAny<CancellationToken>()),
                   Times.Exactly(this.diskVolumeCount));
            }
        }

        private void SetupDefaultMockBehaviors()
        {
            this.disks.ToList().ForEach(disk =>
            {
                if (!disk.IsOperatingSystem())
                {
                    // (disk.Volumes as ICollection<DiskVolume>).Clear();
                    disk.Volumes.ToList().ForEach(volume =>
                        volume.AccessPaths = new List<string>());
                    this.diskVolumeCount += disk.Volumes.Count();
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
