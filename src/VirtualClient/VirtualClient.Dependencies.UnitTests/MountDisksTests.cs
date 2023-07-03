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

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
        }

        [Test]
        public async Task MountDisksMountsTheDisksWithUserProvidedName()
        {
            this.SetupDefaultMockBehaviors();

            List<Disk> disksMounted = new List<Disk>();

            using (MountDisks diskMounter = new MountDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await diskMounter.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotEmpty(disksMounted);
                CollectionAssert.AreEquivalent(this.disks.Where(disk => !disk.Volumes.Any()), disksMounted);
            }
        }

        private void SetupDefaultMockBehaviors()
        {
            Assert.IsTrue(this.disks.Any(disk => disk.IsOperatingSystem()), "Mock disks should contain an OS disk");

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

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MountDisks.MountPointName), "/mlperftraining" },
                { nameof(MountDisks.DiskFilter), "BiggestSize" }
            };
        }
    }
}
