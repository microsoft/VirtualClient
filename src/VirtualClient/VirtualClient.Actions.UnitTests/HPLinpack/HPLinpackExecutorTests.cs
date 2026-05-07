// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using Org.BouncyCastle.Tls.Crypto;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HPLinpackExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(HPLinpackExecutorTests), "Examples", "HPLinpack");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private DependencyPath mockPerformanceLibraryPackage;
        private string exampleResults;

        private void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);

            this.mockPackage = new DependencyPath("HPL", this.mockFixture.GetPackagePath("hplinpack"));
            this.mockPerformanceLibraryPackage = new DependencyPath("hplperformancelibraries", this.mockFixture.GetPackagePath("hplperformancelibraries"));

            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.SetupPackage(this.mockPerformanceLibraryPackage);

            this.mockFixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            this.exampleResults = File.ReadAllText(this.mockFixture.Combine(HPLinpackExecutorTests.ExamplesDirectory, "HPLResultsArm.txt"));
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.exampleResults);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
            this.mockFixture.Process.StandardOutput.Append(this.exampleResults);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1000 * 1024 * 1024));

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, true));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["CompilerName"] = "gcc",
                ["CompilerVersion"] = "11",
                ["PackageName"] = "HPL",
                ["ProblemSizeN"] = "20000",
                ["BlockSizeNB"] = "256",
                ["Scenario"] = "ProcessorSpeed",
                ["NumberOfProcesses"] = "2"
            };
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string workloadExpectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path;

                Assert.AreEqual(workloadExpectedPath, executor.HPLinpackPackagePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void HPLinpackExecutorThrowsOnValidateParametersFailing(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["NumberOfProcesses"] = 100;
            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(
                    $"The 'NumberOfProcesses' parameter value should be less than or equal to number of logical processors on the system.",
                    exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithNoPerformanceLibrariesOnUnixPlatform(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            string numProcesses = (this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount).ToString();

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod -R 2777 \"/home/user/tools/VirtualClient/packages/hplinpack/{this.mockFixture.PlatformArchitectureName}\"",
                    $"bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {numProcesses} --allow-run-as-root --bind-to core ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if ($"{command} {arguments}" == expectedCommands[0])
                    {
                        expectedCommands.RemoveAt(0);
                    }
                    
                    if (arguments.StartsWith($"runuser -u {Environment.UserName}"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "23.04.1", "arm-performance-libraries_23.04.1.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "24.10", "arm-performance-libraries_24.10.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "25.04.1", "arm-performance-libraries_25.04.1.sh")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithArmPerformanceLibraries(PlatformID platform, Architecture architecture, string performanceLibraryVersion, string performanceLibraryScript)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "ARM";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = performanceLibraryVersion;
            string numProcesses = (this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount).ToString();

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-arm64\"",
                    $"sudo chmod -R 2777 \"{this.mockPerformanceLibraryPackage.Path}\"",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-arm64/{performanceLibraryScript} -a",
                    $"bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockPackage.Path}/linux-arm64/setup/Make.Linux_GCC Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {numProcesses} --allow-run-as-root --bind-to core ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if ($"{command} {arguments}" == expectedCommands[0])
                    {
                        expectedCommands.RemoveAt(0);
                    }

                    if (arguments.StartsWith($"runuser -u {Environment.UserName}"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "4.2.0")]
        [TestCase(PlatformID.Unix, Architecture.X64, "5.0.0")]
        [TestCase(PlatformID.Unix, Architecture.X64, "5.1.0")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithAmdPerformanceLibraries(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "AMD";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = $"{performanceLibraryVersion}";
            string numProcesses = (this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount).ToString();

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-x64\"",
                    $"sudo chmod -R 2777 \"{this.mockPerformanceLibraryPackage.Path}\"",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-x64/install.sh -t {this.mockPackage.Path}/linux-x64 -i lp64",
                    $"bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockPackage.Path}/linux-x64/setup/Make.Linux_GCC Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {numProcesses} --allow-run-as-root --bind-to core ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if ($"{command} {arguments}" == expectedCommands[0])
                    {
                        expectedCommands.RemoveAt(0);
                    }

                    if (arguments.StartsWith($"runuser -u {Environment.UserName}"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }


        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "1.0.0")]
        public void HPLinpackExecutorThrowsExceptionForUnsupportedAMDPerformanceLibraryVersions(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "AMD";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = performanceLibraryVersion;

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(
                    $"The HPL workload currently only supports 4.2.0, 5.0.0 and 5.1.0 versions of AMD performance libraries",
                    exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "2025.1.0.803")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithIntelPerformanceLibraries_2025_1_0_803(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "INTEL";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = $"{performanceLibraryVersion}";
            this.mockFixture.PlatformSpecifics.EnvironmentVariables.Add("HOME", "/home/user");

            // Setup CPU info with socket count for Intel execution path
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 2, 9, 11, 13, true));

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-x64\"",
                    $"sudo chmod -R 2777 \"{this.mockPerformanceLibraryPackage.Path}\"",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-x64/intel-onemkl-2025.1.0.803_offline.sh -a --silent --eula accept",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-x64/intel-oneapi-hpc-toolkit-2025.1.3.10_offline.sh -a --silent --eula accept",
                    $"bash -c \"source make_generic\"",
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-x64/setup\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockPackage.Path}/linux-x64/setup/Make.Linux_GCC Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo bash -c \"/home/user/intel/oneapi/mpi/latest/env/vars.sh && {this.mockPerformanceLibraryPackage.Path}/linux-x64/mp_linpack/runme_intel64_dynamic\""
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if ($"{command} {arguments}" == expectedCommands[0])
                    {
                        expectedCommands.RemoveAt(0);
                    }

                    if (arguments.Contains("vars.sh"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }
                                                                                                                            
                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "2024.2.2.17")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithIntelPerformanceLibraries_2024_2_2_17(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "INTEL";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = $"{performanceLibraryVersion}";

            // Setup CPU info with socket count for Intel execution path
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 2, 9, 11, 13, true));

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-x64\"",
                    $"sudo chmod -R 2777 \"{this.mockPerformanceLibraryPackage.Path}\"",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-x64/l_onemkl_p_2024.2.2.17_offline.sh -a --silent --eula accept",
                    $"sudo {this.mockPerformanceLibraryPackage.Path}/linux-x64/l_HPCKit_p_2024.2.1.79_offline.sh -a --silent --eula accept",
                    $"bash -c \"source make_generic\"",          
                    $"sudo chmod -R 2777 \"{this.mockPackage.Path}/linux-x64/setup\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockPackage.Path}/linux-x64/setup/Make.Linux_GCC Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo bash -c \"/opt/intel/oneapi/mpi/latest/env/vars.sh && {this.mockPerformanceLibraryPackage.Path}/linux-x64/mp_linpack/runme_intel64_dynamic\""
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if ($"{command} {arguments}" == expectedCommands[0])
                    {
                        expectedCommands.RemoveAt(0);
                    }

                    if (arguments.Contains("vars.sh"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "1.0.0")]
        public void HPLinpackExecutorThrowsExceptionForUnsupportedIntelPerformanceLibraryVersions(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "INTEL";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = performanceLibraryVersion;

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(
                    $"The HPL workload currently only supports 2024.2.2.17 and 2025.1.0.803 versions of INTEL Math Kernel Library",
                    exception.Message);
            }
        }

        private class TestHPLExecutor : HPLinpackExecutor
        {
            public TestHPLExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new string HPLinpackPackagePath => base.HPLinpackPackagePath;
        }
    }
}
