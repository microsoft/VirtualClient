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
    public class CPSClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;
        private NetworkingWorkloadState networkingWorkloadState;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "Networking";
            this.mockFixture.Parameters["Connections"] = "256";
            this.mockFixture.Parameters["TestDuration"] = "300";
            this.mockFixture.Parameters["WarmupTime"] = "30";
            this.mockFixture.Parameters["Delaytime"] = "0";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CPS", "CPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.Process.StandardOutput.Append(results);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupNetworkingWorkloadState();
        }

        [Test]
        public void CPSClientExecutorThrowsOnUnsupportedOS()
        {
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        public async Task CPSClientExecutorExecutesAsExpected()
        {
            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None,CancellationToken.None);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Stopped;
                var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

                this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                     .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

                return this.mockFixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        private void SetupNetworkingWorkloadState()
        {
            this.networkingWorkloadState = new NetworkingWorkloadState();
            this.networkingWorkloadState.Scenario = "AnyScenario";
            this.networkingWorkloadState.Tool = NetworkingWorkloadTool.CPS;
            this.networkingWorkloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.networkingWorkloadState.Protocol = "UDP";
            this.networkingWorkloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.networkingWorkloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        private class TestCPSClientExecutor : CPSClientExecutor
        {
            public TestCPSClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }
        }
    }
}
