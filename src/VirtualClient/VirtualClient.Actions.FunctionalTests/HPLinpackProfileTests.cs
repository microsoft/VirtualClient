// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class HPLinpackProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-HPLINPACK.json")]
        public void HPLinpackWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPLINPACK.json")]
        public async Task HPLinpackWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "hplinpack");
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPLINPACK.json")]
        public async Task HPLinpackWorkloadProfileWithOutPerformanceLibrariesExecutesExpectedCommandsOnUnixX64PlatformAsync(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands();

            string[] expectedFiles = new string[]
            {
                @"linux-x64/setup/Make.Linux_GCC",
                @"linux-x64/Make.Linux_GCC",
                @"linux-x64/bin/Linux_GCC/HPL.dat"
            };

            this.mockFixture.SetupWorkloadPackage("hpl.2.3", expectedFiles: expectedFiles);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1000 * 1024 * 1024));
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, true));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("mpirun", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HPL.txt"));
                }

                if (arguments == "--version")
                {
                    process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                    process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                }

                if (arguments.Contains("source make_generic"))
                {
                    expectedFiles = new string[]
                    {
                        @"linux-x64/setup/Make.Linux_GCC",
                        @"linux-x64/Make.Linux_GCC",
                    };

                    this.mockFixture.SetupWorkloadPackage("hpl.2.3", expectedFiles: expectedFiles);
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPLINPACK.json")]
        public async Task HPLinpackWorkloadProfileExecutesThxeExpectedWorkloadsOnUnixArm64Platform(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.Arm64);
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestOS",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };

            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1000 * 1024 * 1024));
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, true));
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands();

            string[] expectedFiles = new string[]
            {
                @"linux-arm64/setup/Make.Linux_GCC",
                @"linux-arm64/Make.Linux_GCC",
                @"linux-arm64/bin/Linux_GCC/HPL.dat"
            };

            this.mockFixture.SetupWorkloadPackage("hpl.2.3", expectedFiles: expectedFiles);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("mpirun", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_HPL.txt"));
                }

                if (arguments == "--version")
                {
                    process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                    process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 11");
                }

                if (arguments.Contains("source make_generic"))
                {
                    expectedFiles = new string[]
                    {
                        @"linux-arm64/setup/Make.Linux_GCC",
                        @"linux-arm64/Make.Linux_GCC",
                    };

                    this.mockFixture.SetupWorkloadPackage("hpl.2.3", expectedFiles: expectedFiles);
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["CompilerName"] = "gcc",
                ["CompilerVersion"] = "11",
                ["PackageName"] = "HPL",
                ["ProblemSizeN"] = "20000",
                ["BlockSizeNB"] = "256",
                ["Scenario"] = "ProcessorSpeed",
                ["NumberOfProcesses"] = "2",
                ["BindToCores"] = false
            };

            return new List<string>
                {
                    $"sudo bash -c \"source make_generic\"",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {Environment.ProcessorCount} --allow-run-as-root ./xhpl"
                };
        }
    }
}