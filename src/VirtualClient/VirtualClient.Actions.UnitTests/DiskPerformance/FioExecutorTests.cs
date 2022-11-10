// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
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
                { nameof(DiskPerformanceWorkloadExecutor.CommandLine), this.mockCommandLine },
                { nameof(DiskPerformanceWorkloadExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(DiskPerformanceWorkloadExecutor.DeleteTestFilesOnFinish), "true" },
            };

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    }
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

                DiskPerformanceWorkloadProcess workloadProcess = fioExecutor.CreateWorkloadProcess(expectedCommand, expectedArguments, expectedTestedInstance, diskToTest);

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

                DiskPerformanceWorkloadProcess workloadProcess = fioExecutor.CreateWorkloadProcess(expectedCommand, expectedArguments, expectedTestedInstance, disksToTest.ToArray());

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

            public Func<string, string, string, string[], DiskPerformanceWorkloadProcess> OnCreateProcess { get; set; }

            public new void ApplyConfiguration(string configuration, EventContext telemetryContext)
            {
                base.ApplyConfiguration(configuration, telemetryContext);
            }

            public new void ApplyParameters(EventContext telemetryContext)
            {
                base.ApplyParameters(telemetryContext);
            }

            public new DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
            {
                return base.CreateWorkloadProcess(executable, commandArguments, testedInstance, disksToTest);
            }
        }
    }
}
