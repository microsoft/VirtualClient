// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.SockPerfExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfClientExecutorTests2 : MockFixture
    {
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.Setup(platformID, architecture);
            this.mockPackage = new DependencyPath("sockperf", this.GetPackagePath("sockperf"));
            this.SetupPackage(this.mockPackage);

            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.Parameters["PackageName"] = "sockperf";
            this.Parameters["Protocol"] = ProtocolType.Tcp.ToString();
            this.Parameters["TestMode"] = "mockMode";
            this.Parameters["TestDuration"] = 300;
            this.Parameters["MessageSize"] = 44;
            this.Parameters["MessagesPerSecond"] = "max";
            this.Parameters["ConfidenceLevel"] = "99";

            string exampleResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "SockPerf", "SockPerfClientExample1.txt");

            SockPerfWorkloadState executionStartedState = new SockPerfWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<SockPerfWorkloadState> expectedStateItem = new Item<SockPerfWorkloadState>(nameof(SockPerfWorkloadState), executionStartedState);

            this.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(SockPerfWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupTest(platformID, architecture);
            this.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
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
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            using (TestSockPerfClientExecutor executor = new TestSockPerfClientExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.AreEqual(sendInstructionsExecuted, 3);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task SockPerfClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
        {
            int processExecuted = 0;
            this.SetupTest(platformID, architecture);
            string expectedPath = this.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();

            this.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.Process;
            };

            using (TestSockPerfClientExecutor executor = new TestSockPerfClientExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                string exe = "sockperf";
                Assert.AreEqual(2, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                "sudo chmod +x \"" + this.Combine(expectedPath, exe) + "\"",
                this.Combine(expectedPath, exe) + " mockMode -i 1.2.3.5 -p 6100 --tcp -t 300 --mps=max --full-rtt --msg-size 44 --client_ip 1.2.3.4 --full-log " + this.Combine(expectedPath, "AnyScenario", "sockperf-results.txt")
                },
                commandsExecuted);
            }
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
