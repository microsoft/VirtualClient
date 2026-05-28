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
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class StreamProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MEM-STREAMTRIAD.json")]
        public void StreamTriadWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAMTRIAD.json")]
        public async Task StreamTriadWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = StreamProfileTests.GetStreamTriadProfileExpectedCommands();

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, true));

            this.mockFixture.SetupPackage("stream", expectedFiles: "linux-x64/stream");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("Stream", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Stream.txt"));
                }
                else if (arguments.Contains("lscpu | grep 'Flags'"))
                {
                    process.StandardOutput.AppendLine("Flags: fpu vme de pse tsc msr pae mce cx8 apic sep mtrr pge mca cmov pat pse36 clflush mmx fxsr sse sse2 ss ht syscall nx pdpe1gb rdtscp lm constant_tsc rep_good nopl cpuid tsc_known_freq pni pclmulqdq ssse3 fma cx16 pcid sse4_1 sse4_2 x2apic movbe popcnt aes xsave avx f16c rdrand hypervisor lahf_lm abm 3dnowprefetch fsgsbase tsc_adjust bmi1 avx2 smep bmi2 erms invpcid avx512f avx512dq rdseed adx smap avx512ifma clflushopt clwb avx512cd sha_ni avx512bw avx512vl xsaveopt xsavec xgetbv1 xsaves avx512vbmi");
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [Ignore("We need to rethink how to do dependency testing with extension model.")]
        [TestCase("PERF-MEM-STREAMTRIAD.json")]
        public async Task StreamTriadWorkloadProfileInstallsTheExpectedDependenciesOnUnixPlatform(string profile)
        {
            // The setup in a typical Azure VM scenario
            this.mockFixture.Setup(PlatformID.Unix);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "stream");
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAMTRIAD.json")]
        public void StreamTriadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, true));

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAMMSFT.json")]
        public void StreamMsftWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAMMSFT.json")]
        public async Task StreamMsftWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = StreamProfileTests.GetStreamMsftProfileExpectedCommands();

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix, Architecture.Arm64);
            this.mockFixture.SetupPackage("streammsft", expectedFiles: "linux-arm64/stream");

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, true));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("perfrunner", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_StreamMsft.txt"));
                }
                else if (arguments.Contains("make", StringComparison.OrdinalIgnoreCase))
                {
                    // Make command should succeed without output
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAM.json")]
        public void StreamWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAM.json")]
        public async Task StreamWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = StreamProfileTests.GetStreamProfileExpectedCommands();

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupPackage("stream", expectedFiles: "linux-x64/stream");
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, true));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("streamworkload", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Stream.txt"));
                }
                else if (arguments.Contains("gcc", StringComparison.OrdinalIgnoreCase))
                {
                    // Compilation command - no output needed
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-MEM-STREAM.json")]
        public void StreamProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, true));

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(exe, arguments, workingDir);
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        private static IEnumerable<string> GetStreamTriadProfileExpectedCommands()
        {
            return new List<string>
            {
                "bash -c \"lscpu \\| grep 'Flags'\"",
                "bash -c \"export KMP_AFFINITY=.*&& export OMP_NUM_THREADS=.*&& export LD_LIBRARY_PATH=.*&& chmod \\+x.*&&.*Stream.*\""
            };
        }

        private static IEnumerable<string> GetStreamProfileExpectedCommands()
        {
            return new List<string>
            {
                "bash -c \"gcc.*stream\\.c.*-o.*streamworkload.*\"",
                "bash -c \"export OMP_NUM_THREADS=.*&&.*chmod.*\\+x.*streamworkload.*&&.*streamworkload.*\"",
            };
        }

        private static IEnumerable<string> GetStreamMsftProfileExpectedCommands()
        {
            return new List<string>
            {
                "bash.*make",
                "bash.*perfrunner.*--threads.*--internal-iter",
            };
        }

        private static IEnumerable<string> GetStreamWindowsProfileExpectedCommands()
        {
            return new List<string>
            {
                "cmd\\.exe.*stream\\.exe.*-n 50.*-s 320000000",
            };
        }
    }
}