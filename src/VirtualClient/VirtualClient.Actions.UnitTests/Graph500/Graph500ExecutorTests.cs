// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class Graph500ExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(Graph500ExecutorTests), "Examples", "Graph500");

        private const string Scale = "10";
        private const string EdgeFactor = "4";
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockPackage = new DependencyPath("Graph500", this.mockFixture.GetPackagePath("graph500"));
            this.mockFixture.Parameters["PackageName"] = "Graph500";

            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(Graph500ExecutorTests.ExamplesDirectory, "Graph500ResultsExample.txt"));
            this.mockFixture.Process.StandardOutput.Append(exampleResults);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/src/graph500_reference_bfs_sssp")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/src/graph500_reference_bfs_sssp")]
        public async Task Graph500ExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            using (TestGraph500Executor executor = new TestGraph500Executor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedExecutableFilePath = this.mockFixture.Combine(this.mockPackage.Path, binaryPath);

                Assert.AreEqual(expectedExecutableFilePath, executor.ExecutableFilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/src/graph500_reference_bfs_sssp")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-x64/src/graph500_reference_bfs_sssp")]
        public async Task Graph500ExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            mockFixture.Parameters["Scale"] = Scale;
            mockFixture.Parameters["EdgeFactor"] = EdgeFactor;
            using (TestGraph500Executor executor = new TestGraph500Executor(this.mockFixture))
            {
                string expectedFilePath = this.mockFixture.PlatformSpecifics.Combine(this.mockPackage.Path, binaryPath);
                int executed = 0;

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
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

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(2, executed);
            }
        }

        [Test]
        public async Task Graph500ExecutorLogsTheExpectedWorkloadMetrics()
        {
            this.SetupTest();

            using (TestGraph500Executor executor = new TestGraph500Executor(this.mockFixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var messages = this.mockFixture.Logger.MessagesLogged("Graph500.ScenarioResult");
                Assert.IsNotEmpty(messages);
                Assert.True(messages.Count() == 55);
                Assert.IsTrue(messages.All(msg => msg.Item3 as EventContext != null));
                Assert.IsTrue(messages.All(msg => (msg.Item3 as EventContext).Properties["scenarioName"].ToString() == "Graph500"));
            }
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
