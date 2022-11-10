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
    public class DiskPerformanceWorkloadExecutorTests
    {
        private static string dllPath = Path.GetDirectoryName(Assembly.GetAssembly(typeof(DiskPerformanceWorkloadExecutorTests)).Location);

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
                { nameof(DiskPerformanceWorkloadExecutor.DeleteTestFilesOnFinish), "true" }
            };

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
        }

        [Test]
        public void IOWorkloadExecutorSelectsTheExpectedDisksForTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => !disk.IsOperatingSystem());

            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.AccessPaths.First()), actualDisks.Select(d => d.AccessPaths.First()));
            }
        }

        [Test]
        public void IOWorkloadExecutorSelectsTheExpectedDisksForTest_OSDiskScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks.Where(disk => disk.IsOperatingSystem());
            this.profileParameters[nameof(DiskPerformanceWorkloadExecutor.DiskFilter)] = "osdisk";

            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.AccessPaths.First()), actualDisks.Select(d => d.AccessPaths.First()));
            }
        }

        [Test]
        public void IOWorkloadExecutorSelectsTheExpectedDisksForTest_AllDisksScenario()
        {
            IEnumerable<Disk> expectedDisks = this.disks;
            this.profileParameters[nameof(DiskPerformanceWorkloadExecutor.DiskFilter)] = "none";
            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> actualDisks = workloadExecutor.GetDisksToTest(this.disks);
                CollectionAssert.AreEquivalent(expectedDisks.Select(d => d.AccessPaths.First()), actualDisks.Select(d => d.AccessPaths.First()));
            }
        }

        [Test]
        public void IOWorkloadExecutorCreatesTheExpectedProcessesInTheSingleProcessModel_RemoteDiskScenario()
        {
            string expectedCommand = "/home/any/path/to/fio";
            IEnumerable<Disk> remoteDisks = this.SetupWorkloadScenario(testRemoteDisks: true);

            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<DiskPerformanceWorkloadProcess> processes = workloadExecutor.CreateWorkloadProcesses(expectedCommand, this.mockCommandLine, remoteDisks, "SingleProcess");

                Assert.IsNotNull(processes);
                Assert.IsNotEmpty(processes);
                Assert.AreEqual(1, processes.Count());

                // The remote disk 1 and 2 process
                DiskPerformanceWorkloadProcess process1 = processes.First();
                Assert.AreEqual(expectedCommand, process1.Command);
                Assert.IsTrue(process1.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.IsNotNull(process1.TestFiles);
                Assert.AreEqual(3, process1.TestFiles.Count());
                Assert.AreEqual("/dev/sdd1", process1.TestFiles.ElementAt(0));
                Assert.AreEqual("/dev/sde2", process1.TestFiles.ElementAt(1));
            }
        }

        [Test]
        public void IOWorkloadExecutorCreatesTheExpectedProcessesInTheSingleProcessPerDriveModel_AllDisksScenario()
        {
            string expectedCommand = "/home/any/path/to/fio";
            IEnumerable<Disk> allDisks = this.SetupWorkloadScenario(
                testRemoteDisks: true,
                testOSDisk: true,
                processModel: "SingleProcessPerDisk");

            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<DiskPerformanceWorkloadProcess> processes = workloadExecutor.CreateWorkloadProcesses(expectedCommand, this.mockCommandLine, allDisks, "SingleProcessPerDisk");

                Assert.AreEqual(4, processes.Count()); // one per unique disk categorization.

                // The OS disk processes
                DiskPerformanceWorkloadProcess process1 = processes.ElementAt(0);
                Assert.AreEqual(expectedCommand, process1.Command);
                Assert.IsTrue(process1.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process1.TestFiles.Count());
                Assert.IsTrue(process1.TestFiles.First().StartsWith("/"));

                // The temp disk processes
                DiskPerformanceWorkloadProcess process2 = processes.ElementAt(1);
                Assert.AreEqual(expectedCommand, process2.Command);
                Assert.IsTrue(process2.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process2.TestFiles.Count());
                Assert.AreEqual("/dev/sdd1", process2.TestFiles.ElementAt(0));

                // The remote disk 1 process
                DiskPerformanceWorkloadProcess process3 = processes.ElementAt(2);
                Assert.AreEqual(expectedCommand, process3.Command);
                Assert.IsTrue(process3.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process3.TestFiles.Count());
                Assert.AreEqual("/dev/sde2", process3.TestFiles.ElementAt(0));

                // The remote disk 2 process
                DiskPerformanceWorkloadProcess process4 = processes.ElementAt(3);
                Assert.AreEqual(expectedCommand, process4.Command);
                Assert.IsTrue(process4.CommandArguments.StartsWith(this.mockCommandLine));

                Assert.AreEqual(1, process4.TestFiles.Count());
                Assert.AreEqual("/dev/sdf3", process4.TestFiles.ElementAt(0));
            }
        }

        [Test]
        public async Task IOWorkloadExecutorCreatesExpectedMountPointsForDisksUnderTest_RemoteDiskScenario()
        {
            IEnumerable<Disk> remoteDisks = this.SetupWorkloadScenario(testRemoteDisks: true);

            // Clear any access points out.
            remoteDisks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));

            List<Tuple<DiskVolume, string>> mountPointsCreated = new List<Tuple<DiskVolume, string>>();

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.CreateMountPointAsync(It.IsAny<DiskVolume>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<DiskVolume, string, CancellationToken>((volume, mountPoint, token) =>
                {
                    mountPointsCreated.Add(new Tuple<DiskVolume, string>(volume, mountPoint));
                })
                .Returns(Task.CompletedTask);

            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                await workloadExecutor.CreateMountPointsAsync(remoteDisks, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotEmpty(mountPointsCreated);
                Assert.AreEqual(3, mountPointsCreated.Count);
                Assert.IsTrue(object.ReferenceEquals(mountPointsCreated.ElementAt(0).Item1, remoteDisks.ElementAt(0).Volumes.First()));
                Assert.IsTrue(object.ReferenceEquals(mountPointsCreated.ElementAt(1).Item1, remoteDisks.ElementAt(1).Volumes.First()));
                Assert.IsTrue(object.ReferenceEquals(mountPointsCreated.ElementAt(2).Item1, remoteDisks.ElementAt(2).Volumes.First()));

                string expectedMountPoint1 = Path.Combine(DiskPerformanceWorkloadExecutorTests.dllPath, "vcmnt_dev_sdd1");
                Assert.AreEqual(expectedMountPoint1, mountPointsCreated.ElementAt(0).Item2);

                string expectedMountPoint2 = Path.Combine(DiskPerformanceWorkloadExecutorTests.dllPath, "vcmnt_dev_sdd2");
                Assert.AreEqual(expectedMountPoint2, mountPointsCreated.ElementAt(1).Item2);
            }
        }

        [Test]
        public void IOWorkloadExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined_WindowsScenario()
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
            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
            {
                IEnumerable<Disk> disksToTest = workloadExecutor.GetDisksToTest(this.disks);

                Assert.IsNotNull(disksToTest);
                Assert.AreEqual(2, disksToTest.Count());
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(0), disksToTest.ElementAt(0)));
                Assert.IsTrue(object.ReferenceEquals(this.disks.ElementAt(1), disksToTest.ElementAt(1)));
            }
        }

        [Test]
        public void IOWorkloadExecutorIdentifiesTheExpectedDisksWhenTheyAreExplicitlyDefined_LinuxScenario()
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
            using (TestIOWorkloadExecutor workloadExecutor = new TestIOWorkloadExecutor(this.mockFixture.Dependencies, this.profileParameters))
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

        private class TestIOWorkloadExecutor : DiskPerformanceWorkloadExecutor
        {
            public TestIOWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
               : base(dependencies, parameters)
            {
            }

            public Func<string, string> OnGetTestFile { get; set; }

            public new IEnumerable<DiskPerformanceWorkloadProcess> CreateWorkloadProcesses(string executable, string templateCommand, IEnumerable<Disk> disks, string processModel)
            {
                return base.CreateWorkloadProcesses(executable, templateCommand, disks, processModel);
            }

            public new IEnumerable<Disk> GetDisksToTest(IEnumerable<Disk> disks)
            {
                return base.GetDisksToTest(disks);
            }

            public new Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
            {
                return base.CreateMountPointsAsync(disks, cancellationToken);
            }

            protected override string GetTestFile(string mountPoint)
            {
                return this.OnGetTestFile?.Invoke(mountPoint) ?? Path.Combine(mountPoint, Path.GetRandomFileName());
            }

            protected override DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
            {
                return new DiskPerformanceWorkloadProcess(
                    new InMemoryProcess()
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = executable,
                            Arguments = commandArguments
                        }
                    },
                    testedInstance,
                    disksToTest.Select(disk => disk.AccessPaths.FirstOrDefault()).ToArray());
            }
        }
    }
}
