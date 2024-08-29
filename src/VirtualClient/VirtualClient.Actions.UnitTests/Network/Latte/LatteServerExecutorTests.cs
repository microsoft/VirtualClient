// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LatteServerExecutorTests
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
            this.fixture.Parameters["Connections"] = "256";
            this.fixture.Parameters["TestDuration"] = "300";
            this.fixture.Parameters["WarmupTime"] = "300";
            this.fixture.Parameters["Protocol"] = "Tcp";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Latte", "Latte_Results_Example.txt");
            string results = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        [Ignore("The networking workload is being refactored/rewritten. As such the unit tests will be rewritten as well.")]
        public void LatteServerExecutorThrowsOnUnsupportedOS()
        {
            this.fixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestLatteServerExecutor component = new TestLatteServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task LatteServerExecutorExecutesAsExpected()
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

            TestLatteServerExecutor component = new TestLatteServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.Latte;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.Protocol = "UDP";
            this.networkingWorkloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }
        private class TestLatteServerExecutor : LatteServerExecutor
        {
            public TestLatteServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
