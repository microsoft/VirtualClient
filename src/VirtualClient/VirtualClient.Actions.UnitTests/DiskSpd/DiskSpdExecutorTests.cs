// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [Platform(Exclude = "Unix,Linux,MacOsX")]
    public class DiskSpdExecutorTests : MockFixture
    {
        private DependencyPath mockPackage;
        private Dictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;
        private string output;

        [SetUp]
        public void SetupTest()
        {
            this.disks = this.CreateDisks(PlatformID.Win32NT, true);
            this.SetupDisks(this.disks?.ToArray());

            // The workload requires the DiskSpd package to be registered (either built-in or installed).
            this.mockPackage = new DependencyPath("diskspd", this.GetPackagePath("diskspd"));
            this.SetupPackage(this.mockPackage);
            this.SetupFile(this.Combine(this.mockPackage.Path, "win-arm64", "diskspd.exe"));
            this.SetupFile(this.Combine(this.mockPackage.Path, "win-x64", "diskspd.exe"));

            this.profileParameters = new Dictionary<string, IConvertible>()
            {
                { nameof(DiskSpdExecutor.PackageName), "diskspd" },
                { nameof(DiskSpdExecutor.CommandLine), "-c4G -b4K -r4K -t1 -o1 -w100" },
                { nameof(DiskSpdExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(DiskSpdExecutor.MetricScenario), "diskspd_randwrite_4GB_direct" }
            };

            this.output = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "DiskSpd", "DiskSpdExample-ReadWrite.txt");

            this.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                return new Mock<IProcessProxy>()
                    .Setup(command, arguments, workingDir, this.output).Object;
            };
        }

        [Test]
        public void DiskSpdExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => !disk.IsOperatingSystem());

            using (TestDiskSpdExecutor workloadExecutor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskSpdExecutorSelectsTheExpectedDisksForTest_OSDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => disk.IsOperatingSystem());
            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "osdisk";

            using (TestDiskSpdExecutor workloadExecutor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskSpdExecutorSelectsTheExpectedDisksForTest_AllDisksScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks;
            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "none";
            using (TestDiskSpdExecutor workloadExecutor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskSpdExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined()
        {
            // The default mock setups create 4 disks:
            // C:
            // D:
            // E:
            // F:

            this.disks = this.CreateDisks(PlatformID.Win32NT, true);

            this.profileParameters["DiskFilter"] = "DiskPath:D:,E:";
            using (TestDiskSpdExecutor workloadExecutor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = workloadExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(2, disksToTest.Count());
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), disksToTest.ElementAt(0)));
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), disksToTest.ElementAt(1)));
            }
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c{FileSize} -b8K -r4K -t{ThreadCount} -o{QueueDepth} -w0 -d480 -Suw -W15 -D -L -Rtext";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "diskspd_randread_{FileSize}_8k_d{QueueDepth}_th{ThreadCount}";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496G";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.ThreadCount)] = 32;

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual($"-c496G -b8K -r4K -t32 -o16 -w0 -d480 -Suw -W15 -D -L -Rtext", commandLine);
                Assert.AreEqual($"diskspd_randread_496G_8k_d16_th32", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario1()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c{DiskFillSize} -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "disk_fill";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496G";

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual($"-c496G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        [TestCase("496GB", "496G")]
        [TestCase("128MB", "128M")]
        [TestCase("128KB", "128K")]
        public void DiskSpdExecutorCorrectsTheDiskFillSizeAndFileSizeParameters(string size, string correctedSize)
        {
            // Note that this is not a valid DiskSpd command line. This is used ONLY to ensure
            // the file sizes are corrected.
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c{DiskFillSize} -c{FileSize} -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "disk_fill";

            // The disk fill size '496GB' is not supported. It should be formatted to '496G'.
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = size;
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = size;

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual($"-c{correctedSize} -c{correctedSize} -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorValidatesRequiredProfileParameters()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = null;
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.profileParameters[nameof(DiskSpdExecutor.ProcessModel)] = "notallowed";
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void DiskSpdExecutorCreateProcessesValidatesStringParameters(string invalidParameter)
        {
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                Assert.Throws<ArgumentException>(
                    () => executor.CreateWorkloadProcesses(invalidParameter, "some string", this.disks, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => executor.CreateWorkloadProcesses("some string", invalidParameter, this.disks, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => executor.CreateWorkloadProcesses("some string", "some string", this.disks, invalidParameter));

                Assert.Throws<ArgumentNullException>(
                    () => executor.CreateWorkloadProcesses("some string", "some string", null, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => executor.CreateWorkloadProcesses("some string", "some string", new List<Disk>(), WorkloadProcessModel.SingleProcess));
            }
        }

        [Test]
        public void DiskSpdExecutorCreateProcessCreatesTheExpectedWorkloadProcess_SingleProcessModel()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedexe = "some exe";
            string expectedArguments = "some command";
            IEnumerable<Disk> expectedDisks = this.CreateDisks(PlatformID.Win32NT, true).TakeLast(2);
            IEnumerable<string> expectedAccessPaths = expectedDisks.Select(disk => disk.Volumes.FirstOrDefault().AccessPaths.FirstOrDefault());

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                processCount++;
                Assert.AreEqual(expectedexe, exe);
                Assert.IsTrue(arguments.StartsWith($"{expectedArguments}"));
                expectedAccessPaths.ToList().ForEach(path => Assert.IsTrue(arguments.Contains(path)));

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = expectedexe,
                        Arguments = expectedArguments
                    }
                };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(expectedexe, expectedArguments, expectedDisks, WorkloadProcessModel.SingleProcess);
            }

            Assert.AreEqual(1, processCount);
        }

        [Test]
        public void DiskSpdExecutorCreateProcessCreatesTheExpectedWorkloadProcesses_SingleProcessPerDiskModel()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedexe = "some exe";
            string expectedArguments = "some command";
            IEnumerable<Disk> expectedDisks = this.CreateDisks(PlatformID.Win32NT, true).TakeLast(2);
            IEnumerable<string> expectedAccessPaths = expectedDisks.Select(disk => disk.Volumes.FirstOrDefault().AccessPaths.FirstOrDefault());

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedexe, exe);
                Assert.IsTrue(arguments.StartsWith($"{expectedArguments}"));
                Assert.IsTrue(arguments.Contains(expectedAccessPaths.ElementAt(processCount)));
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = expectedexe,
                        Arguments = expectedArguments
                    }
                };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(expectedexe, expectedArguments, expectedDisks, WorkloadProcessModel.SingleProcessPerDisk);
            }

            Assert.AreEqual(expectedDisks.Count(), processCount);
        }

        [Test]
        public async Task DiskSpdExecutorExecutesExpectedProcesses()
        {
            InMemoryProcess process1 = new InMemoryProcess
            {
                OnStart = () => true,
                OnHasExited = () => true
            };

            InMemoryProcess process2 = new InMemoryProcess
            {
                OnStart = () => true,
                OnHasExited = () => true
            };

            process1.StandardOutput.Append(this.output);
            process2.StandardOutput.Append(this.output);

            List<DiskWorkloadProcess> expectedWorkloads = new List<DiskWorkloadProcess>
            {
                new DiskWorkloadProcess(process1, "anyTestedInstance", "D:\\any\file.dat"),
                new DiskWorkloadProcess(process2, "anyTestedInstance", "E:\\any\file.dat")
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                await executor.ExecuteWorkloadsAsync(expectedWorkloads, CancellationToken.None).ConfigureAwait(false);
                Assert.IsTrue(process1.Executed);
                Assert.IsTrue(process2.Executed);
            }
        }

        [Test]
        public async Task DiskSpdExecutorDeletesTestFilesWhenTheProfileSpecifies()
        {
            this.SetupWorkloadScenario(testRemoteDisks: true);
            this.profileParameters[nameof(DiskSpdExecutor.DeleteTestFilesOnFinish)] = "true";

            int filesDeleted = 0;
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.OnDeleteTestFiles = (testFiles) =>
                {
                    filesDeleted += testFiles.Count();
                };

                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                // 3 Disks will be selected. It will be deleted after the first execution, it will also be deleted in the clean up.
                Assert.AreEqual(6, filesDeleted);
            }
        }

        [Test]
        public async Task DiskSpdExecutorDeletesTestFilesByDefault()
        {
            this.SetupWorkloadScenario(testRemoteDisks: true);

            int filesDeleted = 0;
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.OnDeleteTestFiles = (testFiles) =>
                {
                    filesDeleted += testFiles.Count();
                };

                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                // 3 Disks will be selected. It will be deleted after the first execution, it will also be deleted in the clean up.
                Assert.AreEqual(6, filesDeleted);
            }
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilterUsesPhysicalDeviceNumberSyntax_SingleProcessModel()
        {
            // Bare disks use VC's internal \\.\.PHYSICALDISK{N} identifier.
            // The executor uses DiskSpd's native #N syntax (e.g. #1, #2) derived from disk.Index.
            IEnumerable<Disk> bareDisks = new List<Disk>
            {
                this.CreateDisk(1, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK1"),
                this.CreateDisk(2, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK2")
            };

            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:1,2";
            this.profileParameters[nameof(DiskSpdExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcess;

            List<string> capturedArguments = new List<string>();
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedArguments.Add(arguments);
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments }
                };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses("diskspd.exe", "-b4K -r4K -t1 -o1 -w100", bareDisks, WorkloadProcessModel.SingleProcess);
            }

            Assert.AreEqual(1, capturedArguments.Count);
            // DiskSpd #N syntax -- derived from disk.Index, not disk.DevicePath.
            // DiskSpd uses IOCTL_DISK_GET_DRIVE_GEOMETRY_EX internally; no -c needed.
            Assert.IsTrue(capturedArguments[0].Contains(" #1"));
            Assert.IsTrue(capturedArguments[0].Contains(" #2"));
            // No file path style references should appear.
            Assert.IsFalse(capturedArguments[0].Contains("diskspd-test.dat"));
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilterUsesPhysicalDeviceNumberSyntax_SingleProcessPerDiskModel()
        {
            IEnumerable<Disk> bareDisks = new List<Disk>
            {
                this.CreateDisk(1, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK1"),
                this.CreateDisk(2, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK2")
            };

            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:1,2";

            List<string> capturedArguments = new List<string>();
            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedArguments.Add(arguments);
                processCount++;
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments }
                };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses("diskspd.exe", "-b4K -r4K -t1 -o1 -w100", bareDisks, WorkloadProcessModel.SingleProcessPerDisk);
            }

            Assert.AreEqual(2, processCount);
            // Each process targets exactly one drive via DiskSpd's #N syntax (derived from disk.Index).
            Assert.IsTrue(capturedArguments[0].Contains(" #1"));
            Assert.IsFalse(capturedArguments[0].Contains(" #2"));
            Assert.IsTrue(capturedArguments[1].Contains(" #2"));
            Assert.IsFalse(capturedArguments[1].Contains(" #1"));
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilterDoesNotAppendFilenamesToCommandLine()
        {
            Disk bareDisk = this.CreateDisk(1, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK1");

            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:1";

            string capturedArguments = null;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedArguments = arguments;
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments }
                };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(
                    "diskspd.exe",
                    "-b4K -r4K -t1 -o1 -w100",
                    new[] { bareDisk },
                    WorkloadProcessModel.SingleProcess);
            }

            Assert.IsNotNull(capturedArguments);
            // DiskSpd's #N syntax is used -- derived from disk.Index=1.
            Assert.IsTrue(capturedArguments.Contains(" #1"));
            // No test-file extension should be present.
            Assert.IsFalse(capturedArguments.Contains(".dat"));
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilterStoresDeviceNumberPathsInTestFiles()
        {
            // TestFiles is iterated by DeleteTestFilesAsync. For raw disk targets the paths must be
            // the #N device number strings -- not file paths. File.Exists("#1") returns false,
            // so DeleteTestFilesAsync becomes a correct no-op.
            IEnumerable<Disk> bareDisks = new List<Disk>
            {
                this.CreateDisk(1, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK1"),
                this.CreateDisk(2, PlatformID.Win32NT, os: false, @"\\.\PHYSICALDISK2")
            };

            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:1,2";

            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
                new InMemoryProcess { StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments } };

            IEnumerable<DiskWorkloadProcess> processes;
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                processes = executor.CreateWorkloadProcesses(
                    "diskspd.exe", "-b4K -r4K -t1 -o1 -w100", bareDisks, WorkloadProcessModel.SingleProcessPerDisk).ToList();
            }

            Assert.AreEqual(2, processes.Count());
            CollectionAssert.AreEqual(new[] { "#1" }, processes.ElementAt(0).TestFiles);
            CollectionAssert.AreEqual(new[] { "#2" }, processes.ElementAt(1).TestFiles);
        }

        // -----------------------------------------------------------------------
        // DiskIndex filter in ExecuteAsync tests
        // -----------------------------------------------------------------------

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilter_BypassesDiskManagerEnumeration()
        {
            // When DiskFilter=DiskIndex:6-8, TryGetDiskIndexes resolves disks without any DiskManager call.
            bool diskManagerCalled = false;
            this.DiskManager.OnGetDisks().Returns((CancellationToken token) =>
            {
                diskManagerCalled = true;
                return Task.FromResult(this.disks);
            });

            bool result = DiskFilters.TryGetDiskIndexes("DiskIndex:6-8", out IEnumerable<int> indexes);

            Assert.IsTrue(result);
            Assert.IsNotNull(indexes);
            Assert.AreEqual(3, indexes.Count());
            Assert.IsFalse(diskManagerCalled, "DiskManager must NOT be called by TryGetDiskIndexes.");
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilter_CreatesOneProcessPerDisk()
        {
            // DiskFilter=DiskIndex:6-8 → disks 6, 7, 8 → 3 processes (SingleProcessPerDisk model).
            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:6-8";

            List<string> capturedArguments = new List<string>();
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedArguments.Add(arguments);
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments }
                };
            };

            DiskFilters.TryGetDiskIndexes("DiskIndex:6-8", out IEnumerable<int> indexes);
            IEnumerable<Disk> disksFromRange = indexes.Select(i => new Disk(i, $@"\\.\PHYSICALDISK{i}")).ToList();

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(
                    "diskspd.exe",
                    "-b128K -d60 -o32 -t1 -r -w0 -Sh -L -Rxml",
                    disksFromRange,
                    WorkloadProcessModel.SingleProcessPerDisk);
            }

            Assert.AreEqual(3, capturedArguments.Count, "Expected one process per disk in the range.");
            Assert.IsTrue(capturedArguments[0].TrimEnd().EndsWith("#6"));
            Assert.IsTrue(capturedArguments[1].TrimEnd().EndsWith("#7"));
            Assert.IsTrue(capturedArguments[2].TrimEnd().EndsWith("#8"));
        }

        [Test]
        public void DiskSpdExecutorWithDiskIndexFilter_UsesPhysicalDiskIndexSyntaxInCommandLine()
        {
            // Confirm #N notation (not a file path) is written to each spawned process.
            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:10-11";

            List<string> capturedArguments = new List<string>();
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedArguments.Add(arguments);
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments }
                };
            };

            DiskFilters.TryGetDiskIndexes("DiskIndex:10-11", out IEnumerable<int> indexes);
            IEnumerable<Disk> disksFromRange = indexes.Select(i => new Disk(i, $@"\\.\PHYSICALDISK{i}")).ToList();

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(
                    "diskspd.exe",
                    "-b128K -d60 -o32 -t1 -r -w0 -Sh -L -Rxml",
                    disksFromRange,
                    WorkloadProcessModel.SingleProcessPerDisk);
            }

            Assert.AreEqual(2, capturedArguments.Count);
            foreach (string args in capturedArguments)
            {
                Assert.IsTrue(args.Contains(" #10") || args.Contains(" #11"),
                    $"Expected '#N' syntax but got: {args}");
                Assert.IsFalse(args.Contains(".dat"),
                    $"Unexpected .dat file path: {args}");
            }
        }

        [Test]
        public void DiskSpdExecutorWithoutDiskIndexFilter_GetDisksToTestUsesFilteredDiskSet()
        {
            // When RawDiskIndexRange is NOT set, GetDisksToTest must use DiskFilters and return
            // disks from the DiskManager-sourced collection (not a hardcoded range).
            // This is the normal (non-raw-range) code path.
            IEnumerable<Disk> nonOsDisks = this.disks.Where(disk => !disk.IsOperatingSystem());

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = executor.GetDisksToTest(this.disks);

                // The default filter excludes the OS disk, so only non-OS disks should be returned.
                CollectionAssert.AreEquivalent(
                    nonOsDisks.Select(d => d.DevicePath),
                    disksToTest.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskSpdExecutorThrowsWhenBothDiskIndexFilterAndDiskFillAreEnabled()
        {
            this.profileParameters[nameof(DiskSpdExecutor.DiskFilter)] = "DiskIndex:6-8";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFill)] = true;
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "500G";

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                WorkloadException exc = Assert.Throws<WorkloadException>(() => executor.Validate());
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exc.Reason);
            }
        }

        // -----------------------------------------------------------------------
        // Log file naming tests (ExecuteWorkloadAsync)
        // -----------------------------------------------------------------------

        [Test]
        public async Task DiskSpdExecutorWithDiskIndexFilter_UsesScenarioAndDiskIndexAsLogFileName()
        {
            // When the test file is a #N physical disk target the log filename must encode both
            // the scenario and the disk ordinal: "{Scenario}_disk{N}.log"
            this.profileParameters[nameof(DiskSpdExecutor.Scenario)] = "RandomRead_128k_BlockSize";
            this.profileParameters[nameof(DiskSpdExecutor.LogToFile)] = true;

            string capturedLogFilePath = null;
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, ct) =>
                {
                    if (path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        capturedLogFilePath = path;
                    }
                })
                .Returns(Task.CompletedTask);

            InMemoryProcess process = new InMemoryProcess
            {
                OnStart = () => true,
                OnHasExited = () => true
            };
            process.StandardOutput.Append(this.output);

            DiskWorkloadProcess workload = new DiskWorkloadProcess(process, "SingleProcessPerDisk,OsDisk:false,1", "#6");

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                await executor.ExecuteWorkloadsAsync(new[] { workload }, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNotNull(capturedLogFilePath, "Expected a log file to be written.");
            Assert.IsTrue(
                Path.GetFileName(capturedLogFilePath).Contains("_disk6"),
                $"Expected log filename to contain '_disk6' but was: {capturedLogFilePath}");
        }

        [Test]
        public async Task DiskSpdExecutorWithoutDiskIndexFilter_DoesNotAddDiskIndexToLogFileName()
        {
            // When the test file is a regular file path the log filename must NOT contain a '_disk'
            // suffix; it should fall back to the default (scenario name only).
            this.profileParameters[nameof(DiskSpdExecutor.Scenario)] = "RandomRead_128k_BlockSize";
            this.profileParameters[nameof(DiskSpdExecutor.LogToFile)] = true;

            string capturedLogFilePath = null;
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, ct) =>
                {
                    if (path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        capturedLogFilePath = path;
                    }
                })
                .Returns(Task.CompletedTask);

            InMemoryProcess process = new InMemoryProcess
            {
                OnStart = () => true,
                OnHasExited = () => true
            };
            process.StandardOutput.Append(this.output);

            DiskWorkloadProcess workload = new DiskWorkloadProcess(process, "SingleProcessPerDisk,OsDisk:false,1", "D:\\any\\file.dat");

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                await executor.ExecuteWorkloadsAsync(new[] { workload }, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNotNull(capturedLogFilePath, "Expected a log file to be written.");
            Assert.IsFalse(
                Path.GetFileName(capturedLogFilePath).Contains("_disk"),
                $"Expected no '_disk' suffix in log filename when test file is not a '#N' target, but was: {capturedLogFilePath}");
        }

        [Test]
        public async Task DiskSpdExecutorWithDiskIndexFilter_LogFilenameContainsCorrectDiskOrdinalForEachProcess()
        {
            // Multiple workloads (#42 and #180) must each produce a log file whose
            // name encodes their own disk index, not a shared or wrong value.
            this.profileParameters[nameof(DiskSpdExecutor.Scenario)] = "SequentialRead_1024k_BlockSize";
            this.profileParameters[nameof(DiskSpdExecutor.LogToFile)] = true;

            List<string> capturedPaths = new List<string>();
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, ct) =>
                {
                    if (path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
                    {
                        capturedPaths.Add(path);
                    }
                })
                .Returns(Task.CompletedTask);

            InMemoryProcess p42 = new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
            InMemoryProcess p180 = new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
            p42.StandardOutput.Append(this.output);
            p180.StandardOutput.Append(this.output);

            List<DiskWorkloadProcess> workloads = new List<DiskWorkloadProcess>
            {
                new DiskWorkloadProcess(p42, "instance", "#42"),
                new DiskWorkloadProcess(p180, "instance", "#180")
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                await executor.ExecuteWorkloadsAsync(workloads, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(2, capturedPaths.Count, "Expected one log file per workload.");
            Assert.IsTrue(
                capturedPaths.Any(p => Path.GetFileName(p).Contains("_disk42")),
                $"Expected a log file with '_disk42'. Paths: {string.Join(", ", capturedPaths)}");
            Assert.IsTrue(
                capturedPaths.Any(p => Path.GetFileName(p).Contains("_disk180")),
                $"Expected a log file with '_disk180'. Paths: {string.Join(", ", capturedPaths)}");
        }

        [Test]
        public async Task DiskSpdExecutorRawDiskTarget_ParsesGetPhysicalDiskOutput()
        {
            // Simulate Get-PhysicalDisk output: one integer DeviceId per line.
            string psOutput = "6\r\n7\r\n8\r\n180";

            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (exe.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
                {
                    InMemoryProcess p = new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
                    p.StandardOutput.Append(psOutput);
                    return p;
                }

                return new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disks = await executor.DiscoverRawDisksAsync(CancellationToken.None);

                Assert.AreEqual(4, disks.Count());
                Assert.AreEqual(6, disks.ElementAt(0).Index);
                Assert.AreEqual(7, disks.ElementAt(1).Index);
                Assert.AreEqual(8, disks.ElementAt(2).Index);
                Assert.AreEqual(180, disks.ElementAt(3).Index);
            }
        }

        [Test]
        public async Task DiskSpdExecutorRawDiskTarget_InvokesGetPhysicalDiskViaPowerShell()
        {
            // The discovery method must invoke powershell.exe with Get-PhysicalDisk.
            string capturedExe = null;
            string capturedArgs = null;

            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                capturedExe = exe;
                capturedArgs = arguments;
                InMemoryProcess p = new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
                p.StandardOutput.Append("6\r\n7");
                return p;
            };

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.Dependencies, this.profileParameters))
            {
                await executor.DiscoverRawDisksAsync(CancellationToken.None);
            }

            Assert.AreEqual("powershell.exe", capturedExe, "Expected powershell.exe to be invoked.");
            Assert.IsTrue(
                capturedArgs.Contains("Get-PhysicalDisk"),
                $"Expected 'Get-PhysicalDisk' in the PowerShell command but got: {capturedArgs}");
            Assert.IsTrue(
                capturedArgs.Contains("MediaType") && capturedArgs.Contains("HDD"),
                $"Expected HDD MediaType filter in the PowerShell command but got: {capturedArgs}");
        }

        [Test]
        public void DiskSpdExecutorWithExplicitDiskIndexRange_DoesNotInvokePowerShellDiscovery()
        {
            // When DiskFilter=DiskIndex:<explicit range> (not "hdd"), TryGetDiskIndexes returns
            // explicit indexes without any OS call. PowerShell must NOT be invoked.
            bool powershellInvoked = false;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (exe.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
                {
                    powershellInvoked = true;
                }

                return new InMemoryProcess { OnStart = () => true, OnHasExited = () => true };
            };

            bool result = DiskFilters.TryGetDiskIndexes("DiskIndex:6-8", out IEnumerable<int> indexes);

            Assert.IsTrue(result);
            Assert.IsNotNull(indexes);
            Assert.IsFalse(powershellInvoked, "PowerShell must not be invoked for an explicit DiskIndex range.");
            Assert.AreEqual(3, indexes.Count());
            Assert.IsTrue(indexes.All(i => i >= 6 && i <= 8));
        }

        private IEnumerable<Disk> SetupWorkloadScenario(
            bool testRemoteDisks = false, bool testOSDisk = false, string processModel = WorkloadProcessModel.SingleProcess)
        {
            List<Disk> diskUnderTest = new List<Disk>();
            if (testOSDisk)
            {
                diskUnderTest.AddRange(this.disks.Where(disk => disk.IsOperatingSystem()));
            }

            if (testRemoteDisks)
            {
                diskUnderTest.AddRange(this.disks.Where(disk => !disk.IsOperatingSystem()));
            }

            this.profileParameters["ProcessModel"] = processModel;

            return diskUnderTest;
        }

        private class TestDiskSpdExecutor : DiskSpdExecutor
        {
            public TestDiskSpdExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Func<IEnumerable<Disk>, CancellationToken, bool> OnCreateMountPoints { get; set; }

            public Action<IEnumerable<string>> OnDeleteTestFiles { get; set; }

            public new Task EvaluateParametersAsync(EventContext telemetryContext)
            {
                return base.EvaluateParametersAsync(telemetryContext);
            }

            public new IEnumerable<DiskWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel)
            {
                return base.CreateWorkloadProcesses(executable, commandArguments, disks, processModel);
            }

            public new Task ExecuteWorkloadsAsync(IEnumerable<DiskWorkloadProcess> workloads, CancellationToken cancellationToken)
            {
                return base.ExecuteWorkloadsAsync(workloads, cancellationToken);
            }

            public new IEnumerable<Disk> GetDisksToTest(IEnumerable<Disk> disks)
            {
                return base.GetDisksToTest(disks);
            }

            public new Task<IEnumerable<Disk>> DiscoverRawDisksAsync(CancellationToken cancellationToken)
            {
                return base.DiscoverRawDisksAsync(cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }

            protected override Task DeleteTestFilesAsync(IEnumerable<string> testFiles, IAsyncPolicy retryPolicy = null)
            {
                this.OnDeleteTestFiles?.Invoke(testFiles);
                return base.DeleteTestFilesAsync(testFiles, retryPolicy);
            }
        }
    }
}
