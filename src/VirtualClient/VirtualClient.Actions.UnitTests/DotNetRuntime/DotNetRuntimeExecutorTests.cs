// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    [Platform(Exclude = "Unix,Linux,MacOsX")]
    public class DotNetRuntimeExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;
        private string rawString;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "DotNetRuntime" }
            };

            this.SetupDefaultMockBehavior();
        }

        [Test]
        public async Task DotNetRuntimeExecutorInitializesItsDependenciesAsExpected()
        {
            using (TestDotNetRuntimeExecutor executor = new TestDotNetRuntimeExecutor(this.fixture))
            {
                Assert.IsNull(executor.ExecutablePath);

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, "win-x64", "dotnet.bat");
                Assert.AreEqual(expectedPath, executor.ExecutablePath);
            }
        }

        [Test]
        public void DotNetRuntimeExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound()
        {
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);
            using (TestDotNetRuntimeExecutor executor = new TestDotNetRuntimeExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [Ignore("There is some kind of very unusual and difficult to determine anomaly that causes this method to fail to run due to a call to the IProcessProxy.Kill() method downstream.")]
        public async Task DotNetRuntimeExecutorExecutesWorkloadAsExpected()
        {
            using (TestDotNetRuntimeExecutor executor = new TestDotNetRuntimeExecutor(this.fixture))
            {
                string expectedFilePath = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "runtimes", "win-x64", "dotnet.bat");
                int executed = 0;
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    executed++;
                    Assert.AreEqual(expectedFilePath, file);
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, executed);
            }
        }

        [Test]
        [Ignore("There is some kind of very unusual and difficult to determine anomaly that causes this method to fail to run due to a call to the IProcessProxy.Kill() method downstream.")]
        public void DotNetRuntimeExecutorThrowsWorkloadExceptionWhenTheResultsFileIsNotGenerated()
        {
            using (TestDotNetRuntimeExecutor executor = new TestDotNetRuntimeExecutor(this.fixture))
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

        private void SetupDefaultMockBehavior()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("DotNetRuntime", currentDirectory);
            string resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, "Examples", "DotNetRuntimeResultsExample.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);
            this.fixture.FileSystem.Setup(fc => fc.File.Copy(It.IsAny<string>(), It.IsAny<string>()));
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        private class TestDotNetRuntimeExecutor : DotNetRuntimeExecutor
        {
            public TestDotNetRuntimeExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestDotNetRuntimeExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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