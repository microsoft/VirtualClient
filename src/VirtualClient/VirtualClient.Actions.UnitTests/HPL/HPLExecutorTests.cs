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

    [TestFixture]
    [Category("Unit")]
    public class HPLExecutorTests
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
        public async Task HPLExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
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

                string workloadExpectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.fixture.PlatformSpecifics.GetPackagePath(), $"hpl-{this.fixture.Parameters["HPLVersion"]}");

                Assert.AreEqual(workloadExpectedPath, executor.HPLDirectory);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task HPLExecutorExecutesWorkloadAsExpectedOnUbuntuArmPlatform(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            List<string> expectedCommandsOnLinuxarm64 = new List<string>()
            {
                $"sudo ./arm-performance-libraries_22.1_Ubuntu-20.04.sh -a",
                $"wget http://www.netlib.org/benchmark/hpl/hpl-{this.fixture.Parameters["HPLVersion"]}.tar.gz -O {this.fixture.Parameters["PackageName"]}.tar.gz",
                $"tar -zxvf {this.fixture.Parameters["PackageName"]}.tar.gz",
                $"sudo bash -c \"source make_generic\"",
                $"make arch=Linux_GCC",
                $"sudo runuser -u azureuser -- mpirun -np {Environment.ProcessorCount} ./xhpl"
            };

            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                int executedArmCommands = 0;

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommandsOnLinuxarm64.Any(c => c == $"{command} {arguments}"))
                    {
                        executedArmCommands++;
                    }

                    if (arguments == $"runuser -u azureuser -- mpirun -np {Environment.ProcessorCount} ./xhpl")
                    {
                        this.fixture.Process.StandardOutput.Append(this.rawString);
                    }

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(6, executedArmCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task HPLExecutorExecutesWorkloadAsExpectedOnUbuntuX64Platform(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);

            List<string> expectedCommandsOnLinuxx64 = new List<string>()
            {
                $"wget http://www.netlib.org/benchmark/hpl/hpl-{this.fixture.Parameters["HPLVersion"]}.tar.gz -O {this.fixture.Parameters["PackageName"]}.tar.gz",
                $"tar -zxvf {this.fixture.Parameters["PackageName"]}.tar.gz",
                $"sudo bash -c \"source make_generic\"",
                $"make arch=Linux_GCC",
                $"sudo runuser -u azureuser -- mpirun --use-hwthread-cpus -np {Environment.ProcessorCount} ./xhpl"
            };

            using (TestHPLExecutor executor = new TestHPLExecutor(this.fixture))
            {
                int executedX64Commands = 0;

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (expectedCommandsOnLinuxx64.Any(c => c == $"{command} {arguments}"))
                    {
                        executedX64Commands++;
                    }
                    if (arguments == $"runuser -u azureuser -- mpirun --use-hwthread-cpus -np {Environment.ProcessorCount} ./xhpl")
                    {
                        this.fixture.Process.StandardOutput.Append(this.rawString);
                    }
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(5, executedX64Commands);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void HPLExecutorThrowsWhenPlatformIsNotSupported(PlatformID platformID, Architecture architecture)
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
            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\HPL\HPLResults.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawString);
            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = "HPL",
                ["HPLVersion"] = "2.3",
                ["N"] = "20000",
                ["NB"] = "256",
                ["Scenario"] = "ProcessorSpeed"
            };
        }

        private class TestHPLExecutor : HPLExecutor
        {
            public TestHPLExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestHPLExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
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
        }
    }
}
