// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using VirtualClient.Common;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Contracts;
    using VirtualClient.Properties;

    [TestFixture]
    [Category("Unit")]
    public class UnixDiskManagerTests
    {
        private UnixDiskManager diskManager;
        private InMemoryProcessManager processManager;
        private InMemoryProcess testProcess;
        private InMemoryStream standardInput;

        [SetUp]
        public void SetupTest()
        {
            this.processManager = new InMemoryProcessManager(PlatformID.Unix)
            {
                // Return our test process on creation.
                OnCreateProcess = (command, args, workingDir) =>
                {
                    this.testProcess.StartInfo.FileName = command;
                    this.testProcess.StartInfo.Arguments = args;
                    return this.testProcess;
                }
            };

            this.standardInput = new InMemoryStream();
            this.diskManager = new UnixDiskManager(this.processManager)
            {
                RetryPolicy = Policy.NoOpAsync(),
                WaitTime = TimeSpan.Zero // Wait time in-between individual process calls.
            };

            this.testProcess = new InMemoryProcess(this.standardInput);
        }

        [Test]
        public async Task UnixDiskManagerUsesTheSystemInstalledInstanceOfTheLshwCommandByDefault()
        {
            // Setup the process execution to start, mimic an exit and to have the lshw command
            // results in the standard output.
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;
            this.testProcess.StandardOutput.Append(Resources.lshw_disk_storage_results);

            // The instance of lshw that is installed by default on the Linux system.
            string expectedCommand = "lshw";

            await this.diskManager.GetDisksAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual("sudo", this.testProcess.StartInfo.FileName);
            Assert.IsTrue(this.testProcess.StartInfo.Arguments.StartsWith(expectedCommand));
        }

        [Test]
        public async Task UnixDiskManagerUsesACustomInstanceOfTheLshwCommandWhenAPathToItIsSupplied()
        {
            // Setup the process execution to start, mimic an exit and to have the lshw command
            // results in the standard output.
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;
            this.testProcess.StandardOutput.Append(Resources.lshw_disk_storage_results);

            // Use a custom instance of lshw vs. the default installation on the system.
            string expectedCommand = "/any/path/to/custom/built/lshw";
            this.diskManager.LshwExecutable = expectedCommand;

            await this.diskManager.GetDisksAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual("sudo", this.testProcess.StartInfo.FileName);
            Assert.IsTrue(this.testProcess.StartInfo.Arguments.StartsWith(expectedCommand));
        }

        [Test]
        public async Task UnixDiskManagerCallsTheExpectedCommandsToGetDiskInformation()
        {
            List<string> commandsExecuted = new List<string>();

            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            // Capture the commands ran for each process
            this.testProcess.OnStart = () =>
            {
                this.testProcess.StandardOutput.Clear();
                this.testProcess.StandardOutput.Append(Resources.lshw_disk_storage_results);
                commandsExecuted.Add($"{this.testProcess.StartInfo.FileName} {this.testProcess.StartInfo.Arguments}");

                return true;
            };

            await this.diskManager.GetDisksAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(commandsExecuted.Count == 1);
            Assert.IsTrue(commandsExecuted.ElementAt(0) == "sudo lshw -xml -c disk -c volume");
        }

        [Test]
        public void UnixDiskManagerThrowsIfTheLshwCommandDoesNotReturnAnyDiskDriveInformationInTheResults()
        {
            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () =>
            {
                // No results in standard output
                this.testProcess.StandardOutput.Clear();
                return true;
            };

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.GetDisksAsync(CancellationToken.None));
        }

        [Test]
        public void UnixDiskManagerThrowsIfTheLshwCommandReturnsAnErrorCodeWhenAttemptingToGetDiskDriveInformation()
        {
            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () =>
            {
                // lshw returned an error code.
                this.testProcess.ExitCode = 123;
                return true;
            };

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.GetDisksAsync(CancellationToken.None));
        }

        [Test]
        public void UnixDiskManagerAppliesRetriesOnFailedAttemptsToGetDisks()
        {
            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            int attempts = 0;
            this.testProcess.OnStart = () =>
            {
                attempts++;
                throw new ProcessException();
            };

            this.diskManager.RetryPolicy = Policy.Handle<ProcessException>().WaitAndRetryAsync(3, (retries) => TimeSpan.Zero);

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.GetDisksAsync(CancellationToken.None));
            Assert.IsTrue(attempts == 4);
        }

        [Test]
        [TestCase(PartitionType.MsDos, FileSystemType.MsDos)]
        [TestCase(PartitionType.Gpt, FileSystemType.Ntfs)]
        [TestCase(PartitionType.Bsd, FileSystemType.Ext3)]
        [TestCase(PartitionType.Gpt, FileSystemType.Ext4)]
        public async Task UnixDiskManagerCallsTheExpectedCommandsToFormatADiskInitially(PartitionType expectedPartitionType, FileSystemType expectedFileSystemType)
        {
            List<string> commandsExecuted = new List<string>();

            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            // Capture the commands ran for each process
            this.testProcess.OnStart = () =>
            {
                this.testProcess.StandardOutput.Clear();
                commandsExecuted.Add($"{this.testProcess.StartInfo.FileName} {this.testProcess.StartInfo.Arguments}");

                return true;
            };

            Disk disk = new Disk(0, "/dev/sdc");

            await this.diskManager.FormatDiskAsync(disk, expectedPartitionType, expectedFileSystemType, CancellationToken.None)
                .ConfigureAwait(false);

            List<string> commandsExpected = new List<string>
            {
                // The new partition can now be created
                $"sudo partprobe",
                $"sudo parted -s /dev/sdc mklabel {expectedPartitionType.ToString().ToLower()}",
                $"sudo parted -s -a optimal /dev/sdc mkpart primary 0% 100%",
                $"sudo partprobe",

                // And the new file system can be created
                $"sudo mkfs -t {expectedFileSystemType.ToString().ToLower()}{(expectedFileSystemType == FileSystemType.Ntfs ? " --fast" : string.Empty)} /dev/sdc1"
            };

            Assert.AreEqual(commandsExpected.Count, commandsExecuted.Count);
            CollectionAssert.AreEqual(commandsExpected, commandsExecuted);
        }

        [Test]
        public async Task UnixDiskManagerCallsTheExpectedCommandsToFormatADiskWithExistingFileSystemVolumes()
        {
            List<string> commandsExecuted = new List<string>();

            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            // Capture the commands ran for each process
            this.testProcess.OnStart = () =>
            {
                this.testProcess.StandardOutput.Clear();
                commandsExecuted.Add($"{this.testProcess.StartInfo.FileName} {this.testProcess.StartInfo.Arguments}");

                return true;
            };

            // When we are deleting partitions, we are interactive with a parted
            // session.
            this.standardInput.BytesWritten += (sender, input) =>
            {
                commandsExecuted.Add(input.Trim());
                if (input.Contains("rm 1"))
                {
                    this.testProcess.StandardOutput.Append("(parted)");
                }
            };

            Disk disk = new Disk(0, "/dev/sdc", volumes: new List<DiskVolume>
            {
                // A previous file system volume exists that has mount points.
                new DiskVolume(1,
                    "/dev/sdc1",
                    accessPaths: new List<string>
                    {
                        "/home/virtualclient.1.0.1585.123/ExternalTools/linux-x64/Mount1",
                        "/home/virtualclient.1.0.1585.123/ExternalTools/linux-x64/Mount2",
                    })
            });

            await this.diskManager.FormatDiskAsync(disk, PartitionType.Gpt, FileSystemType.Ext4, CancellationToken.None)
                .ConfigureAwait(false);

            List<string> commandsExpected = new List<string>
            {
                // All mount points must be removed first
                $"sudo umount -l /home/virtualclient.1.0.1585.123/ExternalTools/linux-x64/Mount1",
                $"sudo umount -l /home/virtualclient.1.0.1585.123/ExternalTools/linux-x64/Mount2",

                // The previous partition is deleted
                $"sudo parted /dev/sdc1",
                $"rm 1", // remove the partition
                $"quit", // quit the parted interactive session

                // The new partition can now be created
                $"sudo partprobe",
                $"sudo parted -s /dev/sdc mklabel gpt",
                $"sudo parted -s -a optimal /dev/sdc mkpart primary 0% 100%",
                $"sudo partprobe",

                // And the new file system can be created
                $"sudo mkfs -t ext4 /dev/sdc1"
            };

            Assert.AreEqual(commandsExpected.Count, commandsExecuted.Count);
            CollectionAssert.AreEqual(commandsExpected, commandsExecuted);
        }

        [Test]
        public void UnixDiskManagerAppliesRetriesOnFailedAttemptsToFormatADisk()
        {
            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            int attempts = 0;
            this.testProcess.OnStart = () =>
            {
                attempts++;
                throw new ProcessException();
            };

            Disk disk = FixtureExtensions.CreateDisk(0);

            this.diskManager.RetryPolicy = Policy.Handle<ProcessException>()
                .WaitAndRetryAsync(3, (retries) => TimeSpan.Zero);

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.FormatDiskAsync(disk, PartitionType.Gpt, FileSystemType.Ext4, CancellationToken.None));
            Assert.IsTrue(attempts == 4);
        }

        [Test]
        public async Task UnixDiskManagerCallsTheExpectedCommandsToCreateAMountPoint()
        {
            List<string> commandsExecuted = new List<string>();

            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            // Capture the commands ran for each process
            this.testProcess.OnStart = () =>
            {
                this.testProcess.StandardOutput.Clear();
                commandsExecuted.Add($"{this.testProcess.StartInfo.FileName} {this.testProcess.StartInfo.Arguments}");

                return true;
            };

            string expectedMountPoint = "/home/virtualclient.1.0.1585.123/ExternalTools/linux-x64/Mount1";
            DiskVolume volume = new DiskVolume(0, "/dev/sdc1");

            await this.diskManager.CreateMountPointAsync(volume, expectedMountPoint, CancellationToken.None)
                .ConfigureAwait(false);

            List<string> commandsExpected = new List<string>
            {
                $"sudo mount /dev/sdc1 {expectedMountPoint}"
            };

            Assert.AreEqual(commandsExpected.Count, commandsExecuted.Count);
            CollectionAssert.AreEqual(commandsExpected, commandsExecuted);
        }

        [Test]
        public void UnixDiskManagerAppliesRetriesOnFailedAttemptsToCreateMountPoints()
        {
            // Ensure the process exits promptly
            this.testProcess.OnHasExited = () => true;

            int attempts = 0;
            this.testProcess.OnStart = () =>
            {
                attempts++;
                throw new ProcessException();
            };

            DiskVolume volume = new DiskVolume(0, accessPaths: new List<string> { "/dev/sdc1" });

            this.diskManager.RetryPolicy = Policy.Handle<ProcessException>()
                .WaitAndRetryAsync(3, (retries) => TimeSpan.Zero);

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.CreateMountPointAsync(volume, "/any/mount/point", CancellationToken.None));
            Assert.IsTrue(attempts == 4);
        }

        [Test]
        public async Task UnixDiskManagerReturnsListofDiskPaths()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;
            this.testProcess.StandardOutput.Append(Resources.lshw_disk_storage_results);

            List<string> accessPaths = new List<string>
            {
                "/mnt",
            };

            IEnumerable<string> diskPaths = await this.diskManager.GetDiskPathsAsync("osdisk:false", CancellationToken.None)
                .ConfigureAwait(false);

            CollectionAssert.AreEqual(diskPaths, accessPaths);
        }

        private class TestUnixDiskManager : UnixDiskManager
        {
            public TestUnixDiskManager(ProcessManager processManager)
                : base(processManager)
            {
                this.WaitTime = TimeSpan.Zero;
            }
        }
    }
}
