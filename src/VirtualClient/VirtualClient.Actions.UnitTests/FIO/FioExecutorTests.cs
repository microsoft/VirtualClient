// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FioExecutorTests
    {
        private MockFixture mockFixture;
        private IDictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;
        private string mockCommandLine;
        private string mockResults;
        private DependencyPath mockWorkloadPackage;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockResults = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Examples", "FIO", "Results_FIO.json"));
        }

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupMocks();

            // Setup default profile parameter values.
            this.mockCommandLine = "--name=fio_test_1 --ioengine=libaio";
            this.profileParameters = new Dictionary<string, IConvertible>
            {
                { nameof(FioExecutor.CommandLine), this.mockCommandLine },
                { nameof(FioExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(FioExecutor.DeleteTestFilesOnFinish), "true" },
                { nameof(FioExecutor.MetricScenario), "fio_test_1" },
                { nameof(FioExecutor.PackageName), "fio" }
            };

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("fio", this.mockFixture.GetPackagePath("fio")));
            this.mockFixture.File.OnFileExists().Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                return new InMemoryProcess
                {
                    OnHasExited = () => true,
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    StandardOutput = new ConcurrentBuffer(new StringBuilder(this.mockResults))
                };
            };

            string workloadName = "fio";
            this.mockWorkloadPackage = new DependencyPath(
                workloadName,
                this.mockFixture.PlatformSpecifics.GetPackagePath(workloadName));
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount}_direct --size=496GB --numjobs={ThreadCount} --rw=randwrite --bs=4k --iodepth={QueueDepth}";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount}_direct";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496GB";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.ThreadCount)] = 32;

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual(
                    $"--name=fio_randwrite_496GB_4k_d16_th32_direct --size=496GB --numjobs=32 --rw=randwrite --bs=4k --iodepth=16",
                    commandLine);

                Assert.AreEqual($"fio_randwrite_496GB_4k_d16_th32_direct", testName);
            }
        }

        [Test]
        public void FioExecutorThrowsOnNullCommandLineAndJobFiles()
        {
            this.profileParameters[nameof(TestFioExecutor.CommandLine)] = null;
            this.profileParameters[nameof(TestFioExecutor.JobFiles)] = null;

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                WorkloadException error = Assert.Throws<WorkloadException>(executor.Validate);

                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, error.Reason);
            }
        }

        [Test]
        public void FioExecutorThrowsIfCommandLineAndJobFilesIncluded()
        {
            this.profileParameters[nameof(TestFioExecutor.CommandLine)] = "--name=fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount}_direct --size=496GB --numjobs={ThreadCount} --rw=randwrite --bs=4k --iodepth={QueueDepth}";
            this.profileParameters[nameof(TestFioExecutor.JobFiles)] = "{ScriptPath:fio}/oltp-c.fio.jobfile";

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                WorkloadException error = Assert.Throws<WorkloadException>(executor.Validate);

                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, error.Reason);
            }
        }

        [Test]
        public async Task FioExecutorRunsCommandWithJobFile()
        {
            this.profileParameters[nameof(TestFioExecutor.CommandLine)] = null;
            this.profileParameters[nameof(TestFioExecutor.JobFiles)] = "jobfile1path";

            DependencyPath workloadPlatformSpecificPackage =
                this.mockFixture.ToPlatformSpecificPath(this.mockWorkloadPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture);

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                string updatedJobFilePath = this.mockFixture.PlatformSpecifics.Combine(workloadPlatformSpecificPackage.Path, "jobfile1path");

                Assert.AreEqual($"{updatedJobFilePath} --output-format=json", executor.CommandLine);
            }
        }

        [Test]
        public async Task FioExecutorRunsCommandWithMultipleJobFiles()
        {
            this.profileParameters[nameof(TestFioExecutor.CommandLine)] = null;
            this.profileParameters[nameof(TestFioExecutor.JobFiles)] = "path/to/jobfile1,path/jobfile2;path/jobfile3";

            DependencyPath workloadPlatformSpecificPackage =
                this.mockFixture.ToPlatformSpecificPath(this.mockWorkloadPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture);

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                string updatedJobFile1Path = this.mockFixture.PlatformSpecifics.Combine(workloadPlatformSpecificPackage.Path, "jobfile1");
                string updatedJobFile2Path = this.mockFixture.PlatformSpecifics.Combine(workloadPlatformSpecificPackage.Path, "jobfile2");
                string updatedJobFile3Path = this.mockFixture.PlatformSpecifics.Combine(workloadPlatformSpecificPackage.Path, "jobfile3");

                Assert.AreEqual($"{updatedJobFile1Path} {updatedJobFile2Path} {updatedJobFile3Path} --output-format=json", executor.CommandLine);
            }
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=disk_fill --size={DiskFillSize} --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "disk_fill";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496GB";

            using (TestFioExecutor executor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual($"--name=disk_fill --size=496GB --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public async Task FioExecutorCreatesExpectedMountPointsForDisksUnderTest_RemoteDiskScenario()
        {
            // Clear any access points out.
            this.disks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));

            List<Tuple<DiskVolume, string>> mountPointsCreated = new List<Tuple<DiskVolume, string>>();

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.CreateMountPointAsync(It.IsAny<DiskVolume>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<DiskVolume, string, CancellationToken>((volume, mountPoint, token) =>
                {
                    (volume.AccessPaths as List<string>).Add(mountPoint);
                })
                .Returns(Task.CompletedTask);

            using (TestFioExecutor workloadExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                await workloadExecutor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(this.disks.Skip(1).All(d => d.Volumes.First().AccessPaths?.Any() == true));

                string expectedMountPoint1 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdd1");
                Assert.AreEqual(expectedMountPoint1, this.disks.ElementAt(1).Volumes.First().AccessPaths.First());

                string expectedMountPoint2 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sde1");
                Assert.AreEqual(expectedMountPoint2, this.disks.ElementAt(2).Volumes.First().AccessPaths.First());

                string expectedMountPoint3 = Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdf1");
                Assert.AreEqual(expectedMountPoint3, this.disks.ElementAt(3).Volumes.First().AccessPaths.First());
            }
        }

        [Test]
        public void FioExecutorCreatesTheExpectedWorkloadProcess_Scenario1()
        {
            using (TestFioExecutor fioExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                string expectedCommand = "/home/any/fio";
                string expectedArguments = "--name=fio_test --ioengine=libaio";
                string expectedTestedInstance = "remote_disk_123";
                string expectedMountPath = "/any/mount/path1";

                Disk diskToTest = this.disks.Where(disk => !disk.IsOperatingSystem()).First();
                (diskToTest.Volumes.First().AccessPaths as List<string>).Clear();
                (diskToTest.Volumes.First().AccessPaths as List<string>).Add(expectedMountPath);

                DiskWorkloadProcess workloadProcess = fioExecutor.CreateWorkloadProcess(expectedCommand, expectedArguments, expectedTestedInstance, diskToTest);

                Assert.IsNotNull(workloadProcess);
                Assert.IsNotNull(workloadProcess.Process);
                Assert.IsTrue($"{workloadProcess.Command} {workloadProcess.CommandArguments}".StartsWith($"sudo {expectedCommand} {expectedArguments}"));
                Assert.AreEqual(expectedTestedInstance, workloadProcess.Categorization);
                Assert.AreEqual(1, workloadProcess.TestFiles.Count());
                Assert.IsTrue(workloadProcess.TestFiles.First().StartsWith(expectedMountPath));
                Assert.IsTrue(workloadProcess.CommandArguments.Contains($"--filename={expectedMountPath}"));
            }
        }

        [Test]
        public void FioExecutorCreatesTheExpectedWorkloadProcess_Scenario2()
        {
            using (TestFioExecutor fioExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                string expectedCommand = "/home/any/fio";
                string expectedArguments = "--name=fio_test --ioengine=libaio";
                string expectedTestedInstance = "remote_disk_123";
                string expectedMountPath1 = "/any/mount/path1";
                string expectedMountPath2 = "/any/mount/path2";

                IEnumerable<Disk> disksToTest = this.disks.Where(disk => !disk.IsOperatingSystem()).Take(2);
                disksToTest.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));
                (disksToTest.ElementAt(0).Volumes.First().AccessPaths as List<string>).Add(expectedMountPath1);
                (disksToTest.ElementAt(1).Volumes.First().AccessPaths as List<string>).Add(expectedMountPath2);

                DiskWorkloadProcess workloadProcess = fioExecutor.CreateWorkloadProcess(expectedCommand, expectedArguments, expectedTestedInstance, disksToTest.ToArray());

                Assert.IsNotNull(workloadProcess);
                Assert.IsNotNull(workloadProcess.Process);
                Assert.IsTrue($"{workloadProcess.Command} {workloadProcess.CommandArguments}".StartsWith($"sudo {expectedCommand} {expectedArguments}"));
                Assert.AreEqual(expectedTestedInstance, workloadProcess.Categorization);
                Assert.AreEqual(2, workloadProcess.TestFiles.Count());
                Assert.IsTrue(workloadProcess.TestFiles.ElementAt(0).StartsWith(expectedMountPath1));
                Assert.IsTrue(workloadProcess.TestFiles.ElementAt(1).StartsWith(expectedMountPath2));
                Assert.IsTrue(workloadProcess.CommandArguments.Contains($"--filename={expectedMountPath1}"));
                Assert.IsTrue(workloadProcess.CommandArguments.Contains($"--filename={expectedMountPath2}"));
            }
        }

        private class TestFioExecutor : FioExecutor
        {
            public TestFioExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Func<IEnumerable<Disk>, CancellationToken, bool> OnCreateMountPoints { get; set; }

            public Func<string, string, string, string[], DiskWorkloadProcess> OnCreateProcess { get; set; }

            public new Task EvaluateParametersAsync(EventContext telemetryContext)
            {
                return base.EvaluateParametersAsync(telemetryContext);
            }

            public new DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
            {
                return base.CreateWorkloadProcess(executable, commandArguments, testedInstance, disksToTest);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}
