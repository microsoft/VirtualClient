// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class FioMultiThroughputExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;
        private static readonly string ExamplesPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(FioMetricsParserTests)).Location),
            "Examples",
            "FIO");
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();
        private IEnumerable<Disk> disks;
        private IProcessProxy defaultMemoryProcess;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPath = this.mockFixture.Create<DependencyPath>();
            this.mockFixture.SetupMocks();

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.TemplateJobFile), "oltp-c.fio.jobfile");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.GroupReporting), 1);
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.DurationSec), 1);
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomIOFileSize), "124G");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialIOFileSize), "20G");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.DirectIO), 1);
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.TargetIOPS), "5000");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.TargetPercents), "10");

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomReadBlockSize), "8K");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomReadQueueDepth), "512");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomReadNumJobs), "1");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomReadWeight), "5416");

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomWriteBlockSize), "8K");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomWriteQueueDepth), "512");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomWriteNumJobs), "1");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.RandomWriteWeight), "4255");


            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialReadBlockSize), "56K");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialReadQueueDepth), "64");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialReadNumJobs), "1");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialReadWeight), "0");

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialWriteBlockSize), "56K");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialWriteQueueDepth), "64");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialWriteNumJobs), "1");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialWriteWeight), "329");

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.TestName), "mockTestName");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.PackageName), "fio");
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.CommandLine), "--output-format=json --fallocate=none");

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            string rawtext = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesPath, "Results_FIO.json"));
            string templateJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesPath, "oltp-c.fio.jobfile"));
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>())).Returns(templateJobFile);

            this.defaultOutput.Clear();
            this.defaultOutput.Append(rawtext);

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

            this.defaultMemoryProcess = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "exe",
                    Arguments = "args"
                },
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true,
                StandardOutput = this.defaultOutput
            };
        }

        [Test]
        public async Task FioMultiThroughputExecutorInitializeAsExpected()
        {
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.DiskFill), "True");
            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        Assert.IsTrue(arguments.Equals($"{this.mockPath.Path}/linux-x64/fio {this.mockPath.Path}/linux-x64/" +
                            $"{nameof(FioMultiThroughputExecutor)}" +
                            $"{executor.Parameters[nameof(FioMultiThroughputExecutor.TemplateJobFile)]} " +
                            $"--section initrandomio --section initsequentialio " +
                            $"--time_based --output-format=json --thread --fallocate=none"));
                    }
                    return this.defaultMemoryProcess;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FioMultiThroughputExecutorRunsExpectedExecutions()
        {
            int executions = 0;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.TargetPercents)] = "10,20,30,50,70,100,110";

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
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

            Assert.IsTrue(executions == 7);
        }

        [Test]
        public void FioMultiThroughputExecutorDoesNotSupportRunningAgainstTheOperatingSystemDisk_1()
        {
            // Scenario:
            // The disks selected includes the OS disk only (i.e. the disk filter specified pointed at the OS disk)
            this.mockFixture.Parameters[nameof(FioDiscoveryExecutor.DiskFilter)] = "OSDisk:true";
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks.Where(d => d.IsOperatingSystem()));

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.NotSupported, error.Reason);
            }
        }

        [Test]
        public void FioMultiThroughputExecutorDoesNotSupportRunningAgainstTheOperatingSystemDisk_2()
        {
            // Scenario:
            // The disks selected includes the OS disk with others (i.e. the disk filter specified pointed at the OS disk)
            this.mockFixture.Parameters[nameof(FioDiscoveryExecutor.DiskFilter)] = "OSDisk:true";

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.NotSupported, error.Reason);
            }
        }

        [Test]
        public async Task FioMultiThroughputExecutorExecutesExpectedCommandLine()
        {
            int executions = 0;
            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                        Assert.IsTrue(arguments.Equals($"{this.mockPath.Path}/linux-x64/fio {this.mockPath.Path}/linux-x64/" +
                            $"{nameof(FioMultiThroughputExecutor)}" +
                            $"{executor.Parameters[nameof(FioMultiThroughputExecutor.TemplateJobFile)]}" +
                            $" --section randomreader --section randomwriter --section sequentialwriter --time_based --output-format=json --thread --fallocate=none"));
                    }
                    return this.defaultMemoryProcess;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            Assert.IsTrue(executions == 1);

        }

        [Test]
        public async Task FioMultiThroughputExecutorCreatesExpectedJobFileForMultipleDisks()
        {
            bool createdExpectedJobFile = false;

            this.disks = new List<Disk>
            {
                FixtureExtensions.CreateDisk(0, PlatformID.Unix, true, os: true),
                FixtureExtensions.CreateDisk(1, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(2, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(3, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(4, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(5, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(6, PlatformID.Unix, true, os: false)
            };
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesPath, "expectedoltp-c.fio1.jobfile"));

            using (TestFioMultiThroughputExecutor fioMultiThroughputExecutor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), expectedJobFile))
                    .Callback((string path, string contents) =>
                    {
                        createdExpectedJobFile = true;
                    });

                await fioMultiThroughputExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.IsTrue(createdExpectedJobFile);
        }

        [Test]
        public async Task FioMultiThroughputExecutorCreatesExpectedJobFileForMultipleDisksAnd2SequentialDisks()
        {
            bool createdExpectedJobFile = false;

            this.disks = new List<Disk>
            {
                FixtureExtensions.CreateDisk(0, PlatformID.Unix, true, os: true),
                FixtureExtensions.CreateDisk(1, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(2, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(3, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(4, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(5, PlatformID.Unix, true, os: false),
                FixtureExtensions.CreateDisk(6, PlatformID.Unix, true, os: false)
            };
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialDiskCount), "2");

            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesPath, "expectedoltp-c.fio2.jobfile"));

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), expectedJobFile))
                    .Callback((string path, string contents) =>
                    {
                        createdExpectedJobFile = true;
                    });

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.IsTrue(createdExpectedJobFile);
        }

        [Test]
        public async Task FioMultiThroughputExecutorCreatesExpectedJobFileForSingleDisk()
        {
            bool createdExpectedJobFile = false;

            this.disks = new List<Disk>
            {
                FixtureExtensions.CreateDisk(0, PlatformID.Unix, true, os: true),
                FixtureExtensions.CreateDisk(1, PlatformID.Unix, true, os: false)
            };
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesPath, "expectedoltp-c.fio3.jobfile"));

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), expectedJobFile))
                    .Callback((string path, string contents) =>
                    {
                        createdExpectedJobFile = true;
                    });

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.IsTrue(createdExpectedJobFile);
        }

        private class TestFioMultiThroughputExecutor : FioMultiThroughputExecutor
        {
            public TestFioMultiThroughputExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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