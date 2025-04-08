// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class FioDiscoveryExecutorTests : MockFixture
    {
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();
        private IEnumerable<Disk> disks;
        private IProcessProxy defaultMemoryProcess;

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("fio", this.GetPackagePath("fio"));
            
            this.SetupPackage(this.mockPackage);
            this.SetupMocks();

            // Setup default profile parameter values
            this.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(FioDiscoveryExecutor.Scenario), "AnyScenario_ReadOrWrite_AnyBlockSize" },
                { nameof(FioDiscoveryExecutor.CommandLine), "--size={FileSize} --rw={IOType} --bs={BlockSize} --direct={DirectIO} --ramp_time=30 --runtime={DurationSec} --time_based --overwrite=1 --thread --group_reporting --output-format=json" },
                { nameof(FioDiscoveryExecutor.BlockSize), "4K" },
                { nameof(FioDiscoveryExecutor.DiskFillSize), "140G" },
                { nameof(FioDiscoveryExecutor.FileSize), "134G" },
                { nameof(FioDiscoveryExecutor.DurationSec), 300 },
                { nameof(FioDiscoveryExecutor.QueueDepths), "1,4,16" },
                { nameof(FioDiscoveryExecutor.MaxThreads), 8 },
                { nameof(FioDiscoveryExecutor.IOType), "randwrite" },
                { nameof(FioDiscoveryExecutor.DirectIO), true },
                { nameof(FioDiscoveryExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(FioDiscoveryExecutor.DeleteTestFilesOnFinish), "true" },
                { nameof(FioDiscoveryExecutor.PackageName), "fio" },
                { nameof(FioDiscoveryExecutor.DiskFilter), "BiggestSize" }
            };

            
            this.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(MockFixture.ReadFile(MockFixture.ExamplesDirectory, "FIO", "Results_FIO.json"));

            this.disks = this.CreateDisks(PlatformID.Unix, true);
            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

            this.defaultMemoryProcess = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "exe",
                    Arguments = "--rw=randwrite"
                },
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true,
                StandardOutput = this.defaultOutput
            };

            this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = file,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true,
                    StandardOutput = this.defaultOutput
                };
            };
        }

        [Test]
        public async Task FioDiscoveryExecutorInitializeAsExpected()
        {
            this.Parameters.Add(nameof(FioDiscoveryExecutor.DiskFill), "True");
            this.Parameters[nameof(DiskWorkloadExecutor.CommandLine)] = $"--runtime=300 --rw=write";
            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        Assert.IsTrue(arguments.Contains($"--runtime=300 --rw=write --ioengine=libaio"));
                    }
                    return this.defaultMemoryProcess;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorRunsExpectedExecutions()
        {
            int executions = 0;
            this.Parameters[nameof(FioDiscoveryExecutor.QueueDepths)] = "4,16,64"; // 3 executions

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                    }
                    return this.defaultMemoryProcess;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            Assert.IsTrue(executions == 3);
        }

        [Test]
        public void FioDiscoveryExecutorDoesNotSupportRunningAgainstTheOperatingSystemDisk_1()
        {
            // Scenario:
            // The disks selected includes the OS disk only (i.e. the disk filter specified pointed at the OS disk)
            this.Parameters[nameof(FioDiscoveryExecutor.DiskFilter)] = "OSDisk:true";
            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks.Where(d => d.IsOperatingSystem()));

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.NotSupported, error.Reason);
            }
        }

        [Test]
        public void FioDiscoveryExecutorDoesNotSupportRunningAgainstTheOperatingSystemDisk_2()
        {
            // Scenario:
            // The disks selected includes the OS disk with others (i.e. the disk filter specified pointed at the OS disk)
            this.Parameters[nameof(FioDiscoveryExecutor.DiskFilter)] = "OSDisk:true";

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.NotSupported, error.Reason);
            }
        }


        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedInSingleProcessModel()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcess;

            List<string> expectedCommandLines = new List<string>
            {
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]"
            };

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.AreEqual(4, this.ProcessManager.Commands.Count());
                Assert.IsTrue(this.ProcessManager.CommandsExecuted(expectedCommandLines.ToArray()));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedInSingleProcessPerDiskModel()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcessPerDisk;

            List<string> expectedCommandLines = new List<string>
            {
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z]"
            };

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                      .ConfigureAwait(false);

                Assert.AreEqual(10, this.ProcessManager.Commands.Count());
                Assert.IsTrue(this.ProcessManager.CommandsExecuted(expectedCommandLines.ToArray()));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorUsesBufferedIOWhenInstructed()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.DirectIO)] = false;

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.IsTrue(this.ProcessManager.CommandsExecuted("--direct=0"));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorUsesDirectIOWhenInstructed()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.DirectIO)] = true;

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.IsTrue(this.ProcessManager.CommandsExecuted("--direct=1"));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorDoesNotUseDirectIOWhenInstructedOtherwise()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.DirectIO)] = false;

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.IsTrue(this.ProcessManager.CommandsExecuted("--direct=0"));
            }
        }

        [Test]
        [TestCase(true, 1)]
        [TestCase(1, 1)]
        [TestCase(false, 0)]
        [TestCase(0, 0)]
        public async Task FioDiscoveryExecutorHandlesBothBooleanAndIntegerValuesForDirectIOParameters(IConvertible parameterValue, int expectedCommandLineValue)
        {
            this.Parameters[nameof(FioDiscoveryExecutor.DirectIO)] = parameterValue;

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.IsTrue(this.ProcessManager.CommandsExecuted($"--direct={expectedCommandLineValue}"));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedIfGroupIDIsRemoved()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcess;

            List<string> expectedCommandLines = new List<string>
            {
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]"
            };

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                executor.Metadata.Remove("GroupId".CamelCased());

                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.AreEqual(4, this.ProcessManager.Commands.Count());
                Assert.IsTrue(this.ProcessManager.CommandsExecuted(expectedCommandLines.ToArray()));
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedIfGroupIDHasBadCasing()
        {
            this.Parameters[nameof(FioDiscoveryExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcess;

            List<string> expectedCommandLines = new List<string>
            {
                $"--name=fio_discovery_randwrite_134G_4K_d1_th1 --numjobs=1 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d1_th4 --numjobs=4 --iodepth=1 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]",
                $"--name=fio_discovery_randwrite_134G_4K_d2_th8 --numjobs=8 --iodepth=2 --ioengine=libaio --size=134G --rw=randwrite --bs=4K --direct=1 --ramp_time=30 --runtime=300 --time_based --overwrite=1 --thread --group_reporting --output-format=json --filename=/dev/sd[a-z] --filename=/dev/sd[a-z] --filename=/dev/sd[a-z]"
            };

            using (TestFioDiscoveryExecutor executor = new TestFioDiscoveryExecutor(this.Dependencies, this.Parameters))
            {
                executor.Metadata.Remove("GroupId".CamelCased());
                executor.Metadata.Add("grouPId", string.Empty);

                await executor.ExecuteAsync(CancellationToken.None)
                     .ConfigureAwait(false);

                Assert.AreEqual(4, this.ProcessManager.Commands.Count());
                Assert.IsTrue(this.ProcessManager.CommandsExecuted(expectedCommandLines.ToArray()));
            }
        }

        private class TestFioDiscoveryExecutor : FioDiscoveryExecutor
        {
            public TestFioDiscoveryExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {

            }
            /// <summary>
            /// Retry Wait Time for FIO executors.
            /// </summary>
            protected static new TimeSpan RetryWaitTime { get; } = TimeSpan.FromSeconds(0);

        }
    }
}

