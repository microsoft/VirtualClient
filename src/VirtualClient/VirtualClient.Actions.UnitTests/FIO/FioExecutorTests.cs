// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.DiskPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Serilog.Sinks.File;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FioExecutorTests : MockFixture
    {
        private IDictionary<string, IConvertible> profileParameters;
        private IEnumerable<Disk> disks;
        private string mockCommandLine;
        private string mockResults;
        private DependencyPath mockPackage;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "FIO", "Results_FIO.json");
        }

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);
            this.SetupMocks();

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

            this.disks = this.CreateDisks(PlatformID.Unix, true);

            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);
            this.mockPackage = new DependencyPath("fio", this.GetPackagePath("fio"));
            this.SetupPackage(this.mockPackage);
            this.File.OnFileExists().Returns(true);
            this.File.Setup(file => file.ReadAllText(It.IsAny<string>())).Returns(string.Empty);
            this.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
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
        public void FioExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => !disk.IsOperatingSystem());

            using (TestFioExecutor workloadExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void FioExecutorSelectsTheExpectedDisksForTest_OSDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => disk.IsOperatingSystem());
            this.profileParameters[nameof(FioExecutor.DiskFilter)] = "osdisk";

            using (TestFioExecutor workloadExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void FioExecutorSelectsTheExpectedDisksForTest_AllDisksScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks;
            this.profileParameters[nameof(FioExecutor.DiskFilter)] = "none";
            using (TestFioExecutor workloadExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void FioExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined_LinuxScenario()
        {
            // The default mock setups create 3 data disks:
            // /dev/sdc
            // /dev/sdd
            // /dev/sde
            //
            // We are looking for the first 2 when we define the TestDisks -> /dev/sdd,/dev/sde
            this.disks = this.CreateDisks(PlatformID.Unix, true);

            this.profileParameters["DiskFilter"] = "DiskPath:/dev/sdd,/dev/sde";
            using (TestFioExecutor workloadExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = workloadExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(2, disksToTest.Count());
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), disksToTest.ElementAt(0)));
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(3), disksToTest.ElementAt(1)));
            }
        }

        [Test]
        public void FioExecutorIdentifiesTheExpectedDisksAndGeneratesCorrectCommands_NvmeDisksScenario()
        {
            // Setup NVMe disks with volumes - using CreateDisks approach similar to the existing test setup
            List<Disk> nvmeDisks = new List<Disk>
            {
                // OS disk with volume
                this.CreateDisk(0, PlatformID.Unix, os: true, "/dev/nvme0n0", "/dev/nvme0n0p1"),
                // Data disks with volumes - these are the ones we want to test
                this.CreateDisk(1, PlatformID.Unix, os: false, "/dev/nvme0n1", "/dev/nvme0n1p1"),
                this.CreateDisk(2, PlatformID.Unix, os: false, "/dev/nvme2n1", "/dev/nvme2n1p1"),
                this.CreateDisk(3, PlatformID.Unix, os: false, "/dev/nvme3n1", "/dev/nvme3n1p1"),
                // Additional disk that should not be selected
                this.CreateDisk(4, PlatformID.Unix, os: false, "/dev/nvme1n1", "/dev/nvme1n1p1")
            };

            this.disks = nvmeDisks;
            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);

            // Set DiskFilter to specific NVMe paths
            this.profileParameters["DiskFilter"] = "DiskPath:/dev/nvme0n1,/dev/nvme2n1,/dev/nvme3n1";

            using (TestFioExecutor fioExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                // Test 1: Verify disk selection
                IEnumerable<Disk> disksToTest = fioExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(3, disksToTest.Count(), "Expected exactly 3 disks to be selected");
                Assert.IsTrue(disksToTest.Any(d => d.DevicePath == "/dev/nvme0n1"), "Expected /dev/nvme0n1 to be selected");
                Assert.IsTrue(disksToTest.Any(d => d.DevicePath == "/dev/nvme2n1"), "Expected /dev/nvme2n1 to be selected");
                Assert.IsTrue(disksToTest.Any(d => d.DevicePath == "/dev/nvme3n1"), "Expected /dev/nvme3n1 to be selected");
                Assert.IsFalse(disksToTest.Any(d => d.DevicePath == "/dev/nvme1n1"), "/dev/nvme1n1 should not be selected");
                Assert.IsFalse(disksToTest.Any(d => d.IsOperatingSystem()), "OS disk should not be selected");

                // Test 2: Verify command generation for SingleProcess model
                string expectedCommand = "/home/any/fio";
                string expectedArguments = "--name=fio_randwrite_test --size=128G --numjobs=16 --rw=randwrite --bs=4k --iodepth=32 --ioengine=libaio --direct=1 --ramp_time=30 --runtime=120 --time_based --overwrite=1 --thread --group_reporting --output-format=json";
                string processModel = WorkloadProcessModel.SingleProcess;

                IEnumerable<DiskWorkloadProcess> workloadProcesses = fioExecutor.CreateWorkloadProcesses(expectedCommand, expectedArguments, disksToTest, processModel, EventContext.None);

                Assert.IsTrue(workloadProcesses.Count() == 1, "SingleProcess model should create exactly 1 process");
                DiskWorkloadProcess workloadProcess = workloadProcesses.First();

                // The actual command line generated - get it for verification
                string actualCommandLine = workloadProcess.Process.FullCommand();

                // Verify the command includes all three NVMe disks
                Assert.IsTrue(actualCommandLine.Contains("nvme0n1"), "Command should reference nvme0n1");
                Assert.IsTrue(actualCommandLine.Contains("nvme2n1"), "Command should reference nvme2n1");
                Assert.IsTrue(actualCommandLine.Contains("nvme3n1"), "Command should reference nvme3n1");
                Assert.IsFalse(actualCommandLine.Contains("nvme1n1"), "Command should NOT reference nvme1n1 (not in filter)");
                Assert.IsFalse(actualCommandLine.Contains("nvme0n0"), "Command should NOT reference nvme0n0 (OS disk)");

                // Verify the expected command structure
                Assert.IsTrue(actualCommandLine.StartsWith("sudo /home/any/fio"), "Command should start with sudo /home/any/fio");
                Assert.IsTrue(actualCommandLine.Contains("--name=fio_randwrite_test"), "Command should contain the test name");
                Assert.IsTrue(actualCommandLine.Contains("--ioengine=libaio"), "Command should contain the IO engine");

                // Count the number of --filename parameters (should be 3, one for each disk)
                int filenameCount = Regex.Matches(actualCommandLine, @"--filename=").Count;
                Assert.AreEqual(3, filenameCount, "Command should have exactly 3 --filename parameters for the 3 selected NVMe disks");
            }
        }

        [Test]
        public void FioExecutorAppliesConfigurationParametersCorrectly()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount}_direct --size=496GB --numjobs={ThreadCount} --rw=randwrite --bs=4k --iodepth={QueueDepth}";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "fio_randwrite_{FileSize}_4k_d{QueueDepth}_th{ThreadCount}_direct";
            this.profileParameters[nameof(DiskSpdExecutor.FileSize)] = "496GB";
            this.profileParameters[nameof(DiskSpdExecutor.QueueDepth)] = 16;
            this.profileParameters[nameof(DiskSpdExecutor.ThreadCount)] = 32;

            using (TestFioExecutor executor = new TestFioExecutor(this.Dependencies, this.profileParameters))
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
        public void FioExecutorAppliesConfigurationParametersCorrectly_DiskFillScenario()
        {
            this.profileParameters[nameof(DiskSpdExecutor.CommandLine)] = "--name=disk_fill --size={DiskFillSize} --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread";
            this.profileParameters[nameof(DiskSpdExecutor.MetricScenario)] = "disk_fill";
            this.profileParameters[nameof(DiskSpdExecutor.DiskFillSize)] = "496GB";

            using (TestFioExecutor executor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                executor.EvaluateParametersAsync(EventContext.None);

                string commandLine = executor.Parameters[nameof(DiskSpdExecutor.CommandLine)].ToString();
                string testName = executor.Parameters[nameof(DiskSpdExecutor.MetricScenario)].ToString();

                Assert.AreEqual($"--name=disk_fill --size=496GB --numjobs=1 --rw=write --bs=256k --iodepth=64 --direct=1 --thread", commandLine);
                Assert.AreEqual($"disk_fill", testName);
            }
        }

        [Test]
        public void FioExecutorCreatesTheExpectedWorkloadProcesses_SingleProcess()
        {
            using (TestFioExecutor fioExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                string expectedCommand = "/home/any/fio";
                string expectedArguments = "--name=fio_randwrite_test --size=128G --numjobs=16 --rw=randwrite --bs=4k --iodepth=32 --ioengine=libaio --direct=1 --ramp_time=30 --runtime=120 --time_based --overwrite=1 --thread --group_reporting --output-format=json";
                string processModel = WorkloadProcessModel.SingleProcess;

                IEnumerable<Disk> disksToTest = this.disks.Where(disk => !disk.IsOperatingSystem());
                IEnumerable<DiskWorkloadProcess> workloadProcesses = fioExecutor.CreateWorkloadProcesses(expectedCommand, expectedArguments, disksToTest, processModel, EventContext.None);

                Assert.IsTrue(workloadProcesses.Count() == 1);
                DiskWorkloadProcess workloadProcess = workloadProcesses.First();

                string expectedCommandLine = $"sudo {expectedCommand} {expectedArguments} {string.Join(" ", disksToTest.Select(disk => $"--filename={this.Combine(disk.GetPreferredAccessPath(this.Platform), fioExecutor.FileName)}"))}";
                string actualCommandLine = workloadProcess.Process.FullCommand();

                Assert.AreEqual(expectedCommandLine, actualCommandLine);
            }
        }

        [Test]
        public void FioExecutorCreatesTheExpectedWorkloadProcesses_SingleProcessAggregated()
        {
            using (TestFioExecutor fioExecutor = new TestFioExecutor(this.Dependencies, this.profileParameters))
            {
                string expectedCommand = "/home/any/fio";
                string expectedArguments = 
                    $"--name=fio_randwrite_128G_4K --size=128G --numjobs=16 --rw=randwrite --bs=4k --iodepth=32 --ioengine=libaio --direct=1 " +
                    $"--ramp_time=30 --runtime=120 --time_based --overwrite=1 --thread --group_reporting --output-format=json";

                string processModel = WorkloadProcessModel.SingleProcessAggregated;

                IEnumerable<Disk> disksToTest = this.disks.Where(disk => !disk.IsOperatingSystem());
                IEnumerable<DiskWorkloadProcess> workloadProcesses = fioExecutor.CreateWorkloadProcesses(expectedCommand, expectedArguments, disksToTest, processModel, EventContext.None);

                Assert.IsTrue(workloadProcesses.Count() == 1);
                DiskWorkloadProcess workloadProcess = workloadProcesses.First();

                // The --name (job name) parameter must be removed or it will override the job specifics
                // in the job file causing FIO to run the workload different than expected.

                string expectedCommandLine = Regex.Replace(
                    $"sudo {expectedCommand} {expectedArguments} {this.GetTempPath($"{fioExecutor.ExperimentId}.fio".ToLowerInvariant())}",
                    @"--name=[\x21-\x7E]+\s*",
                    string.Empty,
                    RegexOptions.IgnoreCase);

                string actualCommandLine = workloadProcess.Process.FullCommand();

                Assert.AreEqual(expectedCommandLine, actualCommandLine);
            }
        }

        [Test]
        public void FioExecutorCreatesTheExpectedJobFileContentForSingleProcessAggregatedScenarios()
        {
            IEnumerable<Disk> disksToTest = this.disks.Where(disk => !disk.IsOperatingSystem());

            StringBuilder jobFileContent = new StringBuilder();
            jobFileContent.AppendLine("# Dynamically created job file.");
            jobFileContent.AppendLine("#");
            jobFileContent.AppendLine("# Description:");
            jobFileContent.AppendLine("# Distributes the command line definition across each of the target disks");
            jobFileContent.AppendLine("# running 1 job per disk. This is intended to produce results that are aggregated");
            jobFileContent.AppendLine("# across all disks.");

            int jobNumber = 0;
            foreach (Disk disk in disksToTest)
            {
                jobNumber++;
                string jobName = $"job_{jobNumber}";
                jobFileContent.AppendLine();
                jobFileContent.AppendLine($"[{jobName}]");

                string fileName = this.Combine(disk.GetPreferredAccessPath(this.Platform), "fio-test.dat");
                jobFileContent.AppendLine($"filename={fileName}");
            }

            string expectedJobFileContent = jobFileContent.ToString();
            string actualJobFileContent = TestFioExecutor.CreateJobFileContent(this.PlatformSpecifics, disksToTest, "job", "fio-test.dat");

            Assert.AreEqual(expectedJobFileContent, actualJobFileContent);
        }

        private class TestFioExecutor : FioExecutor
        {
            public TestFioExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Func<IEnumerable<Disk>, CancellationToken, bool> OnCreateMountPoints { get; set; }

            public Func<string, string, string, string[], DiskWorkloadProcess> OnCreateProcess { get; set; }

            public new static string CreateJobFileContent(PlatformSpecifics platformSpecifics, IEnumerable<Disk> targetDisks, string jobNamePrefix, string testFileName)
            {
                return FioExecutor.CreateJobFileContent(platformSpecifics, targetDisks, jobNamePrefix, testFileName);
            }

            public new IEnumerable<DiskWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel, EventContext telemetryContext)
            {
                return base.CreateWorkloadProcesses(executable, commandArguments, disks, processModel, telemetryContext);
            }

            public new Task EvaluateParametersAsync(EventContext telemetryContext)
            {
                return base.EvaluateParametersAsync(telemetryContext);
            }

            public new IEnumerable<Disk> GetDisksToTest(IEnumerable<Disk> disks)
            {
                return base.GetDisksToTest(disks);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}
