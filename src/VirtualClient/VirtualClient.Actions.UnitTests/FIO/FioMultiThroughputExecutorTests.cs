// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests2), "Examples", "FIO");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();
        private IEnumerable<Disk> disks;
        private IProcessProxy defaultMemoryProcess;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = this.mockFixture.Create<DependencyPath>();
            this.mockFixture.SetupMocks();

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(FioMultiThroughputExecutor.TemplateJobFile), "oltp-c.fio.jobfile" },
                { nameof(FioMultiThroughputExecutor.GroupReporting), 1 },
                { nameof(FioMultiThroughputExecutor.DurationSec), 1 },
                { nameof(FioMultiThroughputExecutor.DirectIO), 1 },
                { nameof(FioMultiThroughputExecutor.TargetIOPS), "5000" },
                { nameof(FioMultiThroughputExecutor.TargetPercents), 10 },
                { nameof(FioMultiThroughputExecutor.RandomIOFileSize), "124G" },
                { nameof(FioMultiThroughputExecutor.RandomReadBlockSize), "8K" },
                { nameof(FioMultiThroughputExecutor.RandomReadQueueDepth), 512 },
                { nameof(FioMultiThroughputExecutor.RandomReadNumJobs), 1 },
                { nameof(FioMultiThroughputExecutor.RandomReadWeight), 5416 },
                { nameof(FioMultiThroughputExecutor.RandomWriteBlockSize), "8K" },
                { nameof(FioMultiThroughputExecutor.RandomWriteQueueDepth), 512 },
                { nameof(FioMultiThroughputExecutor.RandomWriteNumJobs), 1 },
                { nameof(FioMultiThroughputExecutor.RandomWriteWeight), 4255 },
                { nameof(FioMultiThroughputExecutor.SequentialIOFileSize), "20G" },
                { nameof(FioMultiThroughputExecutor.SequentialReadBlockSize), "56K" },
                { nameof(FioMultiThroughputExecutor.SequentialReadQueueDepth), 64 },
                { nameof(FioMultiThroughputExecutor.SequentialReadNumJobs), 1 },
                { nameof(FioMultiThroughputExecutor.SequentialReadWeight), 0 },
                { nameof(FioMultiThroughputExecutor.SequentialWriteBlockSize), "56K" },
                { nameof(FioMultiThroughputExecutor.SequentialWriteQueueDepth), 64 },
                { nameof(FioMultiThroughputExecutor.SequentialWriteNumJobs), 1 },
                { nameof(FioMultiThroughputExecutor.SequentialWriteWeight), 329 },
                { nameof(FioMultiThroughputExecutor.PackageName), "fio" }
            };

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            string rawtext = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesDirectory, "Results_FIO.json"));
            string templateJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesDirectory, "oltp-c.fio.jobfile"));
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
                        Assert.IsTrue(arguments.Equals($"{this.mockPackage.Path}/linux-x64/fio {this.mockFixture.PlatformSpecifics.GetScriptPath("fio")}/updated/" +
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
                        Assert.IsTrue(arguments.Equals($"{this.mockPackage.Path}/linux-x64/fio {this.mockFixture.PlatformSpecifics.GetScriptPath("fio")}/updated/" +
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
                this.mockFixture.CreateDisk(0, PlatformID.Unix, os: true, "/dev/sda", "/dev/sda1"),
                this.mockFixture.CreateDisk(1, PlatformID.Unix, os: false, "/dev/sdc", "/dev/sdc1"),
                this.mockFixture.CreateDisk(2, PlatformID.Unix, os: false, "/dev/sdd", "/dev/sdd1"),
                this.mockFixture.CreateDisk(3, PlatformID.Unix, os: false, "/dev/sde", "/dev/sde1"),
                this.mockFixture.CreateDisk(4, PlatformID.Unix, os: false, "/dev/sdf", "/dev/sdf1"),
                this.mockFixture.CreateDisk(5, PlatformID.Unix, os: false, "/dev/sdg", "/dev/sdg1"),
                this.mockFixture.CreateDisk(6, PlatformID.Unix, os: false, "/dev/sdh", "/dev/sdh1")
            };

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);
            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesDirectory, "expectedoltp-c.fio1.jobfile"));

            using (TestFioMultiThroughputExecutor fioMultiThroughputExecutor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
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
                this.mockFixture.CreateDisk(0, PlatformID.Unix, os: true, "/dev/sda", "/dev/sda1"),
                this.mockFixture.CreateDisk(1, PlatformID.Unix, os: false, "/dev/sdc", "/dev/sdc1"),
                this.mockFixture.CreateDisk(2, PlatformID.Unix, os: false, "/dev/sdd", "/dev/sdd1"),
                this.mockFixture.CreateDisk(3, PlatformID.Unix, os: false, "/dev/sde", "/dev/sde1"),
                this.mockFixture.CreateDisk(4, PlatformID.Unix, os: false, "/dev/sdf", "/dev/sdf1"),
                this.mockFixture.CreateDisk(5, PlatformID.Unix, os: false, "/dev/sdg", "/dev/sdg1"),
                this.mockFixture.CreateDisk(6, PlatformID.Unix, os: false, "/dev/sdh", "/dev/sdh1")
            };

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);
            this.mockFixture.Parameters.Add(nameof(FioMultiThroughputExecutor.SequentialDiskCount), "2");
            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesDirectory, "expectedoltp-c.fio2.jobfile"));

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
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
                this.mockFixture.CreateDisk(0, PlatformID.Unix, os: true, "/dev/sda", "/dev/sda1"),
                this.mockFixture.CreateDisk(1, PlatformID.Unix, os: false, "/dev/sdc", "/dev/sdc1")
            };

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);
            string expectedJobFile = File.ReadAllText(Path.Combine(FioMultiThroughputExecutorTests.ExamplesDirectory, "expectedoltp-c.fio3.jobfile"));

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
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
        public async Task FioMultiThroughputExecutorCreatesExpectedJobFile_Anomalous_Scenario_1()
        {
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.RandomReadNumJobs)] = 8;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.RandomReadQueueDepth)] = 2048;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.RandomWriteNumJobs)] = 8;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.RandomWriteQueueDepth)] = 2048;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.SequentialWriteNumJobs)] = 2;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.SequentialWriteQueueDepth)] = 128;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.TargetIOPS)] = 400000;
            this.mockFixture.Parameters[nameof(FioMultiThroughputExecutor.TargetPercents)] = "10,40,80,90,98,100,102,110";

            this.disks = new List<Disk>
            {
                this.mockFixture.CreateDisk(0, PlatformID.Unix, os: true, "/dev/sda", "/dev/sda1"),
                this.mockFixture.CreateDisk(1, PlatformID.Unix, os: false, "/dev/sdc", "/dev/sdc1")
            };

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                List<int> iopsValues = new List<int>();

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) => this.defaultMemoryProcess;
                this.mockFixture.FileSystem.Setup(fe => fe.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback((string path, string contents) =>
                    {
                        MatchCollection rateIops = Regex.Matches(contents, "rate_iops=(-*[0-9]+)");
                        foreach (System.Text.RegularExpressions.Match match in rateIops)
                        {
                            // Bug:
                            // We found a scenario where the use of Int32/int data types cause the mathematical
                            // calculations for some of the target IOPS to be negative numbers.
                            iopsValues.Add(int.Parse(match.Groups[1].Value));
                        }
                    });

                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsNotEmpty(iopsValues);
                Assert.IsTrue(iopsValues.All(rate => rate >= 0));
            }
        }

        [Test]
        public async Task FioMultiThroughputSucceedsIfGroupIDIsRemoved()
        {
            int executions = 0;
            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.Metadata.Remove("GroupId".CamelCased());

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                        Assert.IsTrue(arguments.Equals($"{this.mockPackage.Path}/linux-x64/fio {this.mockFixture.PlatformSpecifics.GetScriptPath("fio")}/updated/" +
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
        public async Task FioMultiThroughputSucceedsIfGroupIDHasBadCasing()
        {
            int executions = 0;
            using (TestFioMultiThroughputExecutor executor = new TestFioMultiThroughputExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.Metadata.Remove("GroupId");
                executor.Metadata.Add("grouPId", string.Empty);

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                        Assert.IsTrue(arguments.Equals($"{this.mockPackage.Path}/linux-x64/fio {this.mockFixture.PlatformSpecifics.GetScriptPath("fio")}/updated/" +
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