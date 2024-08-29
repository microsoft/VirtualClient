// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Reflection;
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
    using static VirtualClient.Actions.LatteExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class LatteClientExecutorTests2
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
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
                        sendInstructionsExecuted++;
                    }

                    Assert.AreEqual(stateItem.Definition.Properties["Scenario"], "AnyScenario");
                    Assert.AreEqual(stateItem.Definition.Properties["Type"], typeof(LatteServerExecutor2).Name);
                    Assert.AreEqual(stateItem.Definition.Properties["Protocol"], ProtocolType.Tcp.ToString());
                    Assert.AreEqual(stateItem.Definition.Properties["TestDuration"], 300);
                    Assert.AreEqual(stateItem.Definition.Properties["WarmupTime"], 300);
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            TestLatteClientExecutor component = new TestLatteClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(sendInstructionsExecuted, 3);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
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

            TestLatteClientExecutor component = new TestLatteClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);


            string exe = "latte.exe";
            Assert.AreEqual(1, processExecuted);
            CollectionAssert.AreEqual(
            new List<string>
            {
                this.fixture.Combine(expectedPath, exe) + " -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -tcp -hist -hl 1 -hc 9998 -bl 1.2.3.4"
            },
            commandsExecuted);
        }

        private void SetupDefaultMockApiBehavior(PlatformID platformID, Architecture architecture)
        {
            this.fixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "latte";
            this.fixture.Parameters["TestDuration"] = 300;
            this.fixture.Parameters["WarmupTime"] = 300;
            this.fixture.Parameters["Protocol"] = "Tcp";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Latte", "Latte_Results_Example.txt");
            string results = File.ReadAllText(resultsPath);

            LatteWorkloadState executionStartedState = new LatteWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<LatteWorkloadState> expectedStateItem = new Item<LatteWorkloadState>(nameof(LatteWorkloadState), executionStartedState);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(LatteWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem));
        }

        private class TestLatteClientExecutor : LatteClientExecutor2
        {
            public TestLatteClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
