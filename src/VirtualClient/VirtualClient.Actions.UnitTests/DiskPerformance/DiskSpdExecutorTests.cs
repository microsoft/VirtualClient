// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DiskSpdExecutorTests
    {
        private DependencyFixture fixture;
        private Dictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new DependencyFixture();
            this.disks = this.fixture.CreateDisks(PlatformID.Win32NT, true);
            this.fixture.DiskManager.AddRange(this.disks);

            // The workload requires the DiskSpd package to be registered (either built-in or installed).
            this.fixture.SetupWorkloadPackage("diskspd", expectedFiles: @"win-x64\diskspd.exe");

            this.profileParameters = new Dictionary<string, IConvertible>()
            {
                { nameof(DiskSpdExecutor.PackageName), "diskspd" },
                { nameof(DiskSpdExecutor.CommandLine), "-c4G -b4K -r4K -t1 -o1 -w100" },
                { nameof(DiskSpdExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(DiskSpdExecutor.TestName), "diskspd_randwrite_4GB_direct" }
            };
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c[filesize] -b8K -r4K -t[threads] -o[queuedepth] -w0 -d480 -Suw -W15 -D -L -Rtext";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "diskspd_randread_[filesize]_8k_d[queuedepth]_th[threads]";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496G";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.Threads)] = 32;

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyParameters(EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                Assert.AreEqual($"-c496G -b8K -r4K -t32 -o16 -w0 -d480 -Suw -W15 -D -L -Rtext", commandLine);
                Assert.AreEqual($"diskspd_randread_496G_8k_d16_th32", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario1()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c[diskfillsize] -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "disk_fill";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496G";

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyParameters(EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                Assert.AreEqual($"-c496G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario2()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c[diskfillsize] -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "disk_fill";

            // The disk fill size '496GB' is not supported. It should be formatted to '496G'.
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496GB"; 

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyParameters(EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                Assert.AreEqual($"-c496G -b256K -si4K -t1 -o64 -w100 -Suw -W15 -D -L", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly_StressConfiguration()
        {
            // Stress Configuration -> Stress Profile
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c[filesize] -b8K -r4K -t[threads] -o[queuedepth] -w0 -d480 -Suw -W15 -D -L -Rtext";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "diskspd_randread_[filesize]_8k_d[queuedepth]_th[threads]";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496G";

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyConfiguration("Stress", EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                int logicalCores = Environment.ProcessorCount;
                int threads = logicalCores / 2;
                int queueDepth = 512 / threads;

                Assert.AreEqual($"-c496G -b8K -r4K -t{threads} -o{queueDepth} -w0 -d480 -Suw -W15 -D -L -Rtext", commandLine);
                Assert.AreEqual($"diskspd_randread_496G_8k_d{queueDepth}_th{threads}", testName);
            }
        }

        [Test]
        public void DiskSpdExecutorThrowsIfAnUnsupportedConfigurationIsDefined()
        {
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                WorkloadException error = Assert.Throws<WorkloadException>(
                    () => diskSpdExecutor.ApplyConfiguration("ConfigurationNotSupported", EventContext.None));

                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, error.Reason);
            }
        }

        [Test]
        public void DiskSpdExecutorValidatesRequiredProfileParameters()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = null;
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                Assert.Throws<WorkloadException>(() => diskSpdExecutor.ValidateParameters());
            }

            this.profileParameters[nameof(DiskSpdExecutor.ProcessModel)] = "notallowed";
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                Assert.Throws<WorkloadException>(() => diskSpdExecutor.ValidateParameters());
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void DiskSpdExecutorCreateProcessesValidatesStringParameters(string invalidParameter)
        {
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                Assert.Throws<ArgumentException>(
                    () => diskSpdExecutor.CreateWorkloadProcesses(invalidParameter, "some string", this.disks, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => diskSpdExecutor.CreateWorkloadProcesses("some string", invalidParameter, this.disks, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => diskSpdExecutor.CreateWorkloadProcesses("some string", "some string", this.disks, invalidParameter));

                Assert.Throws<ArgumentNullException>(
                    () => diskSpdExecutor.CreateWorkloadProcesses("some string", "some string", null, WorkloadProcessModel.SingleProcess));

                Assert.Throws<ArgumentException>(
                    () => diskSpdExecutor.CreateWorkloadProcesses("some string", "some string", new List<Disk>(), WorkloadProcessModel.SingleProcess));
            }
        }

        [Test]
        public void DiskSpdExecutorCreateProcessCreatesTheExpectedWorkloadProcess_SingleProcessModel()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedexe = "some exe";
            string expectedArguments = "some command";
            IEnumerable<Disk> expectedDisks = this.fixture.CreateDisks(PlatformID.Win32NT, true).TakeLast(2);
            IEnumerable<string> expectedAccessPaths = expectedDisks.Select(disk => disk.Volumes.FirstOrDefault().AccessPaths.FirstOrDefault());

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.CreateWorkloadProcesses(expectedexe, expectedArguments, expectedDisks, WorkloadProcessModel.SingleProcess);
            }

            Assert.AreEqual(1, processCount);
        }

        [Test]
        public void DiskSpdExecutorCreateProcessCreatesTheExpectedWorkloadProcesses_SingleProcessPerDiskModel()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedexe = "some exe";
            string expectedArguments = "some command";
            IEnumerable<Disk> expectedDisks = this.fixture.CreateDisks(PlatformID.Win32NT, true).TakeLast(2);
            IEnumerable<string> expectedAccessPaths = expectedDisks.Select(disk => disk.Volumes.FirstOrDefault().AccessPaths.FirstOrDefault());

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.CreateWorkloadProcesses(expectedexe, expectedArguments, expectedDisks, WorkloadProcessModel.SingleProcessPerDisk);
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

            List<DiskPerformanceWorkloadProcess> expectedWorkloads = new List<DiskPerformanceWorkloadProcess>
            {
                new DiskPerformanceWorkloadProcess(process1, "anyTestedInstance", "D:\\any\file.dat"),
                new DiskPerformanceWorkloadProcess(process2, "anyTestedInstance", "E:\\any\file.dat")
            };

            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                await diskSpdExecutor.ExecuteWorkloadsAsync(expectedWorkloads, CancellationToken.None).ConfigureAwait(false);
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
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.OnDeleteTestFiles = (testFiles) =>
                {
                    filesDeleted += testFiles.Count();
                };

                await diskSpdExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                // 3 Disks will be selected. It will be deleted after the first execution, it will also be deleted in the clean up.
                Assert.AreEqual(6, filesDeleted);
            }
        }

        [Test]
        public async Task DiskSpdExecutorDeletesTestFilesByDefault()
        {
            this.SetupWorkloadScenario(testRemoteDisks: true);

            int filesDeleted = 0;
            using (TestDiskSpdExecutor diskSpdExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.OnDeleteTestFiles = (testFiles) =>
                {
                    filesDeleted += testFiles.Count();
                };

                await diskSpdExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
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

            public Func<IEnumerable<Disk>, CancellationToken, bool> OnCreateMountPointsAsync { get; set; }

            public Action<IEnumerable<string>> OnDeleteTestFiles { get; set; }

            public new void ApplyConfiguration(string configuration, EventContext telemetryContext)
            {
                base.ApplyConfiguration(configuration, telemetryContext);
            }

            public new void ApplyParameters(EventContext telemetryContext)
            {
                base.ApplyParameters(telemetryContext);
            }

            public new IEnumerable<DiskPerformanceWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel)
            {
                return base.CreateWorkloadProcesses(executable, commandArguments, disks, processModel);
            }

            public new Task ExecuteWorkloadsAsync(IEnumerable<DiskPerformanceWorkloadProcess> workloads, CancellationToken cancellationToken)
            {
                return base.ExecuteWorkloadsAsync(workloads, cancellationToken);
            }

            public new void ValidateParameters()
            {
                base.ValidateParameters();
            }

            protected override Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.OnCreateMountPointsAsync?.Invoke(disks, cancellationToken) ?? false);
            }

            protected override Task DeleteTestFilesAsync(IEnumerable<string> testFiles, IAsyncPolicy retryPolicy = null)
            {
                this.OnDeleteTestFiles?.Invoke(testFiles);
                return base.DeleteTestFilesAsync(testFiles, retryPolicy);
            }
        }
    }
}
