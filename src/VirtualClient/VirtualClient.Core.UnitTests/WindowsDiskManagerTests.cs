// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using Polly;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class WindowsDiskManagerTests
    {
        private TestWindowsDiskManager diskManager;
        private InMemoryProcessManager processManager;
        private InMemoryProcess testProcess;
        private InMemoryStream standardInput;

        [SetUp]
        public void SetupTest()
        {
            this.processManager = new InMemoryProcessManager(PlatformID.Win32NT)
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
            this.diskManager = new TestWindowsDiskManager(this.processManager)
            {
                RetryPolicy = Policy.NoOpAsync(),
                WaitTime = TimeSpan.Zero
            };

            this.testProcess = new InMemoryProcess(this.standardInput);
        }

        [Test]
        public void WindowsDiskManagerNormalizesDiskPartOutputIntoFixedWidthLines_Scenario1()
        {
            // Scenario:
            // The lines are already fixed width.
            StringBuilder lines = new StringBuilder()
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition    1023 GB  Healthy            ")
                .AppendLine("    C:\\Users\\vcvmadmin\\Desktop\\MountPath1\\                               ");

            IEnumerable<string> fixedWidthLines = TestWindowsDiskManager.GetFixedWidthLines(lines.ToString().TrimEnd()
                .Split(Environment.NewLine));

            Assert.IsNotNull(fixedWidthLines);
            Assert.IsTrue(fixedWidthLines.Count() == 4);
            Assert.AreEqual("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info    ", fixedWidthLines.ElementAt(0));
            Assert.AreEqual("  ----------  ---  -----------  -----  ----------  --------  ---------  --------", fixedWidthLines.ElementAt(1));
            Assert.AreEqual("  Volume 3     E                NTFS   Partition    1023 GB  Healthy            ", fixedWidthLines.ElementAt(2));
            Assert.AreEqual(@"    C:\Users\vcvmadmin\Desktop\MountPath1\                                      ", fixedWidthLines.ElementAt(3));
        }

        [Test]
        public void WindowsDiskManagerNormalizesDiskPartOutputIntoFixedWidthLines_Scenario2()
        {
            // Scenario:
            // The lines have multiple different and inconsistent lengths
            StringBuilder lines = new StringBuilder()
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition    1023 GB  Healthy            ")
                .AppendLine("    C:\\Users\\vcvmadmin\\Desktop\\MountPath1\\");

            IEnumerable<string> fixedWidthLines = TestWindowsDiskManager.GetFixedWidthLines(lines.ToString().TrimEnd()
                .Split(Environment.NewLine));

            Assert.IsNotNull(fixedWidthLines);
            Assert.IsTrue(fixedWidthLines.Count() == 4);
            Assert.AreEqual("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info    ", fixedWidthLines.ElementAt(0));
            Assert.AreEqual("  ----------  ---  -----------  -----  ----------  --------  ---------  --------", fixedWidthLines.ElementAt(1));
            Assert.AreEqual("  Volume 3     E                NTFS   Partition    1023 GB  Healthy            ", fixedWidthLines.ElementAt(2));
            Assert.AreEqual(@"    C:\Users\vcvmadmin\Desktop\MountPath1\                                      ", fixedWidthLines.ElementAt(3));
        }

        [Test]
        public async Task WindowsDiskManagerCallsTheExpectedDiskPartCommandsToCreateAMountPoint_VolumeIndexUsed()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            DiskVolume diskPartition = new DiskVolume(1);

            string expectedMountPath = @"E:\any\path\to\mount\point";
            List<string> expectedCommands = new List<string>
            {
                $"select volume 1",
                $"assign mount={expectedMountPath}"
            };

            List<string> actualCommands = new List<string>();

            // We subscribe to the test stream writes to get the data that is written
            // to standard input. Then we respond by placing expected responses in the standard
            // output. This mimics normal console input/output mechanics.
            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                actualCommands.Add(input);

                if (input.Contains("select volume 1"))
                {
                    this.testProcess.StandardOutput.Append("Volume 1 is the selected volume.");
                }
                else if (input.Contains($"assign mount={expectedMountPath}"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully assigned the drive letter or mount point.");
                }
                else
                {
                    Assert.Fail($"Unexpected command called: {input}");
                }
            };

            await this.diskManager.CreateMountPointAsync(diskPartition, expectedMountPath, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotEmpty(actualCommands);
            Assert.AreEqual(expectedCommands.Count, actualCommands.Count);
            CollectionAssert.AreEquivalent(expectedCommands, actualCommands);
        }

        [Test]
        public async Task WindowsDiskManagerCallsTheExpectedDiskPartCommandsToCreateAMountPoint_VolumeLetterUsed()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            DiskVolume diskPartition = new DiskVolume(null, properties: new Dictionary<string, IConvertible>
            {
                { "Ltr", "E" }
            });

            string expectedMountPath = @"E:\any\path\to\mount\point";
            List<string> expectedCommands = new List<string>
            {
                $"select volume E",
                $"assign mount={expectedMountPath}"
            };

            List<string> actualCommands = new List<string>();

            // We subscribe to the test stream writes to get the data that is written
            // to standard input. Then we respond by placing expected responses in the standard
            // output. This mimics normal console input/output mechanics.
            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                actualCommands.Add(input);

                if (input.Contains("select volume E"))
                {
                    this.testProcess.StandardOutput.Append("Volume 1 is the selected volume.");
                }
                else if (input.Contains($"assign mount={expectedMountPath}"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully assigned the drive letter or mount point.");
                }
                else
                {
                    Assert.Fail($"Unexpected command called: {input}");
                }
            };

            await this.diskManager.CreateMountPointAsync(diskPartition, expectedMountPath, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotEmpty(actualCommands);
            Assert.AreEqual(expectedCommands.Count, actualCommands.Count);
            CollectionAssert.AreEquivalent(expectedCommands, actualCommands);
        }

        [Test]
        public void WindowsDiskManagerThrowsWhenCreatingAMountPointIfNeitherAVolumeIndexOrLetterIsDefined()
        {
            DiskVolume diskPartition = new DiskVolume(null, properties: new Dictionary<string, IConvertible> //, index: not defined
            {
                // Missing volume letter { "Ltr", "E" }
            });

            Assert.ThrowsAsync<ProcessException>(() => this.diskManager.CreateMountPointAsync(diskPartition, @"C:\any\mount\point", CancellationToken.None));
        }

        [Test]
        public async Task WindowsDiskManagerCallsTheExpectedDiskPartCommandsToPartitionAndFormatADisk()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            Disk diskToFormat = new Disk(
                index: 3);

            List<string> expectedCommands = new List<string>
            {
                "select disk 3",
                "clean",
                "create partition primary",
                "list partition",
                "select partition 1",
                "assign letter=",
                "select volume",
                "format fs=",
                "exit"
            };

            List<string> actualCommands = new List<string>();

            // We subscribe to the test stream writes to get the data that is written
            // to standard input. Then we respond by placing expected responses in the standard
            // output. This mimics normal console input/output mechanics.
            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                actualCommands.Add(input);

                if (input.Contains($"select disk {diskToFormat.Index}"))
                {
                    this.testProcess.StandardOutput.Append($"Disk {diskToFormat.Index} is now the selected disk.");
                }
                else if (input.Contains("clean"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in cleaning the disk.");
                }
                else if (input.Contains("create partition primary"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in creating the specified partition.");
                }
                else if (input.Contains("list partition"))
                {
                    this.testProcess.StandardOutput.Append("  Partition ###  Type              Size     Offset");
                    this.testProcess.StandardOutput.Append("  -------------  ----------------  -------  -------");
                    this.testProcess.StandardOutput.Append("  Partition 1    Primary            128 MB    17 KB");
                }
                else if (input.Contains("select partition"))
                {
                    this.testProcess.StandardOutput.Append("Partition 1 is now the selected partition.");
                }
                else if (input.Contains("assign letter="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully assigned the drive letter or mount point.");
                }
                else if (input.Contains("select volume"))
                {
                    this.testProcess.StandardOutput.Append("Volume 1 is the selected volume.");
                }
                else if (input.Contains("format fs="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully formatted the volume.");
                }
                else if (input.Contains("exit"))
                {
                    // Expected
                }
                else
                {
                    Assert.Fail($"Unexpected command called: {input}");
                }
            };

            await this.diskManager.FormatDiskAsync(diskToFormat, PartitionType.Gpt, FileSystemType.Ntfs, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotEmpty(actualCommands);
            Assert.AreEqual(expectedCommands.Count, actualCommands.Count);

            for (int i = 0; i < expectedCommands.Count; i++)
            {
                Assert.IsTrue(actualCommands[i].Contains(expectedCommands[i]));
            }
        }

        [Test]
        [TestCase(FileSystemType.MsDos)]
        [TestCase(FileSystemType.Ntfs)]
        public async Task WindowsDiskManagerUsesTheExpectedFileSystemWhenFormattingAPartition(FileSystemType fileSystemType)
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            Disk diskToFormat = new Disk(
                index: 3);

            List<string> actualCommands = new List<string>();

            // We subscribe to the test stream writes to get the data that is written
            // to standard input. Then we respond by placing expected responses in the standard
            // output. This mimics normal console input/output mechanics.
            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                actualCommands.Add(input);

                if (input.Contains($"select disk {diskToFormat.Index}"))
                {
                    this.testProcess.StandardOutput.Append($"Disk {diskToFormat.Index} is now the selected disk.");
                }
                else if (input.Contains("clean"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in cleaning the disk.");
                }
                else if (input.Contains("create partition primary"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in creating the specified partition.");
                }
                else if (input.Contains("list partition"))
                {
                    this.testProcess.StandardOutput.Append("  Partition ###  Type              Size     Offset");
                    this.testProcess.StandardOutput.Append("  -------------  ----------------  -------  -------");
                    this.testProcess.StandardOutput.Append("* Partition 1    Primary            128 MB    17 KB");
                }
                else if (input.Contains("select partition"))
                {
                    this.testProcess.StandardOutput.Append("Partition 1 is now the selected partition.");
                }
                else if (input.Contains("assign letter="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully assigned the drive letter or mount point.");
                }
                else if (input.Contains("select volume"))
                {
                    this.testProcess.StandardOutput.Append("Volume 1 is the selected volume.");
                }
                else if (input.Contains("format fs="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully formatted the volume.");
                }
                else if (input.Contains("exit"))
                {
                    // Expected
                }
                else
                {
                    Assert.Fail($"Unexpected command called: {input}");
                }
            };

            await this.diskManager.FormatDiskAsync(diskToFormat, PartitionType.Gpt, fileSystemType, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotEmpty(actualCommands);
            Assert.IsTrue(actualCommands.Contains($"format fs={fileSystemType.ToString().ToLowerInvariant()} quick"));
        }

        [Test]
        public async Task WindowsDiskManagerHandlesReservedPartitionsOnDisksDuringTheFormatDiskOperation()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            Disk diskToFormat = new Disk(
                index: 3);

            List<string> expectedCommands = new List<string>
            {
                "select disk 3",
                "clean",
                "create partition primary",
                "list partition",
                "select partition 2",
                "assign letter=",
                "select volume",
                "format fs=",
                "exit"
            };

            List<string> actualCommands = new List<string>();

            // We subscribe to the test stream writes to get the data that is written
            // to standard input. Then we respond by placing expected responses in the standard
            // output. This mimics normal console input/output mechanics.
            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                actualCommands.Add(input);

                if (input.Contains($"select disk {diskToFormat.Index}"))
                {
                    this.testProcess.StandardOutput.Append($"Disk {diskToFormat.Index} is now the selected disk.");
                }
                else if (input.Contains("clean"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in cleaning the disk.");
                }
                else if (input.Contains("create partition primary"))
                {
                    this.testProcess.StandardOutput.Append("DiskPart succeeded in creating the specified partition.");
                }
                else if (input.Contains("list partition"))
                {
                    // Certain types of Azure managed disks (e.g. 32,767 GB Premium_LRS) have a Reserved partition
                    // that cannot be itself formatted during the operation. The logic must pick out the Primary partition
                    // from the two in order to avoid issues.
                    this.testProcess.StandardOutput.Append("  Partition ###  Type              Size     Offset");
                    this.testProcess.StandardOutput.Append("  -------------  ----------------  -------  -------");
                    this.testProcess.StandardOutput.Append("* Partition 1    Reserved           128 MB    17 KB");
                    this.testProcess.StandardOutput.Append("  Partition 2    Primary            128 MB    17 KB");
                }
                else if (input.Contains("select partition"))
                {
                    this.testProcess.StandardOutput.Append("Partition 2 is now the selected partition.");
                }
                else if (input.Contains("assign letter="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully assigned the drive letter or mount point.");
                }
                else if (input.Contains("select volume"))
                {
                    this.testProcess.StandardOutput.Append("Volume 1 is the selected volume.");
                }
                else if (input.Contains("format fs="))
                {
                    this.testProcess.StandardOutput.Append("DiskPart successfully formatted the volume.");
                }
                else if (input.Contains("exit"))
                {
                    // Expected
                }
                else
                {
                    Assert.Fail($"Unexpected command called: {input}");
                }
            };

            await this.diskManager.FormatDiskAsync(diskToFormat, PartitionType.Gpt, FileSystemType.Ntfs, CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotEmpty(actualCommands);
            Assert.AreEqual(expectedCommands.Count, actualCommands.Count);

            for (int i = 0; i < expectedCommands.Count; i++)
            {
                Assert.IsTrue(actualCommands[i].Contains(expectedCommands[i]));
            }
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartListDiskResultsCorrectly_AzureVMScenario()
        {
            ConcurrentBuffer listDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine(" Disk ###  Status         Size     Free     Dyn  Gpt")
                .AppendLine(" --------  -------------  -------  -------  ---  ---")
                .AppendLine(" Disk 0    Online          127 GB  1024 KB          ")
                .AppendLine(" Disk 1    Online           32 GB      0 B          ")
                .AppendLine(" Disk 2    Online         1024 GB      0 B          ")
                .AppendLine(" Disk 3    Online         1024 GB      0 B          ");

            IEnumerable<int> diskIndexes = TestWindowsDiskManager.ParseDiskIndexes(listDiskResults);

            Assert.IsNotNull(diskIndexes);
            Assert.IsNotEmpty(diskIndexes);
            Assert.IsTrue(diskIndexes.Count() == 4);
            CollectionAssert.AreEquivalent(new List<int> { 0, 1, 2, 3 }, diskIndexes);
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartListPartitionResultsCorrectly_AzureVMScenario()
        {
            ConcurrentBuffer listPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine(" Partition ###  Type              Size     Offset")
                .AppendLine(" -------------  ----------------  -------  -------")
                .AppendLine(" Partition 1    Primary            500 MB  1024 KB")
                .AppendLine(" Partition 2    Primary            126 GB   501 MB");

            IEnumerable<int> partitionIndexes = TestWindowsDiskManager.ParseDiskPartitionIndexes(listPartitionResults);

            Assert.IsNotNull(partitionIndexes);
            Assert.IsNotEmpty(partitionIndexes);
            Assert.IsTrue(partitionIndexes.Count() == 2);
            CollectionAssert.AreEquivalent(new List<int> { 1, 2 }, partitionIndexes);
        }

        [Test]
        public void WindowsDiskManagerHandlesScenariosWhereThereAreNoPartitionsExistingForAGivenDisk()
        {
            ConcurrentBuffer listPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("There are no partitions on this disk to show.");

            IEnumerable<int> partitionIndexes = TestWindowsDiskManager.ParseDiskPartitionIndexes(listPartitionResults);

            Assert.IsNotNull(partitionIndexes);
            Assert.IsEmpty(partitionIndexes);
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailDiskResultsCorrectly_OSDisk_AzureVMScenario()
        {
            // OS Disk (disk with partition containing OS)
            ConcurrentBuffer detailDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Virtual HD ATA Device")
                .AppendLine("Disk ID: EF349D83")
                .AppendLine("Type   : ATA")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 0")
                .AppendLine("LUN ID : 0")
                .AppendLine("Location Path : ACPI(_SB_)#ACPI(PCI0)#ACPI(IDE0)#ACPI(CHN0)#ATA(C00T00L00)")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : Yes")
                .AppendLine("Pagefile Disk  : Yes")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : No")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 0         System Rese  NTFS   Partition    500 MB  Healthy    System  ")
                .AppendLine("  Volume 1     C   Windows      NTFS   Partition    126 GB  Healthy    Boot    ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Model", "Virtual HD ATA Device" },
                { "Disk ID", "EF349D83" },
                { "Type", "ATA" },
                { "Status", "Online" },
                { "Path", "0" },
                { "Target", "0" },
                { "LUN ID", "0" },
                { "Location Path", "ACPI(_SB_)#ACPI(PCI0)#ACPI(IDE0)#ACPI(CHN0)#ATA(C00T00L00)" },
                { "Current Read-only State", "No" },
                { "Read-only", "No" },
                { "Boot Disk", "Yes" },
                { "Pagefile Disk", "Yes" },
                { "Hibernation File Disk", "No" },
                { "Crashdump Disk", "No" },
                { "Clustered Disk", "No" }
            };

            IDictionary<string, IConvertible> actualDiskProperties = TestWindowsDiskManager.ParseDiskProperties(detailDiskResults);

            Assert.IsNotNull(actualDiskProperties);
            Assert.IsNotEmpty(actualDiskProperties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskProperties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskProperties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailDiskResultsCorrectly_OSDisk_WindowsClientScenario()
        {
            // OS Disk (disk with partition containing OS)
            ConcurrentBuffer detailDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Samsung SSD 970 EVO Plus 1TB")
                .AppendLine("Disk ID: {6BCFC84A-7979-42D8-B047-B5E2E6A8644A}")
                .AppendLine("Type   : NVMe")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 0")
                .AppendLine("LUN ID : 0")
                .AppendLine("Location Path : PCIROOT(0)#PCI(1D04)#PCI(0000)#NVME(P00T00L00)")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : Yes")
                .AppendLine("Pagefile Disk  : Yes")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : Yes")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info  ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  ------")
                .AppendLine("  Volume 1     C   OS           NTFS   Partition    588 GB  Healthy    Boot  ")
                .AppendLine("  Volume 2     S   Source       NTFS   Partition    341 GB  Healthy          ")
                .AppendLine("  Volume 3         Windows RE   NTFS   Partition    499 MB  Healthy    Hidden")
                .AppendLine("  Volume 4         SYSTEM       FAT32  Partition    499 MB  Healthy    System");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Model", "Samsung SSD 970 EVO Plus 1TB" },
                { "Disk ID", "{6BCFC84A-7979-42D8-B047-B5E2E6A8644A}" },
                { "Type", "NVMe" },
                { "Status", "Online" },
                { "Path", "0" },
                { "Target", "0" },
                { "LUN ID", "0" },
                { "Location Path", "PCIROOT(0)#PCI(1D04)#PCI(0000)#NVME(P00T00L00)" },
                { "Current Read-only State", "No" },
                { "Read-only", "No" },
                { "Boot Disk", "Yes" },
                { "Pagefile Disk", "Yes" },
                { "Hibernation File Disk", "No" },
                { "Crashdump Disk", "Yes" },
                { "Clustered Disk", "No" }
            };

            IDictionary<string, IConvertible> actualDiskProperties = TestWindowsDiskManager.ParseDiskProperties(detailDiskResults);

            Assert.IsNotNull(actualDiskProperties);
            Assert.IsNotEmpty(actualDiskProperties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskProperties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskProperties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailDiskResultsCorrectly_LocalTempDisk_AzureVMScenario()
        {
            // Local Temp Disk
            ConcurrentBuffer detailDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Virtual HD ATA Device")
                .AppendLine("Disk ID: 5BBD5D7F")
                .AppendLine("Type   : ATA")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 1")
                .AppendLine("LUN ID : 0")
                .AppendLine("Location Path : ACPI(_SB_)#ACPI(PCI0)#ACPI(IDE0)#ACPI(CHN0)#ATA(C00T01L00)")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : Yes")
                .AppendLine("Pagefile Disk  : Yes")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : No")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 2     D   Temporary S  NTFS   Partition     31 GB  Healthy    Pagefile");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Model", "Virtual HD ATA Device" },
                { "Disk ID", "5BBD5D7F" },
                { "Type", "ATA" },
                { "Status", "Online" },
                { "Path", "0" },
                { "Target", "1" },
                { "LUN ID", "0" },
                { "Location Path", "ACPI(_SB_)#ACPI(PCI0)#ACPI(IDE0)#ACPI(CHN0)#ATA(C00T01L00)" },
                { "Current Read-only State", "No" },
                { "Read-only", "No" },
                { "Boot Disk", "Yes" },
                { "Pagefile Disk", "Yes" },
                { "Hibernation File Disk", "No" },
                { "Crashdump Disk", "No" },
                { "Clustered Disk", "No" }
            };

            IDictionary<string, IConvertible> actualDiskProperties = TestWindowsDiskManager.ParseDiskProperties(detailDiskResults);

            Assert.IsNotNull(actualDiskProperties);
            Assert.IsNotEmpty(actualDiskProperties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskProperties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskProperties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailDiskResultsCorrectly_RemoteDisk_BeforePartitioning_AzureVMScenario()
        {
            // Remote Disk
            ConcurrentBuffer detailDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Microsoft Virtual Disk")
                .AppendLine("Disk ID: 00000000")
                .AppendLine("Type   : SAS")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 0")
                .AppendLine("LUN ID : 1")
                .AppendLine("Location Path : UNAVAILABLE")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : No")
                .AppendLine("Pagefile Disk  : No")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : No")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("There are no volumes.");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Model", "Microsoft Virtual Disk" },
                { "Disk ID", "00000000" },
                { "Type", "SAS" },
                { "Status", "Online" },
                { "Path", "0" },
                { "Target", "0" },
                { "LUN ID", "1" },
                { "Location Path", "UNAVAILABLE" },
                { "Current Read-only State", "No" },
                { "Read-only", "No" },
                { "Boot Disk", "No" },
                { "Pagefile Disk", "No" },
                { "Hibernation File Disk", "No" },
                { "Crashdump Disk", "No" },
                { "Clustered Disk", "No" }
            };

            IDictionary<string, IConvertible> actualDiskProperties = TestWindowsDiskManager.ParseDiskProperties(detailDiskResults);

            Assert.IsNotNull(actualDiskProperties);
            Assert.IsNotEmpty(actualDiskProperties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskProperties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskProperties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailDiskResultsCorrectly_RemoteDisk_AfterPartitioning_AzureVMScenario()
        {
            // Remote Disk
            ConcurrentBuffer detailDiskResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Microsoft Virtual Disk")
                .AppendLine("Disk ID: 97AFC1BB")
                .AppendLine("Type   : SAS")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 0")
                .AppendLine("LUN ID : 1")
                .AppendLine("Location Path : UNAVAILABLE")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : No")
                .AppendLine("Pagefile Disk  : No")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : No")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition   1023 GB  Healthy");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Model", "Microsoft Virtual Disk" },
                { "Disk ID", "97AFC1BB" },
                { "Type", "SAS" },
                { "Status", "Online" },
                { "Path", "0" },
                { "Target", "0" },
                { "LUN ID", "1" },
                { "Location Path", "UNAVAILABLE" },
                { "Current Read-only State", "No" },
                { "Read-only", "No" },
                { "Boot Disk", "No" },
                { "Pagefile Disk", "No" },
                { "Hibernation File Disk", "No" },
                { "Crashdump Disk", "No" },
                { "Clustered Disk", "No" }
            };

            IDictionary<string, IConvertible> actualDiskProperties = TestWindowsDiskManager.ParseDiskProperties(detailDiskResults);

            Assert.IsNotNull(actualDiskProperties);
            Assert.IsNotEmpty(actualDiskProperties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskProperties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskProperties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_OSDisk_AzureVMScenario_SystemReservedPartition()
        {
            // System Reserved Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 0         System Rese  NTFS   Partition    500 MB  Healthy    System  ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "0" },
                { "Ltr", null },
                { "Label", "System Rese" },
                { "Fs", "NTFS" },
                { "Size", "500 MB" },
                { "Status", "Healthy" },
                { "Info", "System" }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsEmpty(actualVolume.AccessPaths);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_OSDisk_WindowsClientScenario_SystemReservedPartition()
        {
            // System Reserved Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 2")
                .AppendLine("Type  : c12a7328-f81f-11d2-ba4b-00a0c93ec93b")
                .AppendLine("Hidden: Yes")
                .AppendLine("Required: No")
                .AppendLine("Attrib  : 0X8000000000000000")
                .AppendLine("Offset in Bytes: 524288000")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 4         SYSTEM       FAT32  Partition    499 MB  Healthy    System  ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "2" },
                { "Type", "Partition" },
                { "Hidden", "Yes" },
                { "Required", "No" },
                { "Attrib", "0X8000000000000000" },
                { "Offset in Bytes", "524288000" },
                { "Index", "4" },
                { "Ltr", null },
                { "Label", "SYSTEM" },
                { "Fs", "FAT32" },
                { "Size", "499 MB" },
                { "Status", "Healthy" },
                { "Info", "System" }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsEmpty(actualVolume.AccessPaths);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_OSDisk_AzureVMScenario_OSPartition()
        {
            // OS Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 525336576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 1     C   Windows      NTFS   Partition    126 GB  Healthy    Boot    ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "525336576" },
                { "Index", "1" },
                { "Ltr", "C" },
                { "Label", "Windows" },
                { "Fs", "NTFS" },
                { "Size", "126 GB" },
                { "Status", "Healthy" },
                { "Info", "Boot" }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsNotEmpty(actualVolume.AccessPaths);
            Assert.IsTrue(actualVolume.AccessPaths.Count() == 1);

            string defaultAccessPath = actualVolume.DevicePath;
            Assert.AreEqual("C:\\", defaultAccessPath);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_OSDisk_WindowsClientScenario_OSPartition()
        {
            // System Reserved Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 4")
                .AppendLine("Type  : ebd0a0a2-b9e5-4433-87c0-68b6b72699c7")
                .AppendLine("Hidden: No")
                .AppendLine("Required: No")
                .AppendLine("Attrib  : 0000000000000000")
                .AppendLine("Offset in Bytes: 1181745152")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 1     C   OS           NTFS   Partition    588 GB  Healthy    Boot    ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "4" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Required", "No" },
                { "Attrib", "0000000000000000" },
                { "Offset in Bytes", "1181745152" },
                { "Index", "1" },
                { "Ltr", "C" },
                { "Label", "OS" },
                { "Fs", "NTFS" },
                { "Size", "588 GB" },
                { "Status", "Healthy" },
                { "Info", "Boot" }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsTrue(actualVolume.AccessPaths.Count() == 1);

            string defaultAccessPath = actualVolume.DevicePath;
            Assert.AreEqual("C:\\", defaultAccessPath);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_OSDisk_WindowsClientScenario_NonOSPartition()
        {
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 5")
                .AppendLine("Type  : ebd0a0a2-b9e5-4433-87c0-68b6b72699c7")
                .AppendLine("Hidden: No")
                .AppendLine("Required: No")
                .AppendLine("Attrib  : 0000000000000000")
                .AppendLine("Offset in Bytes: 633202540544")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 2     S   Data         NTFS   Partition    341 GB  Healthy            ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "5" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Required", "No" },
                { "Attrib", "0000000000000000" },
                { "Offset in Bytes", "633202540544" },
                { "Index", "2" },
                { "Ltr", "S" },
                { "Label", "Data" },
                { "Fs", "NTFS" },
                { "Size", "341 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsTrue(actualVolume.AccessPaths.Count() == 1);

            string defaultAccessPath = actualVolume.DevicePath;
            Assert.AreEqual("S:\\", defaultAccessPath);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_LocalTempDisk_AzureVMScenario()
        {
            // OS Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 2     D   Temporary S  NTFS   Partition     31 GB  Healthy    Pagefile");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "2" },
                { "Ltr", "D" },
                { "Label", "Temporary S" },
                { "Fs", "NTFS" },
                { "Size", "31 GB" },
                { "Status", "Healthy" },
                { "Info", "Pagefile" }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsTrue(actualVolume.AccessPaths.Count() == 1);

            string defaultAccessPath = actualVolume.DevicePath;
            Assert.AreEqual("D:\\", defaultAccessPath);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesDiskPartDetailPartitionResultsCorrectly_RemoteDisk_AfterPartitioning_AzureVMScenario()
        {
            // Remote Disk Partition
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition    1023 GB  Healthy            ");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "3" },
                { "Ltr", "E" },
                { "Label", null },
                { "Fs", "NTFS" },
                { "Size", "1023 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            DiskVolume actualVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualVolume);
            Assert.IsNotEmpty(actualVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualVolume.Properties.Count());
            Assert.IsTrue(actualVolume.AccessPaths.Count() == 1);

            string defaultAccessPath = actualVolume.DevicePath;
            Assert.AreEqual("E:\\", defaultAccessPath);

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerParsesMountPointsFromDiskPartDetailPartitionResults_Scenario1()
        {
            // Scenario:
            // Single mount point
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: No")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("* Volume 3     E                NTFS   Partition    1023 GB  Healthy            ")
                .AppendLine(@"    C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\                           ");

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.AccessPaths);
            Assert.IsTrue(actualDiskVolume.AccessPaths.Count() == 2);

            string defaultAccessPath = actualDiskVolume.AccessPaths.ElementAt(0);
            Assert.IsTrue(defaultAccessPath == @"E:\");

            string accessPath2 = actualDiskVolume.AccessPaths.ElementAt(1);
            Assert.IsTrue(accessPath2 == @"C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\");
        }

        [Test]
        public void WindowsDiskManagerParsesMountPointsFromDiskPartDetailPartitionResults_Scenario2()
        {
            // Scenario:
            // Single mount point
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: No")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("* Volume 3     E                NTFS   Partition    1023 GB  Healthy            ")
                .AppendLine(@"    C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\                           ")
                .AppendLine(@"    C:\Users\vcvmadmin\Desktop\win-x64\TestMount2\                           ");

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.AccessPaths);
            Assert.IsTrue(actualDiskVolume.AccessPaths.Count() == 3);

            Assert.AreEqual("E:\\", actualDiskVolume.DevicePath);
            Assert.AreEqual(@"C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\", actualDiskVolume.AccessPaths.ElementAt(1));
            Assert.AreEqual(@"C:\Users\vcvmadmin\Desktop\win-x64\TestMount2\", actualDiskVolume.AccessPaths.ElementAt(2));
        }

        [Test]
        public void WindowsDiskManagerParsesMountPointsFromDiskPartDetailPartitionResults_BugFixScenario1()
        {
            // Scenario:
            // Single mount point
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 2")
                .AppendLine("Type    : ebd0a0a2-b9e5-4433-87c0-68b6b72699c7")
                .AppendLine("Hidden  : No")
                .AppendLine("Required: No")
                .AppendLine("Attrib  : 0X8000000000000000")
                .AppendLine("Offset in Bytes: 135266304")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("* Volume 1                      RAW    Partition     715 GB  Healthy            ")
                .AppendLine(@"    C:\App\HostWorkloads.TipNode_0566b03523db3005df3cbdb37c3c7c83\VirtualClient\system_disk_1_0\");

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.AccessPaths);
            Assert.IsTrue(actualDiskVolume.AccessPaths.Count() == 1);

            Assert.AreEqual(@"C:\App\HostWorkloads.TipNode_0566b03523db3005df3cbdb37c3c7c83\VirtualClient\system_disk_1_0\", actualDiskVolume.DevicePath);
        }

        [Test]
        public void WindowsDiskManagerHandlesDiskPartDetailPartitionResultsThatDoNotHaveTypicalFixedWidthRows_Scenario1()
        {
            // The results below have multiple rows that are not the same width.
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition    1023 GB  Healthy");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "3" },
                { "Ltr", "E" },
                { "Label", null },
                { "Fs", "NTFS" },
                { "Size", "1023 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskVolume.Properties.Count());

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerHandlesDiskPartDetailPartitionResultsThatDoNotHaveTypicalFixedWidthRows_Scenario2()
        {
            // The results below have multiple rows that are not the same width.
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("  Volume 3     E                NTFS   Partition    1023 GB  Healthy")
                .AppendLine(@"    C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "3" },
                { "Ltr", "E" },
                { "Label", null },
                { "Fs", "NTFS" },
                { "Size", "1023 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskVolume.Properties.Count());
            Assert.IsTrue(actualDiskVolume.AccessPaths.Count() == 2);
            Assert.IsTrue(actualDiskVolume.AccessPaths.ElementAt(0) == @"E:\");
            Assert.IsTrue(actualDiskVolume.AccessPaths.ElementAt(1) == @"C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\");

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerHandlesDiskPartDetailPartitionResultsThatDoNotHaveTypicalFixedWidthRows_Scenario3()
        {
            // The results below have multiple rows that are not the same width.
            ConcurrentBuffer detailPartitionResults = new ConcurrentBuffer()
                .AppendLine("           ")
                .AppendLine("Partition 1")
                .AppendLine("Type  : 07")
                .AppendLine("Hidden: No")
                .AppendLine("Active: Yes")
                .AppendLine("Offset in Bytes: 1048576")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size      Status     Info")
                .AppendLine("  ----------  ---  -----------  -----  ----------  --------  ---------  --------")
                .AppendLine("* Volume 3     E                NTFS   Partition    1023 GB  Healthy            ")
                .AppendLine(@"    C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\")
                .AppendLine()
                .AppendLine();

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "PartitionIndex", "1" },
                { "Type", "Partition" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Index", "3" },
                { "Ltr", "E" },
                { "Label", null },
                { "Fs", "NTFS" },
                { "Size", "1023 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            DiskVolume actualDiskVolume = TestWindowsDiskManager.ParseDiskVolume(detailPartitionResults);

            Assert.IsNotNull(actualDiskVolume);
            Assert.IsNotEmpty(actualDiskVolume.Properties);
            Assert.AreEqual(expectedDiskProperties.Count(), actualDiskVolume.Properties.Count());
            Assert.IsTrue(actualDiskVolume.AccessPaths.Count() == 2);
            Assert.IsTrue(actualDiskVolume.AccessPaths.ElementAt(0) == @"E:\");
            Assert.IsTrue(actualDiskVolume.AccessPaths.ElementAt(1) == @"C:\Users\vcvmadmin\Desktop\win-x64\TestMount1\");

            CollectionAssert.AreEquivalent(
                expectedDiskProperties.Select(p => $"{p.Key}={p.Value}"),
                actualDiskVolume.Properties.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void WindowsDiskManagerHandlesRaceConditionsInTheEvaluationOfDetailDiskToListPartitionResults()
        {
            // There is the possibility of a race condition when listing partitions directly after
            // the 'detail disk' command. This happens if the standard output is not fully cleared from the
            // 'detail disk' command before the the 'list partition' command executes.
            ConcurrentBuffer detailDiskAndPartitionResults = new ConcurrentBuffer()
                // We cannot assume the results from the 'detail disk' command have been cleared in time.
                .AppendLine("           ")
                .AppendLine("Virtual HD ATA Device")
                .AppendLine("Disk ID: EF349D83")
                .AppendLine("Type   : ATA")
                .AppendLine("Status : Online")
                .AppendLine("Path   : 0")
                .AppendLine("Target : 0")
                .AppendLine("LUN ID : 0")
                .AppendLine("Location Path : ACPI(_SB_)#ACPI(PCI0)#ACPI(IDE0)#ACPI(CHN0)#ATA(C00T00L00)")
                .AppendLine("Current Read-only State : No")
                .AppendLine("Read-only  : No")
                .AppendLine("Boot Disk  : Yes")
                .AppendLine("Pagefile Disk  : Yes")
                .AppendLine("Hibernation File Disk  : No")
                .AppendLine("Crashdump Disk  : No")
                .AppendLine("Clustered Disk  : No")
                .AppendLine("           ")
                .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                .AppendLine("  Volume 0         System Rese  NTFS   Partition    500 MB  Healthy    System  ")
                .AppendLine("  Volume 1     C   Windows      NTFS   Partition    126 GB  Healthy    Boot    ")
                // ...before the results of the 'list partition' are output
                .AppendLine("           ")
                .AppendLine(" Partition ###  Type              Size     Offset")
                .AppendLine(" -------------  ----------------  -------  -------")
                .AppendLine(" Partition 1    Primary            500 MB  1024 KB")
                .AppendLine(" Partition 2    Primary            126 GB   501 MB");

            IDictionary<string, IConvertible> expectedDiskProperties = new Dictionary<string, IConvertible>
            {
                { "Type", "07" },
                { "Hidden", "No" },
                { "Active", "Yes" },
                { "Offset in Bytes", "1048576" },
                { "Volume", "Volume 3" },
                { "Ltr", "E" },
                { "Label", null },
                { "Fs", "NTFS" },
                { "VolumeType", "Partition" },
                { "Size", "1023 GB" },
                { "Status", "Healthy" },
                { "Info", null }
            };

            IEnumerable<int> partitionIndexes = TestWindowsDiskManager.ParseDiskPartitionIndexes(detailDiskAndPartitionResults);

            Assert.IsNotNull(partitionIndexes);
            Assert.IsNotEmpty(partitionIndexes);
            CollectionAssert.AreEquivalent(new List<int> { 1, 2 }, partitionIndexes);
        }

        [Test]
        public async Task WindowsDiskManagerGetsTheExpectedDisks_Scenario1()
        {
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;

            this.standardInput.BytesWritten += (sender, data) =>
            {
                string input = data.ToString().Trim();
                if (input.Contains($"select disk"))
                {
                    int diskIndex = int.Parse(Regex.Match(input, "[0-9]+").Value);
                    this.testProcess.StandardOutput.Append($"Disk {diskIndex} is now the selected disk.");
                }
                else if (input.Contains($"select partition"))
                {
                    int partitionIndex = int.Parse(Regex.Match(input, "[0-9]+").Value);
                    this.testProcess.StandardOutput.Append($"Partition {partitionIndex} is now the selected partition.");
                }
                else if (input.Contains("list disk"))
                {
                    StringBuilder listDiskResults = new StringBuilder()
                        .AppendLine("           ")
                        .AppendLine(" Disk ###  Status         Size     Free     Dyn  Gpt")
                        .AppendLine(" --------  -------------  -------  -------  ---  ---")
                        .AppendLine(" Disk 0    Online          127 GB  1024 KB          ")
                        .AppendLine(" Disk 1    Online           32 GB      0 B          ")
                        .AppendLine(" Disk 2    Online         1024 GB      0 B          ");

                    this.testProcess.StandardOutput.Append(listDiskResults.ToString());
                }
                else if (input.Contains("list partition"))
                {
                    StringBuilder listPartitionResults = new StringBuilder()
                       .AppendLine("           ")
                       .AppendLine(" Partition ###  Type              Size     Offset")
                       .AppendLine(" -------------  ----------------  -------  -------")
                       .AppendLine(" Partition 1    Primary            500 MB  1024 KB")
                       .AppendLine(" Partition 2    Primary            126 GB   501 MB");

                    this.testProcess.StandardOutput.Append(listPartitionResults.ToString());
                }
                else if (input.Contains($"detail disk"))
                {
                    StringBuilder detailDiskResults = new StringBuilder()
                        .AppendLine("           ")
                        .AppendLine("Virtual HD ATA Device")
                        .AppendLine("Disk ID: EF349D83")
                        .AppendLine("Type   : ATA")
                        .AppendLine("Status : Online")
                        .AppendLine("Path   : 0")
                        .AppendLine("Target : 0")
                        .AppendLine("LUN ID : 0")
                        .AppendLine("           ")
                        .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                        .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                        .AppendLine("  Volume 0         System Rese  NTFS   Partition    500 MB  Healthy    System  ")
                        .AppendLine("  Volume 1     C   Windows      NTFS   Partition    126 GB  Healthy    Boot    ");

                    this.testProcess.StandardOutput.Append(detailDiskResults.ToString());
                }
                else if (input.Contains($"detail partition"))
                {
                    StringBuilder detailPartitionResults = new StringBuilder()
                       .AppendLine("           ")
                       .AppendLine("Partition 1")
                       .AppendLine("Type  : 07")
                       .AppendLine("Hidden: No")
                       .AppendLine("Active: Yes")
                       .AppendLine("Offset in Bytes: 525336576")
                       .AppendLine("           ")
                       .AppendLine("  Volume ###  Ltr  Label        Fs     Type        Size     Status     Info    ")
                       .AppendLine("  ----------  ---  -----------  -----  ----------  -------  ---------  --------")
                       .AppendLine("  Volume 1     C   Windows      NTFS   Partition    126 GB  Healthy    Boot    ");

                    this.testProcess.StandardOutput.Append(detailPartitionResults.ToString());
                }
            };

            IEnumerable<Disk> actualDisks = await this.diskManager.GetDisksAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsNotNull(actualDisks);
            Assert.IsNotEmpty(actualDisks);
            Assert.IsTrue(actualDisks.Count() == 3);
            actualDisks.ToList().ForEach(disk => Assert.IsTrue(disk.Volumes.Count() == 2));
        }

        private class TestWindowsDiskManager : WindowsDiskManager
        {
            public TestWindowsDiskManager(ProcessManager processManager)
                : base(processManager)
            {
            }

            public new static IEnumerable<string> GetFixedWidthLines(IEnumerable<string> lines)
            {
                return WindowsDiskManager.GetFixedWidthLines(lines);
            }

            public new static IEnumerable<int> ParseDiskIndexes(ConcurrentBuffer diskPartOutput)
            {
                return WindowsDiskManager.ParseDiskIndexes(diskPartOutput);
            }

            public new static IDictionary<string, IConvertible> ParseDiskProperties(ConcurrentBuffer diskPartOutput)
            {
                return WindowsDiskManager.ParseDiskProperties(diskPartOutput);
            }

            public new static IEnumerable<int> ParseDiskPartitionIndexes(ConcurrentBuffer diskPartOutput)
            {
                return WindowsDiskManager.ParseDiskPartitionIndexes(diskPartOutput);
            }

            public new static DiskVolume ParseDiskVolume(ConcurrentBuffer diskPartOutput)
            {
                return WindowsDiskManager.ParseDiskVolume(diskPartOutput);
            }
        }
    }
}
