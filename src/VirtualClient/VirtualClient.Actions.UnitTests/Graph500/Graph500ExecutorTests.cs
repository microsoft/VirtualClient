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
    public class Graph500ExecutorTests
    {
        private const string Scale = "10";
        private const string EdgeFactor = "4";
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.mockPath = this.fixture.Create<DependencyPath>();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/src/graph500_reference_bfs_sssp")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/src/graph500_reference_bfs_sssp")]
        public async Task Graph500ExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestGraph500Executor executor = new TestGraph500Executor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.fixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedExecutableFilePath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, binaryPath);

                Assert.AreEqual(expectedExecutableFilePath, executor.ExecutableFilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/src/graph500_reference_bfs_sssp")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-x64/src/graph500_reference_bfs_sssp")]
        public async Task Graph500ExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            fixture.Parameters["Scale"] = Scale;
            fixture.Parameters["EdgeFactor"] = EdgeFactor;
            using (TestGraph500Executor executor = new TestGraph500Executor(this.fixture))
            {
                string expectedFilePath = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, binaryPath);
                int executed = 0;

                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (command == "make")
                    {
                        executed++;
                    }
                    else
                    {
                        executed++;
                        Assert.AreEqual($"{command} {arguments}", $"{executor.ExecutableFilePath} {executor.Scale} {executor.EdgeFactor}");
                    }

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(2, executed);
            }
        }

        [Test]
        public async Task Graph500ExecutorLogsTheExpectedWorkloadMetrics()
        {
            this.SetupDefaultMockBehavior();

            using (TestGraph500Executor executor = new TestGraph500Executor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var messages = this.fixture.Logger.MessagesLogged("Graph500.ScenarioResult");
                Assert.IsNotEmpty(messages);
                Assert.True(messages.Count() == 55);
                Assert.IsTrue(messages.All(msg => msg.Item3 as EventContext != null));
                Assert.IsTrue(messages.All(msg => (msg.Item3 as EventContext).Properties["scenarioName"].ToString() == "Graph500"));
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            this.fixture.Parameters["PackageName"] = "Graph500";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Graph500", currentDirectory);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.fixture.Process.StandardOutput.Append(
                File.ReadAllText(this.fixture.Combine(this.currentDirectoryPath.Path, "Examples", "Graph500", "Graph500ResultsExample.txt")));
        }

        private class TestGraph500Executor : Graph500Executor
        {
            public TestGraph500Executor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestGraph500Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new string ExecutableFilePath
            {
                get
                {
                    return base.ExecutableFilePath;
                }
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
