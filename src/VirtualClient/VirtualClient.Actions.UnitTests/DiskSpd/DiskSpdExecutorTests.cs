// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [Platform(Exclude = "Unix,Linux,MacOsX")]
    public class DiskSpdExecutorTests
    {
        private DependencyFixture fixture;
        private Dictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;
        private string output;

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
                { nameof(DiskSpdExecutor.MetricScenario), "diskspd_randwrite_4GB_direct" }
            };

            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DiskSpd", "DiskSpdExample-ReadWrite.txt");
            this.output = File.ReadAllText(outputPath);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append(this.output);

                return process;
            };
        }

        [Test]
        public void DiskSpdExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "-c{FileSize} -b8K -r4K -t{ThreadCount} -o{QueueDepth} -w0 -d480 -Suw -W15 -D -L -Rtext";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "diskspd_randread_{FileSize}_8k_d{QueueDepth}_th{ThreadCount}";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496G";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.ThreadCount)] = 32;

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }

            this.profileParameters[nameof(DiskSpdExecutor.ProcessModel)] = "notallowed";
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                executor.CreateWorkloadProcesses(expectedexe, expectedArguments, expectedDisks, WorkloadProcessModel.SingleProcessPerDisk);
            }

            Assert.AreEqual(expectedDisks.Count(), processCount);
        }

        [Test]
        public async Task DiskSpdExecutorCreatesExpectedMountPointsForDisksUnderTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> remoteDisks = this.SetupWorkloadScenario(testRemoteDisks: true);

            // Clear any access points out.
            remoteDisks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));

            using (TestDiskSpdExecutor workloadExecutor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
            {
                await workloadExecutor.ExecuteAsync(CancellationToken.None);

                Assert.IsNotEmpty(remoteDisks);
                Assert.AreEqual(3, remoteDisks.SelectMany(d => d.Volumes.Where(v => v.AccessPaths?.Any() == true)).Count());

                string expectedMountPoint1 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_d");
                Assert.AreEqual(expectedMountPoint1, remoteDisks.ElementAt(0).Volumes.ElementAt(0).AccessPaths.First());

                string expectedMountPoint2 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_e");
                Assert.AreEqual(expectedMountPoint2, remoteDisks.ElementAt(1).Volumes.ElementAt(0).AccessPaths.First());

                string expectedMountPoint3 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_f");
                Assert.AreEqual(expectedMountPoint3, remoteDisks.ElementAt(2).Volumes.ElementAt(0).AccessPaths.First());
            }
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

            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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
            using (TestDiskSpdExecutor executor = new TestDiskSpdExecutor(this.fixture.Dependencies, this.profileParameters))
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
