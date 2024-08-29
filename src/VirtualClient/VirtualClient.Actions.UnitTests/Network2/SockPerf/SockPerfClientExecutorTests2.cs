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
    using System.Runtime.InteropServices;
    using Newtonsoft.Json.Linq;
    using static VirtualClient.Actions.SockPerfExecutor2;
    using System.Net.Sockets;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class
        SockPerfClientExecutorTests2
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();

        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.fixture.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((obj, can, pol) =>
                {
                    Item<Instructions> stateItem = obj.ToObject<Item<Instructions>>();
                    if (stateItem.Definition.Type == InstructionsType.ClientServerReset)
                    {
                        sendInstructionsExecuted++;
                    }

                    if (stateItem.Definition.Type == InstructionsType.ClientServerStartExecution)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 1);
                        Assert.AreEqual(stateItem.Definition.Properties["Scenario"], "AnyScenario");
                        Assert.AreEqual(stateItem.Definition.Properties["Type"], typeof(SockPerfServerExecutor2).Name);
                        Assert.AreEqual(stateItem.Definition.Properties["Protocol"], ProtocolType.Tcp.ToString());
                        Assert.AreEqual(stateItem.Definition.Properties["TestMode"], "mockMode");
                        Assert.AreEqual(stateItem.Definition.Properties["TestDuration"], 300);
                        Assert.AreEqual(stateItem.Definition.Properties["MessageSize"], 44);
                        sendInstructionsExecuted++;
                    }
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            TestSockPerfClientExecutor component = new TestSockPerfClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(sendInstructionsExecuted, 3);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
        {
            int processExecuted = 0;
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.fixture.Process;
            };

            TestSockPerfClientExecutor component = new TestSockPerfClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            string exe = "sockperf";
            Assert.AreEqual(2, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                "sudo chmod +x \"" + this.fixture.Combine(expectedPath, exe) + "\"",
                this.fixture.Combine(expectedPath, exe) + " mockMode -i 1.2.3.5 -p 6100 --tcp -t 300 --mps=max --full-rtt --msg-size 44 --client_ip 1.2.3.4 --full-log " + this.fixture.Combine(expectedPath, "AnyScenario", "sockperf-results.txt")
            },
            commandsExecuted) ;
        }

        private void SetupDefaultMockApiBehavior(PlatformID platformID, Architecture architecture)
        {
            this.fixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "Networking";
            this.fixture.Parameters["Protocol"] = ProtocolType.Tcp.ToString();
            this.fixture.Parameters["TestMode"] = "mockMode";
            this.fixture.Parameters["TestDuration"] = 300;
            this.fixture.Parameters["MessageSize"] = 44;
            this.fixture.Parameters["MessagesPerSecond"] = "max";
            this.fixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "SockPerf", "SockPerfClientExample1.txt");
            string results = File.ReadAllText(resultsPath);

            SockPerfWorkloadState executionStartedState = new SockPerfWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<SockPerfWorkloadState> expectedStateItem = new Item<SockPerfWorkloadState>(nameof(SockPerfWorkloadState), executionStartedState);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(SockPerfWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        private class TestSockPerfClientExecutor : SockPerfClientExecutor2
        {
            public TestSockPerfClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
