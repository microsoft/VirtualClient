// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class CoreMarkProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-CPU-COREMARK.json")]
        public void CoreMarkWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-COREMARKPRO.json")]
        public void CoreMarkProWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-CPU-COREMARK.json")]
        public async Task CoreMarkWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $@"make XCFLAGS=""-DMULTITHREAD={Environment.ProcessorCount} -DUSE_PTHREAD"" REBUILD=1 LFLAGS_END=-pthread",
            };

            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, false));
            this.mockFixture.SetupLinuxPackagesInstalled(new Dictionary<string, string>
            {
                { "gcc", "10" }, // Should match profile defaults.
                { "cc", "10" }
            });

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("XCFLAGS", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Coremark.txt"));
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
        [TestCase("PERF-CPU-COREMARK.json")]
        public async Task CoreMarkWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $@"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremark; make XCFLAGS=""-DMULTITHREAD={Environment.ProcessorCount} -DUSE_PTHREAD"" REBUILD=1 LFLAGS_END=-pthread'",
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, false));
            DependencyPath mockPackage = new DependencyPath("cygwin", this.mockFixture.PlatformSpecifics.GetPackagePath("cygwin"));

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("XCFLAGS", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_Coremark.txt"));
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
        [TestCase("PERF-CPU-COREMARKPRO.json")]
        public async Task CoreMarkProWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $@"make TARGET=linux64 XCMD='-c{Environment.ProcessorCount}' certify-all"
            };

            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, false));
            this.mockFixture.SetupLinuxPackagesInstalled(new Dictionary<string, string>
            {
                { "gcc", "10" }, // Should match profile defaults.
                { "cc", "10" }
            });

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("certify-all", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_CoremarkPro.txt"));
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
        [TestCase("PERF-CPU-COREMARKPRO.json")]
        public async Task CoreMarkProWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = new List<string>
            {
                $@"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremarkpro; make TARGET=linux64 XCMD='-c{Environment.ProcessorCount}' certify-all'"
            };

            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, Environment.ProcessorCount, 9, 10, false));
            this.mockFixture.SetupLinuxPackagesInstalled(new Dictionary<string, string>
            {
                { "gcc", "10" }, // Should match profile defaults.
                { "cc", "10" },
                { "gfortran", "10" }
            });

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("certify-all", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_CoremarkPro.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }
    }
}
