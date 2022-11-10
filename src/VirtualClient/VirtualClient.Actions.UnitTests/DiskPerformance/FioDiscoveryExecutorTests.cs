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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    class FioDiscoveryExecutorTests
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

            // Setup default profile parameter values

            this.mockFixture.Parameters.Add(nameof(DiskPerformanceWorkloadExecutor.ProcessModel), WorkloadProcessModel.SingleProcess);
            this.mockFixture.Parameters.Add(nameof(DiskPerformanceWorkloadExecutor.CommandLine), $"--runtime=300 --rw=[{nameof(FioDiscoveryExecutor.IOType)}] --bs=[{nameof(FioDiscoveryExecutor.BlockSize)}]");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.DiskFillSize), "140G");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.FileSize), "134G");
            this.mockFixture.Parameters.Add(nameof(DiskPerformanceWorkloadExecutor.DeleteTestFilesOnFinish), "true");
            this.mockFixture.Parameters.Add("PackageName", "fio");
            this.mockFixture.Parameters[nameof(DiskPerformanceWorkloadExecutor.Scenario)] = "AnyScenario_[IOType]_[BlockSize]";

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            string rawtext = File.ReadAllText(Path.Combine(FioDiscoveryExecutorTests.ExamplesPath, "Results_FIO.json"));
            this.defaultOutput.Clear();
            this.defaultOutput.Append(rawtext);

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix,true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);

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
        }

        [Test]
        public async Task FioDiscoveryExecutorInitializeAsExpected()
        {
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.DiskFill), "True");
            this.mockFixture.Parameters[nameof(DiskPerformanceWorkloadExecutor.CommandLine)] = $"--runtime=300 --rw=write";
            using (TestFioDiscoveryExecutor fioDiscoveryExecutor = new TestFioDiscoveryExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        Assert.IsTrue(arguments.Contains($"--runtime=300 --rw=write --ioengine=libaio"));
                    }
                    return this.defaultMemoryProcess;
                };

                await fioDiscoveryExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        public async Task FioDiscoveryExecutorRunsExpectedExecutions()
        {
            int executions = 0;
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.QueueDepths), "4,16,64");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.BlockSize), "4k");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.MaxThreads), "1024");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.IOType), "randwrite");
            using (TestFioDiscoveryExecutor fioDiscoveryExecutor = new TestFioDiscoveryExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            { 
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                    }
                    return this.defaultMemoryProcess;
                };

                await fioDiscoveryExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            Assert.IsTrue(executions == 3);
        }

        
        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedInSingleProcessModel()
        {
            int executions = 0;
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.QueueDepths), "16");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.BlockSize), "16k");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.MaxThreads), "32");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.IOType), "randwrite");

            using (TestFioDiscoveryExecutor fioDiscoveryExecutor = new TestFioDiscoveryExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                        Assert.IsTrue(arguments.Contains($"--runtime=300 --rw=randwrite --bs=16k --name=AnyScenario_randwrite_16k_d1_th16 --numjobs=16 --iodepth=1 --ioengine=libaio "));
                    }
                    return this.defaultMemoryProcess;
                };

                await fioDiscoveryExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
            Assert.IsTrue(executions == 1);

        }

        [Test]
        public async Task FioDiscoveryExecutorExecutesAsExpectedInSingleProcessPerDiskModel()
        {
            int executions = 0;
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.QueueDepths), "16");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.BlockSize), "16k");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.MaxThreads), "32");
            this.mockFixture.Parameters.Add(nameof(FioDiscoveryExecutor.IOType), "randwrite");
            this.mockFixture.Parameters[nameof(FioDiscoveryExecutor.ProcessModel)] = WorkloadProcessModel.SingleProcessPerDisk;

            List<string> expectedCommandLines = new List<string>
            {
                $"--runtime=300 --rw=randwrite --bs=16k --name=AnyScenario_randwrite_16k_d1_th16 --numjobs=16 --iodepth=1 --ioengine=libaio ",
                $"--runtime=300 --rw=randwrite --bs=16k --name=AnyScenario_randwrite_16k_d1_th16 --numjobs=16 --iodepth=1 --ioengine=libaio "
            };

            using (TestFioDiscoveryExecutor fioDiscoveryExecutor = new TestFioDiscoveryExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (!arguments.Contains("chmod"))
                    {
                        executions++;
                        
                        Assert.IsTrue(expectedCommandLines.Where(cmd => arguments.Contains(cmd)).Count() != 0);
                    }
                    return this.defaultMemoryProcess;
                };

                await fioDiscoveryExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }

            Assert.IsTrue(executions == 3);

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
