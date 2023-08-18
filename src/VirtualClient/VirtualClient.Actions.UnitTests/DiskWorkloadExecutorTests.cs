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
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DiskWorkloadExecutorTests
    {
        private static string dllPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DiskWorkloadExecutorTests)).Location);

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
                { nameof(DiskWorkloadExecutor.CommandLine), this.mockCommandLine },
                { nameof(DiskWorkloadExecutor.ProcessModel), WorkloadProcessModel.SingleProcess },
                { nameof(DiskWorkloadExecutor.DeleteTestFilesOnFinish), "true" }
            };

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
        }

        [Test]
        public void DiskWorkloadExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => !disk.IsOperatingSystem());

            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskWorkloadExecutorSelectsTheExpectedDisksForTest_OSDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => disk.IsOperatingSystem());
            this.profileParameters[nameof(DiskWorkloadExecutor.DiskFilter)] = "osdisk";

            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskWorkloadExecutorSelectsTheExpectedDisksForTest_AllDisksScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks;
            this.profileParameters[nameof(DiskWorkloadExecutor.DiskFilter)] = "none";
            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.DevicePath), actualDisks.Select(d => d.DevicePath));
            }
        }

        [Test]
        public void DiskWorkloadExecutorCreatesExpectdTestFilesStringSeperatedByWhitespace()
        {
            this.profileParameters[nameof(DiskWorkloadExecutor.FileName)] = "test-1.dat, test-2.dat, test-3.dat";

            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                string testFilesSeperatedByWhitesapce = workloadExecutor.GetTestFiles("D:\\any\\");
                IEnumerable<string> testFiles = testFilesSeperatedByWhitesapce.Split(" ");
                Assert.AreEqual(3, testFiles.Count());
            }
        }

        [Test]
        public void DiskWorkloadExecutorCreatesTheExpectedProcessesInTheSingleProcessModel_RemoteDiskScenario()
        {
            string expectedCommand = "/home/any/path/to/fio";
            IEnumerable<Disk> remoteDisks = this.SetupWorkloadScenario(testRemoteDisks: true);

            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<DiskWorkloadProcess> processes = workloadExecutor.CreateWorkloadProcesses(expectedCommand, this.mockCommandLine, remoteDisks, "SingleProcess");

                Assert.IsNotNull(processes);
                Assert.IsNotEmpty(processes);
                Assert.AreEqual(1, processes.Count());

                // The remote disk 1 and 2 process
                DiskWorkloadProcess process1 = processes.First();
                Assert.AreEqual(expectedCommand, process1.Command);
                Assert.IsTrue(process1.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.IsNotNull(process1.TestFiles);
                Assert.AreEqual(3, process1.TestFiles.Count());
                Assert.AreEqual("/dev/sdd1", process1.TestFiles.ElementAt(0));
                Assert.AreEqual("/dev/sde1", process1.TestFiles.ElementAt(1));
            }
        }

        [Test]
        public void DiskWorkloadExecutorCreatesTheExpectedProcessesInTheSingleProcessPerDriveModel_AllDisksScenario()
        {
            string expectedCommand = "/home/any/path/to/fio";
            IEnumerable<Disk> allDisks = this.SetupWorkloadScenario(
                testRemoteDisks: true,
                testOSDisk: true,
                processModel: "SingleProcessPerDisk");

            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<DiskWorkloadProcess> processes = workloadExecutor.CreateWorkloadProcesses(expectedCommand, this.mockCommandLine, allDisks, "SingleProcessPerDisk");

                Assert.AreEqual(4, processes.Count()); // one per unique disk categorization.

                // The OS disk processes
                DiskWorkloadProcess process1 = processes.ElementAt(0);
                Assert.AreEqual(expectedCommand, process1.Command);
                Assert.IsTrue(process1.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process1.TestFiles.Count());
                Assert.IsTrue(process1.TestFiles.First().StartsWith("/"));

                // The temp disk processes
                DiskWorkloadProcess process2 = processes.ElementAt(1);
                Assert.AreEqual(expectedCommand, process2.Command);
                Assert.IsTrue(process2.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process2.TestFiles.Count());
                Assert.AreEqual("/dev/sdd1", process2.TestFiles.ElementAt(0));

                // The remote disk 1 process
                DiskWorkloadProcess process3 = processes.ElementAt(2);
                Assert.AreEqual(expectedCommand, process3.Command);
                Assert.IsTrue(process3.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process3.TestFiles.Count());
                Assert.AreEqual("/dev/sde1", process3.TestFiles.ElementAt(0));

                // The remote disk 2 process
                DiskWorkloadProcess process4 = processes.ElementAt(3);
                Assert.AreEqual(expectedCommand, process4.Command);
                Assert.IsTrue(process4.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process4.TestFiles.Count());
                Assert.AreEqual("/dev/sdf1", process4.TestFiles.ElementAt(0));
            }
        }

        [Test]
        public void DiskWorkloadExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined_WindowsScenario()
        {
            // The default mock setups create 4 disks:
            // C:
            // D:
            // E:
            // F:
            //
            // We are looking for the first 2 when we define the TestDisks -> C,D
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Win32NT, true);

            this.profileParameters["DiskFilter"] = "DiskPath:C,D";
            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = workloadExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(2, disksToTest.Count());
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), disksToTest.ElementAt(0)));
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), disksToTest.ElementAt(1)));
            }
        }

        [Test]
        public void DiskWorkloadExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined_LinuxScenario()
        {
            // The default mock setups create 4 disks:
            // /dev/sdc
            // /dev/sdd
            // /dev/sde
            // /dev/sdf
            //
            // We are looking for the first 2 when we define the TestDisks -> /dev/sdd,/dev/sde
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.profileParameters["DiskFilter"] = "DiskPath:/dev/sdd,/dev/sde";
            using (TestDiskWorkloadExecutor workloadExecutor = new TestDiskWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = workloadExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(2, disksToTest.Count());
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), disksToTest.ElementAt(0)));
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(2), disksToTest.ElementAt(1)));
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

        private class TestDiskWorkloadExecutor : DiskWorkloadExecutor
        {
            public TestDiskWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
               : base(dependencies, parameters)
            {
            }

            public Func<string, string> OnGetTestFile { get; set; }

            public new IEnumerable<DiskWorkloadProcess> CreateWorkloadProcesses(string executable, string templateCommand, IEnumerable<Disk> disks, string processModel)
            {
                return base.CreateWorkloadProcesses(executable, templateCommand, disks, processModel);
            }

            public new IEnumerable<Disk> GetDisksToTest(IEnumerable<Disk> disks)
            {
                return base.GetDisksToTest(disks);
            }

            public new string GetTestFiles(string mountPoint)
            {
                return base.GetTestFiles(mountPoint);
            }

            protected override Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }

            protected override string GetTestFile(string mountPoint)
            {
                return this.OnGetTestFile?.Invoke(mountPoint) ?? Path.Combine(mountPoint, Path.GetRandomFileName());
            }

            protected override DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
            {
                return new DiskWorkloadProcess(
                    new InMemoryProcess()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = executable,
                            Arguments = commandArguments
                        }
                    },
                    testedInstance,
                    disksToTest.Select(disk => disk.GetPreferredAccessPath(this.Platform)).ToArray());
            }
        }
    }
}
