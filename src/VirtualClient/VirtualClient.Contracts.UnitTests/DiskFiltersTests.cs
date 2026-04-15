// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class DiskFiltersTests
    {
        private MockFixture mockFixture;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupMocks();
        }

        [Test]
        public void DiskFiltersCanFilterOnNoneFilter()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 2 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 4 * 1024 * 1024;

            string filterString = "none";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(4, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(2)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(3)));
        }

        [Test]
        public void DiskFiltersAlwaysFiltersOutCDROMDevices()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            Disk cdRom1 = new Disk(4, "/dev/dvd");
            Disk cdRom2 = new Disk(5, "/dev/cdrom");
            this.disks = this.disks.Append(cdRom1).Append(cdRom2);

            string filterString = "none";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(4, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(2)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(3)));
        }

        [Test]
        public void DiskFiltersAlwaysFiltersOutOfflineDisksOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties["Status"] = "Online";
            this.disks.ElementAt(1).Properties["Status"] = "Online";
            this.disks.ElementAt(2).Properties["Status"] = "Offline (Policy)";

            string filterString = "none";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(3, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(2)));
        }

        [Test]
        public void DiskFiltersAlwaysFiltersOutReadOnlyDisksOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(2).Properties["Read-only"] = "Yes";
            this.disks.ElementAt(3).Properties["Current Read-only State"] = "Yes";

            string filterString = "none";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnBiggestDisksOnLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 2 * 1024 * 1024;

            string filterString = "biggestsize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnBiggestDisksOnLinuxForDisksWithoutVolumePartititions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, false);
            this.disks.ElementAt(0).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 2 * 1024 * 1024;

            string filterString = "biggestsize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnBiggestDisksOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties["Size"] = "6 GB";
            this.disks.ElementAt(1).Properties["Size"] = "4 TB";
            this.disks.ElementAt(2).Properties["Size"] = "5TB";
            this.disks.ElementAt(3).Properties["Size"] = "7 mb";

            string filterString = "biggestsize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(0)));
        }

        [Test]
        [Ignore("We can support this in the future if we add a 'Size' property to the Disk object for Windows (e.g. DiskPart -> list disks).")]
        public void DiskFiltersCanFilterOnBiggestDisksOnWindowsForDisksWithoutVolumePartititions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, false);
            this.disks.ElementAt(0).Properties["Size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["Size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["Size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["Size"] = 2 * 1024 * 1024;

            string filterString = "biggestsize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSmallestDisksOnLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 1 * 1024 * 1024;

            string filterString = "smallestSize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSmallestDisksOnLinuxForDisksWithoutVolumePartitions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, false);
            this.disks.ElementAt(0).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 1 * 1024 * 1024;

            string filterString = "smallestSize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSmallestDisksOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties["Size"] = "6 GB";
            this.disks.ElementAt(1).Properties["Size"] = "4 TB";
            this.disks.ElementAt(2).Properties["Size"] = "5TB";
            this.disks.ElementAt(3).Properties["Size"] = "7 mb";

            string filterString = "smallestSize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(0)));
        }

        [Test]
        [Ignore("We can support this in the future if we add a 'Size' property to the Disk object for Windows (e.g. DiskPart -> list disks).")]
        public void DiskFiltersCanFilterOnSmallestDisksOnWindowsForDisksWithoutVolumePartitions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, false);
            this.disks.ElementAt(0).Properties["Size"] = "6 GB";
            this.disks.ElementAt(1).Properties["Size"] = "4 TB";
            this.disks.ElementAt(2).Properties["Size"] = "5TB";
            this.disks.ElementAt(3).Properties["Size"] = "7 mb";

            string filterString = "smallestSize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(0)));
        }

        [Test]
        public void DiskFiltersCanFilterOnOsDiskOnLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            string filterString = "osdisk";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));

            string filterString2 = "osdisk:true";
            IEnumerable<Disk> result2 = DiskFilters.FilterDisks(this.disks, filterString2, PlatformID.Unix);
            Assert.AreEqual(1, result2.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result2.ElementAt(0)));

            string filterString3 = "osdisk:false";
            IEnumerable<Disk> result3 = DiskFilters.FilterDisks(this.disks, filterString3, PlatformID.Unix);
            Assert.AreEqual(3, result3.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result3.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result3.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result3.ElementAt(2)));
        }

        [Test]
        public void DiskFiltersCanFilterOnOsDiskOnLinuxForDisksWithoutVolumePartitions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, false);

            string filterString = "osdisk";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));

            string filterString2 = "osdisk:true";
            IEnumerable<Disk> result2 = DiskFilters.FilterDisks(this.disks, filterString2, PlatformID.Unix);
            Assert.AreEqual(1, result2.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result2.ElementAt(0)));

            string filterString3 = "osdisk:false";
            IEnumerable<Disk> result3 = DiskFilters.FilterDisks(this.disks, filterString3, PlatformID.Unix);
            Assert.AreEqual(3, result3.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result3.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result3.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result3.ElementAt(2)));
        }

        [Test]
        public void DiskFiltersCanFilterOnOsDiskOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);

            string filterString = "osdisk";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));

            string filterString2 = "osdisk:true";
            IEnumerable<Disk> result2 = DiskFilters.FilterDisks(this.disks, filterString2, PlatformID.Win32NT);
            Assert.AreEqual(1, result2.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result2.ElementAt(0)));

            string filterString3 = "osdisk:false";
            IEnumerable<Disk> result3 = DiskFilters.FilterDisks(this.disks, filterString3, PlatformID.Win32NT);
            Assert.AreEqual(3, result3.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result3.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result3.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result3.ElementAt(2)));
        }

        [Test]
        public void DiskFiltersCanFilterOnOsDiskOnWindowsForDisksWithoutVolumePartitions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, false);

            string filterString = "osdisk";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));

            string filterString2 = "osdisk:true";
            IEnumerable<Disk> result2 = DiskFilters.FilterDisks(this.disks, filterString2, PlatformID.Win32NT);
            Assert.AreEqual(1, result2.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result2.ElementAt(0)));

            string filterString3 = "osdisk:false";
            IEnumerable<Disk> result3 = DiskFilters.FilterDisks(this.disks, filterString3, PlatformID.Win32NT);
            Assert.AreEqual(3, result3.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result3.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result3.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result3.ElementAt(2)));
        }

        [Test]
        public void DiskFiltersCanFilterOnBiggestNonOsDisk()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = 1 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = 3 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = 3 * 1024 * 1024;

            string filterString = "osdisk:false&biggestsize";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSizeBiggerThanOnLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = (long)5 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = (long)3 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = (long)7 * 1024 * 1024 * 1024;

            string filterString = "SizeGreaterThan:4gb";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        [Ignore("We can support this in the future if we add a 'Size' property to the Disk object for Windows (e.g. DiskPart -> list disks) and consider that in the filtering.")]
        public void DiskFiltersCanFilterOnSizeBiggerThanOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties["Size"] = (long)5 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["Size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["Size"] = (long)3 * 1024 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["Size"] = (long)7 * 1024 * 1024 * 1024;

            string filterString = "SizeGreaterThan:4gb";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSizeLessThanOnLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = (long)5 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = (long)3 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = (long)7 * 1024 * 1024 * 1024;

            string filterString = "SizeLessThan:4000 Mb";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        [Ignore("We can support this in the future if we add a 'Size' property to the Disk object for Windows (e.g. DiskPart -> list disks) and consider that in the filtering.")]
        public void DiskFiltersCanFilterOnSizeLessThanOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["Size"] = (long)5 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["Size"] = (long)3 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["Size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["Size"] = (long)7 * 1024 * 1024 * 1024;

            string filterString = "SizeLessThan:4000 Mb";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnSizeLessThanAndNonOs()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = (long)4 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = (long)5 * 1024 * 1024 * 1024;
            this.disks.ElementAt(3).Properties["size"] = (long)7 * 1024 * 1024 * 1024;

            string filterString = "SizeLessThan:3000000000 & osdisk:false";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(0, result.Count());
        }

        [Test]
        public void DiskFiltersCanFilterOnSizeEqualTo()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties["size"] = (long)2 * 1024 * 1024 * 1024;
            this.disks.ElementAt(1).Properties["size"] = (long)3 * 1024 * 1024 * 1024;
            this.disks.ElementAt(2).Properties["size"] = (long)(3.1 * 1024 * 1024 * 1024);
            this.disks.ElementAt(3).Properties["size"] = (long)(2.99 * 1024 * 1024 * 1024); // Less than 1% of difference is tolerated in this filter.

            string filterString = "SizeEqualTo:3gb";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnDiskPathInLinux()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            // The disks are sdc, sdd, sde, sdf
            string filterString = "DiskPath:/dev/sdc,/dev/sde";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnDiskPathInLinuxWhenDisksHaveNoVolumePartitions()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, false);

            // The disks are sdc, sdd, sde, sdf
            string filterString = "DiskPath:/dev/sdc,/dev/sde";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Unix);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnDiskPathInWindows()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);

            // The disks C, D, E, F
            string filterString = "DiskPath:C:/,E:";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersCanFilterOnDiskPathInWindowsWhenDisksHaveNoVolumePartitions()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, false);

            // The disks \\.\PHYSICALDISK0,\\.\PHYSICALDISK2
            string filterString = @"DiskPath:\\.\PHYSICALDISK0,\\.\PHYSICALDISK2";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);
            Assert.AreEqual(2, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), result.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.ElementAt(1)));
        }

        [Test]
        public void DiskFiltersHandlesAnomaliesEncounters_1()
        {
            // Scenario:
            // We found an anomaly on one of the Linux systems that had 32 disks where they were not
            // identified properly.
            string rawText = File.ReadAllText(MockFixture.GetDirectory(typeof(DiskFiltersTests), "Examples", "lshw", "lshw_disk_storage_results_anomaly.xml"));
            LshwDiskParser parser = new LshwDiskParser(rawText);

            IEnumerable<Disk> disks = parser.Parse();
            IEnumerable<Disk> filteredDisks = DiskFilters.FilterDisks(disks, "BiggestSize", PlatformID.Unix);

            Assert.IsNotNull(filteredDisks);
            Assert.IsNotEmpty(filteredDisks);
            Assert.IsTrue(filteredDisks.Count() == 32);

            Assert.AreEqual("/dev/sda", filteredDisks.ElementAt(0).DevicePath);
            Assert.AreEqual("/dev/sdb", filteredDisks.ElementAt(1).DevicePath);
            Assert.AreEqual("/dev/sdk", filteredDisks.ElementAt(2).DevicePath);
            Assert.AreEqual("/dev/sdl", filteredDisks.ElementAt(3).DevicePath);
            Assert.AreEqual("/dev/sdm", filteredDisks.ElementAt(4).DevicePath);
            Assert.AreEqual("/dev/sdn", filteredDisks.ElementAt(5).DevicePath);
            Assert.AreEqual("/dev/sdo", filteredDisks.ElementAt(6).DevicePath);
            Assert.AreEqual("/dev/sdp", filteredDisks.ElementAt(7).DevicePath);
            Assert.AreEqual("/dev/sdq", filteredDisks.ElementAt(8).DevicePath);
            Assert.AreEqual("/dev/sdr", filteredDisks.ElementAt(9).DevicePath);
            Assert.AreEqual("/dev/sds", filteredDisks.ElementAt(10).DevicePath);
            Assert.AreEqual("/dev/sdt", filteredDisks.ElementAt(11).DevicePath);
            Assert.AreEqual("/dev/sdc", filteredDisks.ElementAt(12).DevicePath);
            Assert.AreEqual("/dev/sdu", filteredDisks.ElementAt(13).DevicePath);
            Assert.AreEqual("/dev/sdv", filteredDisks.ElementAt(14).DevicePath);
            Assert.AreEqual("/dev/sdw", filteredDisks.ElementAt(15).DevicePath);
            Assert.AreEqual("/dev/sdx", filteredDisks.ElementAt(16).DevicePath);
            Assert.AreEqual("/dev/sdy", filteredDisks.ElementAt(17).DevicePath);
            Assert.AreEqual("/dev/sdz", filteredDisks.ElementAt(18).DevicePath);
            Assert.AreEqual("/dev/sdaa", filteredDisks.ElementAt(19).DevicePath);
            Assert.AreEqual("/dev/sdab", filteredDisks.ElementAt(20).DevicePath);
            Assert.AreEqual("/dev/sdac", filteredDisks.ElementAt(21).DevicePath);
            Assert.AreEqual("/dev/sdad", filteredDisks.ElementAt(22).DevicePath);
            Assert.AreEqual("/dev/sdd", filteredDisks.ElementAt(23).DevicePath);
            Assert.AreEqual("/dev/sdae", filteredDisks.ElementAt(24).DevicePath);
            Assert.AreEqual("/dev/sdaf", filteredDisks.ElementAt(25).DevicePath);
            Assert.AreEqual("/dev/sde", filteredDisks.ElementAt(26).DevicePath);
            Assert.AreEqual("/dev/sdf", filteredDisks.ElementAt(27).DevicePath);
            Assert.AreEqual("/dev/sdg", filteredDisks.ElementAt(28).DevicePath);
            Assert.AreEqual("/dev/sdh", filteredDisks.ElementAt(29).DevicePath);
            Assert.AreEqual("/dev/sdi", filteredDisks.ElementAt(30).DevicePath);
            Assert.AreEqual("/dev/sdj", filteredDisks.ElementAt(31).DevicePath);
        }

        [Test]
        public void DiskFiltersIncludeOfflineFilterKeepsOfflineDisksOnWindows()
        {
            // Arrange: create 4 disks; mark one as offline.
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties["Status"] = "Online";
            this.disks.ElementAt(1).Properties["Status"] = "Online";
            this.disks.ElementAt(2).Properties["Status"] = "Offline (Policy)";
            this.disks.ElementAt(3).Properties["Status"] = "Online";

            // "none" alone would remove the offline disk; "none&IncludeOffline" should retain it.
            string filterString = "none&IncludeOffline";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);

            Assert.AreEqual(4, result.Count());
            Assert.IsTrue(result.Any(d => d.Properties["Status"].ToString().Contains("Offline")));
        }

        [Test]
        public void DiskFiltersIncludeOfflineFilterIsCaseInsensitive()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(1).Properties["Status"] = "Offline";

            // All casing variants should be accepted.
            foreach (string variant in new[] { "none&IncludeOffline", "none&includeoffline", "none&INCLUDEOFFLINE" })
            {
                IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, variant, PlatformID.Win32NT);
                Assert.AreEqual(4, result.Count(), $"Expected offline disk retained for filter '{variant}'");
            }
        }

        [Test]
        public void DiskFiltersWithoutIncludeOfflineDoesNotRetainOfflineDisksOnWindows()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(2).Properties["Status"] = "Offline (Policy)";

            // Default behaviour: the offline disk is excluded.
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, "none", PlatformID.Win32NT);

            Assert.AreEqual(3, result.Count());
            Assert.IsFalse(result.Any(d => d.Properties.ContainsKey("Status") &&
                d.Properties["Status"].ToString().Contains("Offline")));
        }

        [Test]
        public void DiskFiltersIncludeOfflineCanBeCombinedWithBiggestSizeFilter()
        {
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);
            // Make the offline disk the biggest.
            this.disks.ElementAt(0).Properties["Size"] = "100 GB";
            this.disks.ElementAt(1).Properties["Size"] = "100 GB";
            this.disks.ElementAt(2).Properties["Size"] = "2000 GB";   // offline + biggest
            this.disks.ElementAt(2).Properties["Status"] = "Offline";
            this.disks.ElementAt(3).Properties["Size"] = "100 GB";

            string filterString = "BiggestSize&IncludeOffline";
            IEnumerable<Disk> result = DiskFilters.FilterDisks(this.disks, filterString, PlatformID.Win32NT);

            Assert.AreEqual(1, result.Count());
            Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), result.First()));
        }

        // -----------------------------------------------------------------------
        // TryGetDiskIndexes tests
        // -----------------------------------------------------------------------

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ReturnsFalseForNonDiskIndexFilter()
        {
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes("BiggestSize", out _));
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes("none", out _));
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes("osdisk:false", out _));
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes(null, out _));
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes(string.Empty, out _));
            Assert.IsFalse(DiskFilters.TryGetDiskIndexes("   ", out _));
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesDashRange_CorrectCount()
        {
            bool result = DiskFilters.TryGetDiskIndexes("DiskIndex:6-10", out IEnumerable<int> indexes);

            Assert.IsTrue(result);
            Assert.IsNotNull(indexes);
            // Indices 6, 7, 8, 9, 10 → 5 entries
            Assert.AreEqual(5, indexes.Count());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesDashRange_IndicesAreCorrect()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex:6-10", out IEnumerable<int> indexes);

            CollectionAssert.AreEqual(new[] { 6, 7, 8, 9, 10 }, indexes.ToList());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesDashRange_SingleDisk()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex:42-42", out IEnumerable<int> indexes);

            Assert.AreEqual(1, indexes.Count());
            Assert.AreEqual(42, indexes.First());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesDashRange_IgnoresWhitespace()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex: 6 - 8 ", out IEnumerable<int> indexes);

            Assert.AreEqual(3, indexes.Count());
            CollectionAssert.AreEqual(new[] { 6, 7, 8 }, indexes.ToList());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesDashRange_LargeJBODRange()
        {
            bool result = DiskFilters.TryGetDiskIndexes("DiskIndex:6-180", out IEnumerable<int> indexes);

            Assert.IsTrue(result);
            Assert.AreEqual(175, indexes.Count());
            Assert.AreEqual(6, indexes.First());
            Assert.AreEqual(180, indexes.Last());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesCommaSeparatedList_CorrectCount()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex:6,10,15", out IEnumerable<int> indexes);

            Assert.AreEqual(3, indexes.Count());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesCommaSeparatedList_IndicesAreCorrect()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex:6,10,15", out IEnumerable<int> indexes);

            CollectionAssert.AreEqual(new[] { 6, 10, 15 }, indexes.ToList());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_ParsesCommaSeparatedList_IgnoresWhitespace()
        {
            DiskFilters.TryGetDiskIndexes("DiskIndex: 6 , 7 , 8 ", out IEnumerable<int> indexes);

            CollectionAssert.AreEqual(new[] { 6, 7, 8 }, indexes.ToList());
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_IsCaseInsensitive()
        {
            Assert.IsTrue(DiskFilters.TryGetDiskIndexes("diskindex:6-8", out _));
            Assert.IsTrue(DiskFilters.TryGetDiskIndexes("DISKINDEX:6-8", out _));
            Assert.IsTrue(DiskFilters.TryGetDiskIndexes("DiskIndex:6-8", out _));
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_HddSentinel_ReturnsTrueWithNullIndexes()
        {
            bool result = DiskFilters.TryGetDiskIndexes("DiskIndex:hdd", out IEnumerable<int> indexes);

            Assert.IsTrue(result, "Expected true for the 'hdd' auto-discover sentinel.");
            Assert.IsNull(indexes, "Expected null indexes for the 'hdd' sentinel — caller should use OS discovery.");
        }

        [Test]
        public void DiskFiltersTryGetDiskIndexes_HddSentinel_IsCaseInsensitive()
        {
            Assert.IsTrue(DiskFilters.TryGetDiskIndexes("DiskIndex:HDD", out IEnumerable<int> i1));
            Assert.IsNull(i1);
            Assert.IsTrue(DiskFilters.TryGetDiskIndexes("DiskIndex:hdd", out IEnumerable<int> i2));
            Assert.IsNull(i2);
        }
    }
}
