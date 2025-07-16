// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DiskExtensionsTests
    {
        private DependencyFixture fixture;
        private IEnumerable<Disk> disks;

        [OneTimeSetUp]
        public void SetupTests()
        {
            this.fixture = new DependencyFixture();
        }

        [Test]
        public void DiskExtensionsReadSizeCorrectlyInWindows()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);
            // In mock it was 123GB
            Assert.AreEqual((long)123 * 1024 * 1024 * 1024, this.disks.ElementAt(0).SizeInBytes(PlatformID.Win32NT));
        }

        [Test]
        public void DiskExtensionsReadSizeReturnZeroIfSizeNotDefinedInWindows()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties.Remove("Size");
            this.disks.ElementAt(0).Volumes.ElementAt(0).Properties.Remove("Size");
            Assert.AreEqual(0, this.disks.ElementAt(0).SizeInBytes(PlatformID.Win32NT));
        }

        [Test]
        public void DiskExtensionsReadSizeReturnTotalVolumeSizeIfDiskSizeIsZeroInWindows()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);
            this.disks.ElementAt(0).Properties.Remove("Size");
            // In mock the volume size is 123GB
            Assert.AreEqual(123 * Math.Pow(1024,3), this.disks.ElementAt(0).SizeInBytes(PlatformID.Win32NT));
        }

        [Test]
        public void DiskExtensionsReadSizeCorrectlyInLinux()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);
            // In mock it was 1234567890123
            Assert.AreEqual((long)1234567890, this.disks.ElementAt(0).SizeInBytes(PlatformID.Unix));
        }

        [Test]
        public void DiskExtensionsReadSizeReturnZeroIfSizeNotDefinedInUnix()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties.Remove("size");
            this.disks.ElementAt(0).Volumes.ElementAt(0).Properties.Remove("size");
            Assert.AreEqual(0, this.disks.ElementAt(0).SizeInBytes(PlatformID.Unix));
        }

        [Test]
        public void DiskExtensionsReadSizeReturnTotalVolumeSizeIfDiskSizeIsZeroInLinux()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);
            this.disks.ElementAt(0).Properties.Remove("size");
            Assert.AreEqual(1234567890, this.disks.ElementAt(0).SizeInBytes(PlatformID.Unix));
        }

        [Test]
        public void DiskExtensionsDeterminesIfADiskVolumeIsOperatingSystemInUnix()
        {
            DiskVolume volume = new DiskVolume(0);
            List<string> osPaths = new List<string> { "/" };
            volume = new DiskVolume(0, accessPaths: osPaths);
            Assert.IsTrue(volume.IsOperatingSystem());

            osPaths = new List<string>();
            volume = new DiskVolume(0, accessPaths: osPaths);
            Assert.IsFalse(volume.IsOperatingSystem());

            osPaths = new List<string> { "a", "/", "b"};
            volume = new DiskVolume(0, accessPaths: osPaths);
            Assert.IsTrue(volume.IsOperatingSystem());

            osPaths = new List<string> { "a", "c", "b" };
            volume = new DiskVolume(0, accessPaths: osPaths);
            Assert.IsFalse(volume.IsOperatingSystem());
        }

        [Test]
        public void DiskExtensionsDeterminesIfADiskVolumeIsOperatingSystemInWindows()
        {
            DiskVolume volume = new DiskVolume(0);
            Assert.IsFalse(volume.IsOperatingSystem());

            volume.Properties["Info"] = "Boot";
            Assert.IsTrue(volume.IsOperatingSystem());

            volume.Properties["Info"] = "System";
            Assert.IsFalse(volume.IsOperatingSystem());

            volume.Properties["Info"] = "SomethingElse";
            Assert.IsFalse(volume.IsOperatingSystem());
        }

        [Test]
        public void GetDefaultMountPointNameExtensionReturnsTheExpectedNameOnWindowsSystems()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);

            foreach (DiskVolume volume in this.disks.SelectMany(disk => disk.Volumes))
            {
                string expectedMountPointName = $"mnt_{volume.DevicePath.Replace(":", string.Empty).Replace("\\", string.Empty)}".ToLowerInvariant();
                string actualMountPointName = volume.GetDefaultMountPointName();

                Assert.AreEqual(expectedMountPointName, actualMountPointName);
            }
        }

        [Test]
        public void GetDefaultMountPointNameExtensionReturnsTheExpectedNameOnWindowsSystemsHavingDisksWithMultipleVolumes()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);

            foreach (Disk disk in this.disks)
            {
                foreach (DiskVolume volume in disk.Volumes)
                {
                    string expectedMountPointName = $"mnt_{volume.DevicePath.Replace(":", string.Empty).Replace("\\", string.Empty)}".ToLowerInvariant();
                    string actualMountPointName = volume.GetDefaultMountPointName();

                    Assert.AreEqual(expectedMountPointName, actualMountPointName);
                }
            }
        }

        [Test]
        public void GetDefaultMountPointNameExtensionReturnsTheExpectedNameOnUnixSystems()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);

            foreach (DiskVolume volume in this.disks.SelectMany(disk => disk.Volumes))
            {
                string expectedMountPointName = $"mnt_{volume.DevicePath.Substring(1).Replace("/", "_")}".ToLowerInvariant();
                string actualMountPointName = volume.GetDefaultMountPointName();

                Assert.AreEqual(expectedMountPointName, actualMountPointName);
            }
        }

        [Test]
        public void GetDefaultMountPointNameExtensionReturnsTheExpectedNameOnUnixSystemsHavingDisksWithMultipleVolumes()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);

            foreach (Disk disk in this.disks.Skip(1))
            {
                foreach (DiskVolume volume in disk.Volumes)
                {
                    string expectedMountPointName = $"mnt_{volume.DevicePath.Substring(1).Replace("/", "_")}".ToLowerInvariant();
                    string actualMountPointName = volume.GetDefaultMountPointName();

                    Assert.AreEqual(expectedMountPointName, actualMountPointName);
                }
            }
        }

        [Test]
        public void GetDefaultMountPointNameExtensionUsesASpecificPrefixWhenProvided()
        {
            this.disks = this.fixture.CreateDisks(PlatformID.Unix, true);

            string expectedPrefix = "mnt_vc";
            foreach (DiskVolume volume in this.disks.SelectMany(disk => disk.Volumes))
            {
                string expectedMountPointName = $"{expectedPrefix}_{volume.DevicePath.Substring(1).Replace("/", "_")}".ToLowerInvariant();
                string actualMountPointName = volume.GetDefaultMountPointName(prefix: expectedPrefix);

                Assert.AreEqual(expectedMountPointName, actualMountPointName);
            }
        }
    }
}
