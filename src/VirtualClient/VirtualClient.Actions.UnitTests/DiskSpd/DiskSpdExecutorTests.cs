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
