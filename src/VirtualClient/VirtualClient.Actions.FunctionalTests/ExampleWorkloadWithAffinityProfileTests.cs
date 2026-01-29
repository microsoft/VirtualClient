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
    public class ExampleWorkloadWithAffinityProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Unix, Architecture.Arm64)]
        public void ExampleWorkloadProfileParametersAreInlinedCorrectly_Linux(string profile, PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Win32NT, Architecture.Arm64)]
        public void ExampleWorkloadProfileParametersAreInlinedCorrectly_Windows(string profile, PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Unix)]
        public async Task ExampleWorkloadProfileInstallsTheExpectedDependenciesOnLinuxPlatform(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package should be installed
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "exampleworkload");
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Win32NT)]
        public async Task ExampleWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatform(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package should be installed
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "exampleworkload");
            }
        }

        [Test]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Unix)]
        public async Task ExampleWorkloadProfileExecutesTheExpectedWorkloadWithAffinityOnLinux(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.SetupPackage("exampleworkload", expectedFiles: "linux-x64/ExampleWorkload");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (command.Contains("bash") && arguments.Contains("numactl"))
                {
                    // Verify numactl wrapper is used for CPU affinity on Linux
                    process.StandardOutput.Append("{ \"metric1\": 100, \"metric2\": 200 }");
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Verify numactl was used for CPU affinity
                WorkloadAssert.CommandsExecuted(this.mockFixture, "\"numactl -C");
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json", PlatformID.Win32NT)]
        public async Task ExampleWorkloadProfileExecutesTheExpectedWorkloadWithAffinityOnWindows(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.mockFixture.SetupPackage("exampleworkload", expectedFiles: "win-x64/ExampleWorkload.exe");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                InMemoryProcess process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (command.Contains("ExampleWorkload"))
                {
                    // Set HasExited to false initially, true after "wait"
                    bool hasExited = false;
                    process.OnHasExited = () => hasExited;
                    process.OnStart = () =>
                    {
                        hasExited = false;
                        return true;
                    };
                    
                    process.OnApplyAffinity = (mask) =>
                    {
                        // Verify affinity mask is set while process is running
                        Assert.IsFalse(hasExited, "Affinity should be applied while process is running");
                        Assert.Greater(mask.ToInt64(), 0);
                    };
                    
                    // Simulate process completion when WaitForExitAsync is called
                    Task originalWait = process.WaitForExitAsync(CancellationToken.None);
                    process.StandardOutput.Append("{ \"metric1\": 100, \"metric2\": 200 }");
                    hasExited = true;
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Verify the process was executed
                WorkloadAssert.CommandsExecuted(this.mockFixture, "ExampleWorkload");
            }
        }

        [Test]
        [TestCase("PERF-CPU-EXAMPLE-AFFINITY.json")]
        public void ExampleWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            
            // Ensure the workload package does not exist
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }
    }
}
