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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HPLinpackExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(HPLinpackExecutorTests), "Examples", "HPLinpack");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private DependencyPath mockPerformanceLibrariesPackage;
        private string exampleResults;

        private void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockPackage = new DependencyPath("HPL", this.mockFixture.GetPackagePath("hplinpack"));
            this.mockPerformanceLibrariesPackage = new DependencyPath("hplperformancelibraries", this.mockFixture.GetPackagePath("hplperformancelibraries"));


            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.SetupPackage(this.mockPerformanceLibrariesPackage);

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

                Assert.AreEqual(workloadExpectedPath, executor.GetHPLDirectory);
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
                Exception exception = Assert.ThrowsAsync<Exception>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                StringAssert.Contains("NumberOfProcesses parameter value should be less than or equal to number of logical cores", exception.Message);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithNoPerformanceLibrariesOnUbuntuPlatform(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries23OnUbuntuArm64Platform(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "ARM";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = "23.04.1";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockPackage.Path, "ARM", "arm-performance-libraries_23.04.1.sh")}",
                    $"sudo ./arm-performance-libraries_23.04.1.sh -a",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries24OnUbuntuArm64Platform(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "ARM";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = "24.10";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockPackage.Path, "ARM", "arm-performance-libraries_24.10.sh")}",
                    $"sudo ./arm-performance-libraries_24.10.sh -a",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries25OnUbuntuArm64Platform(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "ARM";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = "25.04.1";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockPackage.Path, "ARM", "arm-performance-libraries_25.04.1.sh")}",
                    $"sudo ./arm-performance-libraries_25.04.1.sh -a",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "4.2.0")]
        [TestCase(PlatformID.Unix, Architecture.X64, "5.0.0")]
        [TestCase(PlatformID.Unix, Architecture.X64, "5.1.0")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibrariesOnUbuntuX64AMDPlatform(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
        {
            this.SetupTest(platform, architecture);
            this.mockFixture.Parameters["PerformanceLibrary"] = "AMD";
            this.mockFixture.Parameters["PerformanceLibraryVersion"] = $"{performanceLibraryVersion}";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.mockFixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "AMD", performanceLibraryVersion, "install.sh")}",
                    $"sudo ./install.sh -t {executor.GetHPLDirectory} -i lp64",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.mockFixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.mockFixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
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
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries25OnUbuntuX64IntelPlatform(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
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
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", $"{performanceLibraryVersion}", "intel-onemkl-2025.1.0.803_offline.sh")}",
                    $"sudo ./intel-onemkl-2025.1.0.803_offline.sh -a --silent --eula accept",
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", "2025.1.0.803", "intel-oneapi-hpc-toolkit-2025.1.3.10_offline.sh")}",
                    $"sudo ./intel-oneapi-hpc-toolkit-2025.1.3.10_offline.sh -a --silent --eula accept",
                    $"cp -r /opt/intel/oneapi/mkl/2025.1/share/mkl/benchmarks/mp_linpack {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", "2025.1.0.803")}",
                    $"make arch=Linux_GCC",
                    $"sudo bash -c \". /opt/intel/oneapi/mpi/latest/env/vars.sh && ./runme_intel64_dynamic\""
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"-c \". /opt/intel/oneapi/mpi/latest/env/vars.sh && ./runme_intel64_dynamic\"")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }
                                                                                                                            
                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "2024.2.2.17")]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries24OnUbuntuX64IntelPlatform(PlatformID platform, Architecture architecture, string performanceLibraryVersion)
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
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", $"{performanceLibraryVersion}", "l_onemkl_p_2024.2.2.17_offline.sh")}",
                    $"sudo ./l_onemkl_p_2024.2.2.17_offline.sh -a --silent --eula accept",
                    $"sudo chmod +x {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", "2024.2.2.17", "l_HPCKit_p_2024.2.1.79_offline.sh")}",
                    $"sudo ./l_HPCKit_p_2024.2.1.79_offline.sh -a --silent --eula accept",
                    $"cp -r /opt/intel/oneapi/mkl/2024.2/share/mkl/benchmarks/mp_linpack {this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.GetPackagePath("hplperformancelibraries"), "INTEL", "2024.2.2.17")}",
                    $"make arch=Linux_GCC",
                    $"sudo bash -c \". /opt/intel/oneapi/mpi/latest/env/vars.sh && ./runme_intel64_dynamic\""
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"-c \". /opt/intel/oneapi/mpi/latest/env/vars.sh && ./runme_intel64_dynamic\"")
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.exampleResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
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

            public string GetHPLDirectory => base.HPLDirectory;
        }
    }
}
