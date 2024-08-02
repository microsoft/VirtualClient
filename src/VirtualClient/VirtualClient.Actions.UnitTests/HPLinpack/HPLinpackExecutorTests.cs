// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Contracts;
    using System.Runtime.InteropServices;
    using System.IO;
    using System.Reflection;
    using Moq;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using System.CodeDom.Compiler;
    using Microsoft.Azure.Amqp.Framing;

    [TestFixture]
    [Category("Unit")]
    public class HPLinpackExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;

        private string resultsPath;
        private string rawString;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.mockPath = this.fixture.Create<DependencyPath>();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.fixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string workloadExpectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platform, architecture).Path;

                Assert.AreEqual(workloadExpectedPath, executor.GetHPLDirectory);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void HPLinpackExecutorThrowsOnValidateParametersFailing(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.Parameters["NumberOfProcesses"] = 100;
            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
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
            this.SetupDefaultMockBehavior(platform, architecture);

            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.fixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.fixture.Process.StandardOutput.Append(this.rawString);
                    }

                    return this.fixture.Process;
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
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.Parameters["PerformanceLibrary"] = "ARM";
            this.fixture.Parameters["PerformanceLibraryVersion"] = "23.04.1";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "ARM", "arm-performance-libraries_23.04.1.sh")}",
                    $"sudo ./arm-performance-libraries_23.04.1.sh -a",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.fixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.fixture.Process.StandardOutput.Append(this.rawString);
                    }

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedWithPerformanceLibraries24OnUbuntuArm64Platform(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.Parameters["PerformanceLibrary"] = "ARM";
            this.fixture.Parameters["PerformanceLibraryVersion"] = "24.04";

            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                List<string> expectedCommands = new List<string>()
                {
                    $"sudo chmod +x {this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "ARM", "arm-performance-libraries_24.04.sh")}",
                    $"sudo ./arm-performance-libraries_24.04.sh -a",
                    $"sudo bash -c \"source make_generic\"",
                    $"mv Make.UNKNOWN Make.Linux_GCC",
                    $"ln -s {this.fixture.PlatformSpecifics.Combine(executor.GetHPLDirectory, "setup", "Make.Linux_GCC" )} Make.Linux_GCC",
                    $"make arch=Linux_GCC",
                    $"sudo runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {Environment.UserName} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
                    {
                        this.fixture.Process.StandardOutput.Append(this.rawString);
                    }

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(expectedCommands.Count, 0);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("HPL", currentDirectory);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);
            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, "Examples", "HPLinpack", "HPLResults.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawString);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
            this.fixture.Process.StandardOutput.Append(this.rawString);
            this.fixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1000 * 1024 * 1024));
            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, true));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
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
