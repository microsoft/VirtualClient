// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
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
    using static VirtualClient.Actions.CPSExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class CPSClientExecutorTests2
    {
        private MockFixture fixture;
        private DependencyPath mockPath;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            VirtualClientApiClient.DefaultPollingWaitTime = TimeSpan.Zero;
        }

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSClientExecutorSendsExpectedInstructions(PlatformID platformID, Architecture architecture)
        {
            int sendInstructionsExecuted = 0;
            this.SetupDefaultMockApiBehavior(platformID, architecture);
            this.fixture.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((obj, can, pol) =>
                {
                    Item<Instructions> stateItem = obj.ToObject<Item<Instructions>>();
                    if (stateItem.Definition.Type == InstructionsType.ClientServerReset)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 0);

                        sendInstructionsExecuted++;
                    }

                    if (stateItem.Definition.Type == InstructionsType.ClientServerStartExecution)
                    {
                        Assert.AreEqual(sendInstructionsExecuted, 1);
                        sendInstructionsExecuted++;
                    }

                    Assert.AreEqual(stateItem.Definition.Properties["Scenario"], "AnyScenario");
                    Assert.AreEqual(stateItem.Definition.Properties["Type"], typeof(CPSServerExecutor2).Name);
                    Assert.AreEqual(stateItem.Definition.Properties["Connections"], 256);
                    Assert.AreEqual(stateItem.Definition.Properties["Port"], 3001);
                    Assert.AreEqual(stateItem.Definition.Properties["TestDuration"], 300);
                    Assert.AreEqual(stateItem.Definition.Properties["WarmupTime"], 44);
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                return this.fixture.Process;
            };

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(sendInstructionsExecuted, 2);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CPSClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
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

            TestCPSClientExecutor component = new TestCPSClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            string exe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
            if (platformID == PlatformID.Win32NT)
            {
                Assert.AreEqual(1, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    this.fixture.Combine(expectedPath, exe) + " -c -r 256 1.2.3.4,0,1.2.3.5,3001,100,100,0,1 -i 10 -wt 44 -t 300 -ds 30"
                },
                commandsExecuted);
            }
            else
            {
                Assert.AreEqual(2, processExecuted);
                CollectionAssert.AreEqual(
                new List<string>
                {
                    "sudo chmod +x \"" + this.fixture.Combine(expectedPath, exe) + "\"",
                    this.fixture.Combine(expectedPath, exe) + " -c -r 256 1.2.3.4,0,1.2.3.5,3001,100,100,0,1 -i 10 -wt 44 -t 300 -ds 30"
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

            this.fixture.Parameters["PackageName"] = "cps";
            this.fixture.Parameters["Port"] = 3001;
            this.fixture.Parameters["Connections"] = 256;
            this.fixture.Parameters["TestDuration"] = 300;
            this.fixture.Parameters["WarmupTime"] = 44;
            this.fixture.Parameters["Delaytime"] = 30;
            this.fixture.Parameters["ConfidenceLevel"] = "99";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CPS", "CPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.fixture.Process.StandardOutput.Append(results);

            CPSWorkloadState executionStartedState = new CPSWorkloadState(ClientServerStatus.ExecutionStarted);
            Item<CPSWorkloadState> expectedStateItem = new Item<CPSWorkloadState>(nameof(CPSWorkloadState), executionStartedState);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(CPSWorkloadState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }


        private class TestCPSClientExecutor : CPSClientExecutor2
        {
            public TestCPSClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
