// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class OpenFOAMExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;
        private string rawString;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void OpenFOAMExecutorThrowsWhenPlatformOrArchitectureIsNotSupported(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platformID, architecture);
            using (TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                if (platformID == PlatformID.Win32NT)
                {
                    Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
                }
                else
                {
                    Assert.AreEqual(ErrorReason.ProcessorArchitectureNotSupported, exception.Reason);
                } 
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task OpenFOAMExecutorInitializesItsDependenciesAsExpected(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platformID, architecture);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            using (TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                Assert.IsNull(executor.AllCleanExecutablePath);
                Assert.IsNull(executor.AllRunExecutablePath);
                Assert.IsNull(executor.IterationsFilePath);

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string arch = architecture == Architecture.X64 ? "x64" : "arm64";
                string expectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, $"linux-{arch}", "elbow", "Allclean");
                Assert.AreEqual(expectedPath, executor.AllCleanExecutablePath);

                expectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, $"linux-{arch}", "elbow", "Allrun");
                Assert.AreEqual(expectedPath, executor.AllRunExecutablePath);

                expectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, $"linux-{arch}", "elbow", "system", "controlDict");
                Assert.AreEqual(expectedPath, executor.IterationsFilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task OpenFOAMExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            int processExecuted = 0;
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platform, architecture).Path;
            string expectedArgumentsForCleanCommand = this.fixture.PlatformSpecifics.Combine(expectedPath, "elbow", "Allclean");
            string runWrapper = this.fixture.PlatformSpecifics.Combine(expectedPath, "tools", "AllrunWrapper");
            string runExe = this.fixture.PlatformSpecifics.Combine(expectedPath, "elbow", "Allrun");

            string expectedArgumentsForRunCommand = $"{runWrapper} {runExe}";
            string quotedCleanCommand = $"\"{expectedArgumentsForCleanCommand}\"";
            string quotedRunCommand = $"\"{runExe}\"";
            string quotedRunWrapperCommand = $"\"{runWrapper}\"";
            string expectedMakeExecutableCleanCommand = $"chmod +x {quotedCleanCommand}";
            string expectedMakeExecutableRunCommand = $"chmod +x {quotedRunCommand}";
            string expectedMakeExecutableRunWrapperCommand = $"chmod +x {quotedRunWrapperCommand}";

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                processExecuted++;
                if (!arguments.Contains("chmod"))
                {
                    if (arguments.Contains("Allclean", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.AreEqual(arguments, expectedArgumentsForCleanCommand);
                    }
                    else
                    {
                        Assert.AreEqual(arguments, expectedArgumentsForRunCommand);
                    }
                }
                else
                {
                    if (arguments.Contains("Allclean", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.AreEqual(arguments, expectedMakeExecutableCleanCommand);
                    }
                    else if (arguments.Contains("AllrunWrapper", StringComparison.OrdinalIgnoreCase))
                    {
                        Assert.AreEqual(arguments, expectedMakeExecutableRunWrapperCommand);
                    }
                    else
                    {
                        Assert.AreEqual(arguments, expectedMakeExecutableRunCommand);
                    }

                }
                Assert.IsNull(workingDirectory);
                return this.fixture.Process;

            };

            using TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.ExecuteAsync(CancellationToken.None);

            Assert.AreEqual(5, processExecuted);

        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMExecutorThrowsWhenAllCleanExecutableFileIsNotGenerated(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.fixture.FileSystem.Setup(fe => fe.File.Exists(executor.AllCleanExecutablePath)).Returns(false);
                    return this.fixture.Process;
                };

                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMExecutorThrowsWhenAllRunExecutableFileIsNotPresent(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.fixture.FileSystem.Setup(fe => fe.File.Exists(executor.AllRunExecutablePath)).Returns(false);
                    return this.fixture.Process;
                };

                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMExecutorThrowsWhenTheResultsFileIsNotGenerated(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            using (TestOpenFOAMExecutor executor = new TestOpenFOAMExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.fixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsFilePath)).Returns(false);
                    return this.fixture.Process;
                };

                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            this.mockPath = new DependencyPath("OpenFOAM", this.fixture.PlatformSpecifics.GetPackagePath("OpenFOAM"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(OpenFOAMExecutor.PackageName), "OpenFOAM" },
                { nameof(OpenFOAMExecutor.Simulation), "elbow" },
                { nameof(OpenFOAMExecutor.Solver), "icoFoam" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("OpenFOAM", currentDirectory);
            string resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, "Examples","OpenFOAM", "OpenFOAMResultsExample.txt");
            this.rawString = File.ReadAllText(resultsPath);
            
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        private class TestOpenFOAMExecutor : OpenFOAMExecutor
        {
            public TestOpenFOAMExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestOpenFOAMExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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