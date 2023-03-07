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
    public class HPLProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-HPL.json")]
        public void HPLWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPL.json")]
        public async Task HPLWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
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
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "arm_performance_libraries");
            }
        }

        [Test]
        [TestCase("PERF-CPU-HPL.json")]
        public async Task HPLWorkloadProfileExecutesTheExpectedWorkloadsOnUnixX64Platform(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters.Add("PackageName", "HPL");
            this.mockFixture.Parameters.Add("HPLVersion", "2.3");
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands();

            string[] expectedFiles = new string[]
            {
                @"setup/Make.Linux_GCC",
                @"Make.Linux_GCC",
                @"bin/Linux_GCC/HPL.dat"
            };

            this.mockFixture.SetupWorkloadPackage("hpl-2.3", expectedFiles: expectedFiles);
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
                        @"setup/Make.Linux_GCC",
                        @"Make.Linux_GCC",
                    };

                    this.mockFixture.SetupWorkloadPackage("hpl-2.3", expectedFiles: expectedFiles);
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
        [TestCase("PERF-CPU-HPL.json")]
        public async Task HPLWorkloadProfileExecutesTheExpectedWorkloadsOnUnixArm64Platform(string profile)
        {
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.Arm64);
            this.mockFixture.Parameters.Add("PackageName", "HPL");
            this.mockFixture.Parameters.Add("HPLVersion", "2.3");
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands();

            string[] expectedFiles = new string[]
            {
                @"setup/Make.Linux_GCC",
                @"Make.Linux_GCC",
                @"bin/Linux_GCC/HPL.dat"
            };

            this.mockFixture.SetupWorkloadPackage("armperformancelibraries", expectedFiles: @"arm-performance-libraries_22.1_Ubuntu-20.04.sh");
            this.mockFixture.SetupWorkloadPackage("hpl-2.3", expectedFiles: expectedFiles);
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
                        @"setup/Make.Linux_GCC",
                        @"Make.Linux_GCC",
                    };

                    this.mockFixture.SetupWorkloadPackage("hpl-2.3", expectedFiles: expectedFiles);
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
            if (this.mockFixture.CpuArchitecture == Architecture.X64)
            {
                return new List<string>
                {
                    $"wget http://www.netlib.org/benchmark/hpl/hpl-{this.mockFixture.Parameters["HPLVersion"]}.tar.gz -O {this.mockFixture.Parameters["PackageName"]}.tar.gz",
                    $"tar -zxvf {this.mockFixture.Parameters["PackageName"]}.tar.gz",
                    $"sudo bash -c \"source make_generic\"",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u azureuser -- mpirun --use-hwthread-cpus -np {Environment.ProcessorCount} ./xhpl"
                };
            }
            else
            {
                return new List<string>
                {
                    $"sudo ./arm-performance-libraries_22.1_Ubuntu-20.04.sh -a",
                    $"wget http://www.netlib.org/benchmark/hpl/hpl-{this.mockFixture.Parameters["HPLVersion"]}.tar.gz -O {this.mockFixture.Parameters["PackageName"]}.tar.gz",
                    $"tar -zxvf {this.mockFixture.Parameters["PackageName"]}.tar.gz",
                    $"sudo bash -c \"source make_generic\"",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u azureuser -- mpirun -np {Environment.ProcessorCount} ./xhpl"
                };
            }
        }
    }
}