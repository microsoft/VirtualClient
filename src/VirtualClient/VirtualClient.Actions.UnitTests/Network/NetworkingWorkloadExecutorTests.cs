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
        private MockFixture mockFixture;
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
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
            this.mockFixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

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
            this.mockFixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(toolName.ToString() == component.ToolExecuted);
        }

        [Test]
        [TestCase(NetworkingWorkloadTool.Latte, PlatformID.Unix)]
        [TestCase(NetworkingWorkloadTool.SockPerf, PlatformID.Win32NT)]
        public async Task NetworkingWorkloadExecutorClientDoesNotRunToolsOnNonSupportedPlatformsAsExpectedTools(NetworkingWorkloadTool toolName, PlatformID platformID)
        {
            this.SetupForPlatform(platformID, toolName);
            this.mockFixture.Parameters["ToolName"] = toolName.ToString();

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(component.ToolExecuted == string.Empty);
        }

        [Test]
        [Ignore("This is a flaky test and networking executors will be redesigned.")]
        public async Task NetworkingWorkloadExecutorServerExecutesToolOnInstructionsReceived()
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
            TestNetworkingWorkloadExecutor component = new TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Mock<object> mockSender = new Mock<object>();
        }

        private void SetupForPlatform(PlatformID platformID, NetworkingWorkloadTool tool)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.mockFixture.Parameters["PackageName"] = "Networking";
            this.mockFixture.Parameters["Scenario"] = "Scenario_1";
            this.mockFixture.Parameters["ToolName"] = NetworkingWorkloadTool.NTttcp.ToString();

            NetworkingWorkloadState runningWorkloadState = new NetworkingWorkloadState(
                "networking",
                "Scenario_1",
                tool,
                NetworkingWorkloadToolState.Running);

            Item<JObject> expectedStateItem = new Item<JObject>(nameof(NetworkingWorkloadState), JObject.FromObject(runningWorkloadState));

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetEventingOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(NetworkingWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), runningWorkloadState)))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        public class TestNetworkingWorkloadExecutor : NetworkingWorkloadExecutor
        {
            private MockFixture mockFixture;
            private DependencyPath mockPath;

            public string ToolExecuted { get; set; } = string.Empty;

            public TestNetworkingWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
               : base(dependencies, parameters)
            {
                this.mockFixture = new MockFixture();
                this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
                mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
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
                this.mockFixture.Parameters["ToolName"] = toolName.ToString();
                TestToolExecutor testToolExecutor = new TestToolExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
