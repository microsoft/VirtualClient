// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    using static VirtualClient.Actions.LatteExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class LatteClientExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(ScriptExecutorTests), "Examples", "Latte");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID, architecture);
            this.mockPackage = new DependencyPath("latte", this.mockFixture.PlatformSpecifics.GetPackagePath("latte"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "latte";
            this.mockFixture.Parameters["TestDuration"] = 300;
            this.mockFixture.Parameters["WarmupTime"] = 300;
            this.mockFixture.Parameters["Protocol"] = "Tcp";

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(!OperatingSystem.IsWindows(), LatteClientExecutorTests2.ExamplesDirectory, "Latte_Results_Example.txt"));

            LatteWorkloadState executionStartedState = new LatteWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<LatteWorkloadState> expectedStateItem = new Item<LatteWorkloadState>(nameof(LatteWorkloadState), executionStartedState);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(LatteWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem));
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupTest(platformID, architecture);

            this.mockFixture.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
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
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            using (TestLatteClientExecutor executor = new TestLatteClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);
                Assert.AreEqual(sendInstructionsExecuted, 3);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task LatteClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
        {
            int processExecuted = 0;
            this.SetupTest(platformID, architecture);
            string expectedPath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platformID, architecture).Path;
            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                processExecuted++;
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestLatteClientExecutor executor = new TestLatteClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                string exe = "latte.exe";
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                    new List<string>
                    {
                    this.mockFixture.Combine(expectedPath, exe) + " -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -tcp -hist -hl 1 -hc 9998 -bl 1.2.3.4"
                    },
                    commandsExecuted);
            }
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
