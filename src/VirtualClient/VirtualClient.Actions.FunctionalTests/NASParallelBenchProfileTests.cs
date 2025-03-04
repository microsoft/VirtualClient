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

                var expectedCommands = this.GetProfileExpectedCommands(platform, architecture);
                
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

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            List<string> commands = null;
            if (architecture == Architecture.X64)
            {
                commands = new List<string>
                {
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/bt.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/cg.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/ep.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/ft.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/is.C.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/lu.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/mg.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/sp.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/ua.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-x64/NPB-OMP/bin/dc.B.x\"",
                };
            }
            else
            {
                commands = new List<string>
                {
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/bt.D.x",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/cg.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/ep.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/ft.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/is.C.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/lu.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/mg.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/sp.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/ua.D.x\"",
                    $"bash -c \"export OMP_NUM_THREADS={Environment.ProcessorCount} && /home/user/tools/VirtualClient/packages/nasparallelbench/linux-arm64/NPB-OMP/bin/dc.B.x\"",
                };
            }

            return commands;
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);
            string[] expectedFiles = null;

            if (architecture == Architecture.X64)
            {
                expectedFiles = new string[]
                {
                        @"linux-x64/NPB-OMP/bin/bt.D.x",
                        @"linux-x64/NPB-OMP/bin/cg.D.x",
                        @"linux-x64/NPB-OMP/bin/ep.D.x",
                        @"linux-x64/NPB-OMP/bin/ft.D.x",
                        @"linux-x64/NPB-OMP/bin/is.C.x",
                        @"linux-x64/NPB-OMP/bin/lu.D.x",
                        @"linux-x64/NPB-OMP/bin/mg.D.x",
                        @"linux-x64/NPB-OMP/bin/sp.D.x",
                        @"linux-x64/NPB-OMP/bin/ua.D.x",
                        @"linux-x64/NPB-OMP/bin/dc.B.x",
                        @"linux-x64/NPB-OMP/bin/dt.D.x"
                };
            }
            else
            {
                expectedFiles = new string[]
                {
                        @"linux-arm64/NPB-OMP/bin/bt.D.x",
                        @"linux-arm64/NPB-OMP/bin/cg.D.x",
                        @"linux-arm64/NPB-OMP/bin/ep.D.x",
                        @"linux-arm64/NPB-OMP/bin/ft.D.x",
                        @"linux-arm64/NPB-OMP/bin/is.C.x",
                        @"linux-arm64/NPB-OMP/bin/lu.D.x",
                        @"linux-arm64/NPB-OMP/bin/mg.D.x",
                        @"linux-arm64/NPB-OMP/bin/sp.D.x",
                        @"linux-arm64/NPB-OMP/bin/ua.D.x",
                        @"linux-arm64/NPB-OMP/bin/dc.B.x",
                        @"linux-arm64/NPB-OMP/bin/dt.D.x"
                };
            }

            this.mockFixture.SetupPackage("nasparallelbench", expectedFiles: expectedFiles);
        }
    }
}
