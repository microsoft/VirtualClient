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
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task HPLinpackExecutorExecutesWorkloadAsExpectedOnUbuntuPlatform(PlatformID platform, Architecture architecture)
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
                    $"sudo runuser -u {this.fixture.Parameters["Username"]} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl"
                };

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove(expectedCommands[0]);
                    if (arguments == $"runuser -u {this.fixture.Parameters["Username"]} -- mpirun --use-hwthread-cpus -np {this.fixture.Parameters["NumberOfProcesses"] ?? Environment.ProcessorCount} ./xhpl")
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
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void HPLinpackExecutorThrowsWhenPlatformIsNotSupported(PlatformID platformID, Architecture architecture)
        {
            this.fixture.Setup(platformID, architecture);
            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                if (platformID == PlatformID.Win32NT)
                {
                    Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
                }
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("HPL", currentDirectory);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);
            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\HPLinpack\HPLResults.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawString);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Username"] = "username",
                ["CompilerName"] = "gcc",
                ["CompilerVersion"] = "11",
                ["PackageName"] = "HPL",
                ["Version"] = "2.3",
                ["ProblemSizeN"] = "20000",
                ["BlockSizeNB"] = "256",
                ["Scenario"] = "ProcessorSpeed",
                ["NumberOfProcesses"] = "10"
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
