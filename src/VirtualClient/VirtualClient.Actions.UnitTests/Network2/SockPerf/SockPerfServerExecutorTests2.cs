// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.SockPerfExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfServerExecutorTests2
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();

        }

        public void SetupDefaultMockApiBehavior(PlatformID platformID, Architecture architecture)
        {
            this.fixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "Networking";
            this.fixture.Parameters["Protocol"] = "TCP";
            this.fixture.Parameters["Port"] = 5001;
            this.fixture.Parameters["Workload"] = "CPS";
            this.fixture.Parameters["Scenario"] = "SockPerfMock";
            this.fixture.Parameters["MessagesPerSecond"] = "max";
            this.fixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "SockPerf", "SockPerfClientExample1.txt");
            string results = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfServerExecutorExecutesAsExpectedForStartInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.fixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerStartExecution;
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestSockPerfServerExecutor executor = new TestSockPerfServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.fixture.Process;
            };

            TestSockPerfServerExecutor component = new TestSockPerfServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                "sudo chmod +x \"" + this.fixture.Combine(expectedPath, "sockperf") + "\"",
            },
            commandsExecuted); ;
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfServerExecutorExecutesAsExpectedForResetInstructions(PlatformID platformID, Architecture architecture)
        {
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.fixture.Parameters["TypeOfInstructions"] = InstructionsType.ClientServerReset;
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            TestSockPerfServerExecutor executor = new TestSockPerfServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.fixture.Process;
            };

            TestSockPerfServerExecutor component = new TestSockPerfServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                "sudo chmod +x \"" + this.fixture.Combine(expectedPath, "sockperf") + "\"",
            },
            commandsExecuted); ;
        }

        private class TestSockPerfServerExecutor : SockPerfServerExecutor2
        {
            public TestSockPerfServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public new CancellationTokenSource ServerCancellationSource
            {
                get
                {
                    return base.ServerCancellationSource;
                }

                set
                {
                    base.ServerCancellationSource = value;
                }
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            protected override bool IsProcessRunning(string processName)
            {
                return true;
            }
        }
    }
}
