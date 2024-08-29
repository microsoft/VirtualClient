// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Contracts;
    using Polly;
    using System.Net.Http;
    using System.Net;
    using System.IO;
    using System.Reflection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private NetworkingWorkloadState networkingWorkloadState;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "Networking";
            this.fixture.Parameters["TestDuration"] = "300";
            this.fixture.Parameters["Protocol"] = "TCP";
            this.fixture.Parameters["TestMode"] = "ping-pong";
            this.fixture.Parameters["MessageSize"] = "64";
            this.fixture.Parameters["MessagesPerSecond"] = "max";
            this.fixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "SockPerf", "SockPerfClientExample1.txt");
            string results = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        [Ignore("The networking workload is being refactored/rewritten. As such the unit tests will be rewritten as well.")]
        public async Task SockPerfExecutorClientExecutesAsExpected()
        {
            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None,CancellationToken.None);

            int processExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                     .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
                return this.fixture.Process;
            };

            TestSockPerfExecutor component = new TestSockPerfExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        [Ignore("The networking workload is being refactored/rewritten. As such the unit tests will be rewritten as well.")]
        public async Task SockPerfExecutorServerExecutesAsExpected()
        {
            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                return this.fixture.Process;
            };

            TestSockPerfExecutor component = new TestSockPerfExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }


        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.SockPerf;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.Protocol = "UDP";
            this.networkingWorkloadState.TestMode = "MockTestMode";
            this.networkingWorkloadState.MessagesPerSecond = "max";
            this.networkingWorkloadState.ConfidenceLevel = 99;

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        private class TestSockPerfExecutor : SockPerfExecutor
        {
            public TestSockPerfExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            protected override bool IsProcessRunning(string processName)
            {
                return true;
            }
        }
    }
}
