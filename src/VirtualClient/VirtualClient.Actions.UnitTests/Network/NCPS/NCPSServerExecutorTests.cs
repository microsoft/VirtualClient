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
    public class NCPSServerExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests2), "Examples", "NCPS");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private NetworkingWorkloadState workloadState;

        public void SetupApiCalls()
        {
            this.workloadState = new NetworkingWorkloadState();
            this.workloadState.Scenario = "AnyScenario";
            this.workloadState.Tool = NetworkingWorkloadTool.NCPS;
            this.workloadState.ToolState = NetworkingWorkloadToolState.Running;
            this.workloadState.Protocol = "TCP";
            this.workloadState.TestMode = "MockTestMode";

            var expectedStateItem = new Item<NetworkingWorkloadState>(nameof(NetworkingWorkloadState), this.workloadState);

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                 .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
        }

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockPackage = new DependencyPath("ncps", this.mockFixture.PlatformSpecifics.GetPackagePath("ncps"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "ncps";
            this.mockFixture.Parameters["ThreadCount"] = "16";
            this.mockFixture.Parameters["TestDuration"] = "00:05:00";
            this.mockFixture.Parameters["WarmupTime"] = "00:00:30";
            this.mockFixture.Parameters["Delaytime"] = "00:00:00";
            this.mockFixture.Parameters["ConfidenceLevel"] = "99";

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(NCPSServerExecutorTests.ExamplesDirectory, "NCPS_Example_Results_Server.txt"));

            this.mockFixture.Process.StandardOutput.Append(exampleResults);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.SetupApiCalls();
        }

        [Test]
        public void NCPSServerExecutorThrowsOnUnsupportedOS()
        {
            this.mockFixture.SystemManagement.SetupGet(sm => sm.Platform).Returns(PlatformID.Other);
            TestNCPSServerExecutor component = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task NCPSServerExecutorExecutesAsExpected()
        {
            NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor networkingWorkloadExecutor = new NetworkingWorkloadExecutorTests.TestNetworkingWorkloadExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await networkingWorkloadExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            int processExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
               
                return this.mockFixture.Process;
            };

            TestNCPSServerExecutor component = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        public void NCPSServerExecutorUsesExpectedDefaultParameters()
        {
            TestNCPSServerExecutor executor = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(16, executor.ThreadCount);
            Assert.AreEqual(9800, executor.Port);
            Assert.AreEqual(16, executor.PortCount);
            Assert.AreEqual("1", executor.DataTransferMode);
        }

        [Test]
        public void NCPSServerExecutorSupportsCustomDataTransferModes()
        {
            // Test continuous send mode
            this.mockFixture.Parameters["DataTransferMode"] = "s";
            TestNCPSServerExecutor executor = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.AreEqual("s", executor.DataTransferMode);

            // Test continuous receive mode
            this.mockFixture.Parameters["DataTransferMode"] = "r";
            executor = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.AreEqual("r", executor.DataTransferMode);

            // Test ping-pong mode
            this.mockFixture.Parameters["DataTransferMode"] = "p";
            executor = new TestNCPSServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.AreEqual("p", executor.DataTransferMode);
        }

        private class TestNCPSServerExecutor : NCPSServerExecutor
        {
            public TestNCPSServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
