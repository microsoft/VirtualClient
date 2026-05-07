// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class NASParallelBenchProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public void NASParallelWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task NASParallelWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "nasparallelbench");
            }
        }

        [Test]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task NASParallelWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.SetupDefaultMockBehavior(platform, architecture);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // The profile uses {calculate({LogicalCoreCount} - 2)} for ThreadCount.
                // Read the mock CpuInfo to derive the expected value dynamically.
                CpuInfo cpuInfo = await this.mockFixture.SystemManagement.Object
                    .GetCpuInfoAsync(CancellationToken.None).ConfigureAwait(false);

                int expectedThreadCount = cpuInfo.LogicalProcessorCount - 2;
                var expectedCommands = this.GetProfileExpectedCommands(expectedThreadCount, platform, architecture);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-HPC-NASPARALLELBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public void NASParallelWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            // We ensure the workload package does not exist.

            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(int expectedThreadCount, PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            string platformDir = architecture == Architecture.X64 ? "linux-x64" : "linux-arm64";

            List<string> commands = new List<string>
            {
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/bt.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/cg.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/ep.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/ft.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/is.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/lu.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/mg.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/sp.C.x\"",
                $"bash -c \"export OMP_NUM_THREADS={expectedThreadCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/{platformDir}/NPB-OMP/bin/ua.C.x\"",
            };

            return commands;
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            string platformDir = architecture == Architecture.X64 ? "linux-x64" : "linux-arm64";

            string[] expectedFiles = new string[]
            {
                $@"{platformDir}/NPB-OMP/bin/bt.C.x",
                $@"{platformDir}/NPB-OMP/bin/cg.C.x",
                $@"{platformDir}/NPB-OMP/bin/ep.C.x",
                $@"{platformDir}/NPB-OMP/bin/ft.C.x",
                $@"{platformDir}/NPB-OMP/bin/is.C.x",
                $@"{platformDir}/NPB-OMP/bin/lu.C.x",
                $@"{platformDir}/NPB-OMP/bin/mg.C.x",
                $@"{platformDir}/NPB-OMP/bin/sp.C.x",
                $@"{platformDir}/NPB-OMP/bin/ua.C.x",
                $@"{platformDir}/NPB-OMP/bin/dt.C.x"
            };

            this.mockFixture.SetupPackage("nasparallelbench", expectedFiles: expectedFiles);
        }
    }
}
