// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class NetworkingWorkloadExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.SetupForPlatform(PlatformID.Win32NT, NetworkingWorkloadTool.NTttcp);
        }

        [Test]
        [TestCase(NetworkingWorkloadTool.NTttcp, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.NTttcp, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.CPS, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.CPS, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.Latte, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.SockPerf, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.Latte, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.SockPerf, PlatformID.Win32NT)]
        public async Task NetworkingWorkloadExecutorServerDoesNotRunOnItOwn(NetworkingWorkloadTool toolName, PlatformID platformID)
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
            this.fixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(component.ToolExecuted == string.Empty);
        }

        [Test]
        [TestCase(NetworkingWorkloadTool.NTttcp, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.NTttcp, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.CPS, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.CPS, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.Latte, PlatformID.Win32NT)]
        [TestCase(NetworkingWorkloadTool.SockPerf, PlatformID.Unix)]
        public async Task NetworkingWorkloadExecutorClientExecutesAsExpectedTools(NetworkingWorkloadTool toolName, PlatformID platformID)
        {
            this.SetupForPlatform(platformID, toolName);
            this.fixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(toolName.ToString() == component.ToolExecuted);
        }

        [Test]
        [TestCase(NetworkingWorkloadTool.Latte, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.SockPerf, PlatformID.Win32NT)]
        public async Task NetworkingWorkloadExecutorClientDoesNotRunToolsOnNonSupportedPlatformsAsExpectedTools(NetworkingWorkloadTool toolName, PlatformID platformID)
        {
            this.SetupForPlatform(platformID, toolName);
            this.fixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(component.ToolExecuted == string.Empty);
        }

        [Test]
        [Ignore("This is a flaky test and networking executors will be redesigned.")]
        public async Task NetworkingWorkloadExecutorServerExecutesToolOnInstructionsReceived()
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Mock<object> mockSender = new Mock<object>();
            
            NetworkingWorkloadState mockNetworkingWorkloadState = new NetworkingWorkloadState();
            mockNetworkingWorkloadState.Scenario = "AnyScenario";
            mockNetworkingWorkloadState.Tool = NetworkingWorkloadTool.NTttcp;
            mockNetworkingWorkloadState.ToolState = NetworkingWorkloadToolState.Start;
            JObject mockJobject = JObject.FromObject(new Item<NetworkingWorkloadState>("mockId",mockNetworkingWorkloadState));
            
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            component.ServerCancellationSource = new CancellationTokenSource();
            component.OnClientInstructionsReceived.Invoke(mockSender.Object, mockJobject);
            
            Assert.AreEqual(NetworkingWorkloadTool.NTttcp.ToString(), component.ToolExecuted);
        }

        [Test]
        public void NetworkingWorkloadExecutorCreatesServerAPIClientForClient()
        {
            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Mock<object> mockSender = new Mock<object>();
        }

        private void SetupForPlatform(PlatformID platformID, NetworkingWorkloadTool tool)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.fixture.Parameters["PackageName"] = "Networking";
            this.fixture.Parameters["Scenario"] = "Scenario_1";
            this.fixture.Parameters["ToolName"] = NetworkingWorkloadTool.NTttcp.ToString();

            NetworkingWorkloadState runningWorkloadState = new NetworkingWorkloadState(
                "networking",
                "Scenario_1",
                tool,
                NetworkingWorkloadToolState.Running);

            Item<JObject> expectedStateItem = new Item<JObject>(nameof(NetworkingWorkloadState), JObject.FromObject(runningWorkloadState));

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(NetworkingWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), runningWorkloadState)))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        public class TestNetworkingWorkloadExecutor : NetworkingWorkloadExecutor
        {
            private MockFixture fixture;
            private DependencyPath mockPath;

            public string ToolExecuted { get; set; } = string.Empty;

            public TestNetworkingWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
               : base(dependencies, parameters)
            {
                this.fixture = new MockFixture();
                this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
                fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
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

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            public Action<object, JObject> OnClientInstructionsReceived => base.OnInstructionsReceived;

            protected override NetworkingWorkloadToolExecutor CreateWorkloadExecutor(NetworkingWorkloadTool toolName)
            {
                this.fixture.Parameters["ToolName"] = toolName.ToString();
                TestToolExecutor testToolExecutor = new TestToolExecutor(this.fixture.Dependencies, this.fixture.Parameters);
                testToolExecutor.OnExecuteAsync = () => this.ToolExecuted = toolName.ToString();
                return testToolExecutor;
            }
        }

        public class TestToolExecutor : NetworkingWorkloadToolExecutor
        {
            public TestToolExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) : base(dependencies, parameters)
            {
            }
            public Action OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() => this.OnExecuteAsync?.Invoke());
            }

            protected override string GetCommandLineArguments()
            {
                return "Dummy Parameters";
            }
        }
    }
}
