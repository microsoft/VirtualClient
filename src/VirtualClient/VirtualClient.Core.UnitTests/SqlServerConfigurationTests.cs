// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SqlServerConfigurationTests
    {
        private MockFixture mockFixture;
        private List<Disk> mockDisks;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockDisks = new List<Disk>
            {
                // OS disk
                this.mockFixture.CreateDisk(0, platform: PlatformID.Win32NT, os: true, "DISK0", @"C:\"),

                // Remote Disk
                this.mockFixture.CreateDisk(1, platform: PlatformID.Win32NT, os: false, "DISK1", @"D:\"),
                this.mockFixture.CreateDisk(2, platform: PlatformID.Win32NT, os: false, "DISK2", @"E:\"),
                this.mockFixture.CreateDisk(3, platform: PlatformID.Win32NT, os: false, "DISK3", @"F:\"),
                this.mockFixture.CreateDisk(4, platform: PlatformID.Win32NT, os: false, "DISK4", @"G:\"),
                this.mockFixture.CreateDisk(5, platform: PlatformID.Win32NT, os: false, "DISK5", @"H:\"),
                this.mockFixture.CreateDisk(6, platform: PlatformID.Win32NT, os: false, "DISK6", @"I:\"),
                this.mockFixture.CreateDisk(7, platform: PlatformID.Win32NT, os: false, "DISK7", @"J:\"),
            };
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK0")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK0")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK0")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK0")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisks_OperatingSystem_Disk_Only_Scenario(string fileType, string expectedDisk)
        {
            IEnumerable<Disk> disks = this.mockDisks.Where(d => d.IsOperatingSystem());
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK2")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK2")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisks_Single_Managed_Disk_Scenario(string fileType, string expectedDisk)
        {
            IEnumerable<Disk> disks = this.mockDisks.Take(3);
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK2")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK3")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK3")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisks_Two_Managed_Disks_Scenario(string fileType, string expectedDisk)
        {
            // Data files on 1 disk. Log files on the second disk.
            IEnumerable<Disk> disks = this.mockDisks.Take(4);
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK2")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK3")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK4")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisks_Three_Managed_Disks_Scenario(string fileType, string expectedDisk)
        {
            // TempDB data + log files on their own disk
            IEnumerable<Disk> disks = this.mockDisks.Take(5);
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK2")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK3")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK4")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisks_Four_Managed_Disks_Scenario(string fileType, string expectedDisk)
        {
            // Database, transaction log, TempDB data, and TempDB log files on their own disk
            IEnumerable<Disk> disks = this.mockDisks.Take(6);
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }

        [Test]
        [TestCase(SqlServerConfiguration.DataFilePath, "DISK1")]
        [TestCase(SqlServerConfiguration.LogFilePath, "DISK2")]
        [TestCase(SqlServerConfiguration.TempDBDataFilePath, "DISK3")]
        [TestCase(SqlServerConfiguration.TempDBLogFilePath, "DISK4")]
        public void SqlServerConfigurationMapsTheDatabaseFileTypesToTheExpectedDisksWhenThereAreMoreDisksThanNeeded(string fileType, string expectedDisk)
        {
            IEnumerable<Disk> disks = this.mockDisks;
            Disk selectedDisk = SqlServerConfiguration.GetDiskFromFileType(fileType, disks);

            Assert.IsNotNull(selectedDisk);
            Assert.IsTrue(object.ReferenceEquals(this.mockDisks.First(d => d.DevicePath == expectedDisk), selectedDisk));
        }
    }
}
