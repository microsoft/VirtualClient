// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Microsoft.Extensions.Azure;
    using NUnit.Framework;
    using VirtualClient.Contracts.Parser;

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
        public void DiskFiltersCanFilterOnOsDisk()
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
        public void DiskFiltersCanFilterOnSizeBiggerThan()
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
        public void DiskFiltersCanFilterOnSizeLessThan()
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
            string filterString = "DiskPath:/dev/sdd,/dev/sdf/";
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
    }
}
