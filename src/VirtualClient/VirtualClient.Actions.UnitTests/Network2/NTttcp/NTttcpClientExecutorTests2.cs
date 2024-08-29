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
    using static VirtualClient.Actions.NTttcpExecutor2;
    using Newtonsoft.Json;
    using System.Net.Sockets;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class NTttcpClientExecutorTests2
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
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task NTttcpClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.fixture.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((obj, can, pol) =>
                {
                    Item<Instructions> notification = obj.ToObject<Item<Instructions>>();
                    if (notification.Definition.Type == InstructionsType.ClientServerReset)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 0);

                        sendInstructionsExecuted++;
                    }

                    if (notification.Definition.Type == InstructionsType.ClientServerStartExecution)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 1);

                        sendInstructionsExecuted++;
                    }

                    Assert.AreEqual(notification.Definition.Properties["Type"], typeof(NTttcpServerExecutor2).Name);
                    Assert.AreEqual(notification.Definition.Properties["Scenario"], "AnyScenario");
                    Assert.AreEqual(notification.Definition.Properties["Protocol"], ProtocolType.Tcp.ToString());
                    Assert.AreEqual(notification.Definition.Properties["ThreadCount"], 1);
                    Assert.AreEqual(notification.Definition.Properties["ClientBufferSize"], "4k");
                    Assert.AreEqual(notification.Definition.Properties["TestDuration"], 300);
                    Assert.AreEqual(notification.Definition.Properties["Port"], 5500);
                    Assert.AreEqual(notification.Definition.Properties["ReceiverMultiClientMode"], true);
                    Assert.AreEqual(notification.Definition.Properties["SenderLastClient"], true);
                    Assert.AreEqual(notification.Definition.Properties["ThreadsPerServerPort"], 2);
                    Assert.AreEqual(notification.Definition.Properties["ConnectionsPerThread"], 2);
                    Assert.AreEqual(notification.Definition.Properties["DevInterruptsDifferentiator"], "mlx");
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            TestNTttcpClientExecutor component = new TestNTttcpClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(sendInstructionsExecuted, 2);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task NTttcpClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
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

            TestNTttcpClientExecutor component = new TestNTttcpClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);


            string exe = platformID == PlatformID.Win32NT ? "NTttcp.exe" : "ntttcp";
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    this.fixture.Combine(expectedPath, exe) + " -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 300 -l  -p 5500 -xml " + this.fixture.Combine(expectedPath, "AnyScenario", "ntttcp-results.xml") + "  -nic 1.2.3.4"
                },
                commandsExecuted);
            }
            else
            {
                Assert.AreEqual(3, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    "sudo chmod +x \"" + this.fixture.Combine(expectedPath, exe) + "\"",
                    this.fixture.Combine(expectedPath, exe) + " -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 300 -b  -x " + this.fixture.Combine(expectedPath, "AnyScenario", "ntttcp-results.xml") + " -p 5500  -L -M -n 2 -l 2 --show-dev-interrupts mlx",
                    "sysctl net.ipv4.tcp_rmem net.ipv4.tcp_wmem"
                },
                commandsExecuted);
            }
        }

        private void SetupDefaultMockApiBehavior(PlatformID platformID, Architecture architecture)
        {
            this.fixture.Setup(platformID, architecture);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "Networking";
            this.fixture.Parameters["TestDuration"] = 300;
            this.fixture.Parameters["WarmupTime"] = 300;
            this.fixture.Parameters["Protocol"] = ProtocolType.Tcp.ToString();
            this.fixture.Parameters["ThreadCount"] = 1;
            this.fixture.Parameters["ClientBufferSize"] = "4k";
            this.fixture.Parameters["Port"] = 5500;
            this.fixture.Parameters["ReceiverMultiClientMode"] = true;
            this.fixture.Parameters["SenderLastClient"] = true;
            this.fixture.Parameters["ThreadsPerServerPort"] = 2;
            this.fixture.Parameters["ConnectionsPerThread"] = 2;
            this.fixture.Parameters["DevInterruptsDifferentiator"] = "mlx";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "NTttcp", "ClientOutput.xml");
            string results = File.ReadAllText(resultsPath);

            NTttcpWorkloadState executionStartedState = new NTttcpWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<NTttcpWorkloadState> expectedStateItem = new Item<NTttcpWorkloadState>(nameof(NTttcpWorkloadState), executionStartedState);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(NTttcpWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        private class TestNTttcpClientExecutor : NTttcpClientExecutor2
        {
            public TestNTttcpClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
