// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using System.Runtime.CompilerServices;
    using VirtualClient.Common;
    using System.Text;

    [TestFixture]
    [Category("Unit")]
    public class FioExecutorTests
    {
        private MockFixture mockFixture;
        private IDictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;
        private string mockCommandLine;
        private string mockResults;

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
                { nameof(FioExecutor.TestName), "fio_test_1" }
            };

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("fio", this.mockFixture.GetPackagePath("fio")));
            this.mockFixture.File.OnFileExists().Returns(true);
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
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads]_direct --size=496GB --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth]";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads]_direct";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496GB";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.Threads)] = 32;

            using (TestFioExecutor diskSpdExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyParameters(EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                Assert.AreEqual(
                    $"--name=fio_randwrite_496GB_4k_d16_th32_direct --size=496GB --numjobs=32 --rw=randwrite --bs=4k --iodepth=16",
                    commandLine);

                Assert.AreEqual($"fio_randwrite_496GB_4k_d16_th32_direct", testName);
            }
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=disk_fill --size=[diskfillsize] --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "disk_fill";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496GB";

            using (TestFioExecutor diskSpdExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyParameters(EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                Assert.AreEqual($"--name=disk_fill --size=496GB --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly_StressConfiguration()
        {
            // Stress Configuration -> Stress Profile
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads]_direct --size=496GB --numjobs=[threads] --rw=randwrite --bs=4k --iodepth=[queuedepth]";
            this.profileParameters[nameof(DiskSpdExecutor.TestName)] = "fio_randwrite_[filesize]_4k_d[queuedepth]_th[threads]_direct";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496GB";

            using (TestFioExecutor diskSpdExecutor = new TestFioExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                diskSpdExecutor.ApplyConfiguration("Stress", EventContext.None);

                string commandLine = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = diskSpdExecutor.Parameters[nameof(DiskSpdExecutor.TestName)].ToString();

                int logicalCores = Environment.ProcessorCount;
                int threads = logicalCores / 2;
                int queueDepth = 512 / threads;

                Assert.AreEqual(
                    $"--name=fio_randwrite_496GB_4k_d{queueDepth}_th{threads}_direct --size=496GB --numjobs={threads} --rw=randwrite --bs=4k --iodepth={queueDepth}",
                    commandLine);

                Assert.AreEqual($"fio_randwrite_496GB_4k_d{queueDepth}_th{threads}_direct", testName);
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

            public new void ApplyConfiguration(string configuration, EventContext telemetryContext)
            {
                base.ApplyConfiguration(configuration, telemetryContext);
            }

            public new void ApplyParameters(EventContext telemetryContext)
            {
                base.ApplyParameters(telemetryContext);
            }

            public new DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
            {
                return base.CreateWorkloadProcess(executable, commandArguments, testedInstance, disksToTest);
            }
        }
    }
}
