// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CPSExecutor2;

    [TestFixture]
    [Category("Unit")]
    public class NetworkingWorkloadProxyTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;

        [OneTimeSetUp]
        public void InitializeFixture()
        {
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();

        }

        public void SetupDefaultMockApiBehavior(PlatformID platformID)
        {
            this.mockFixture.Setup(platformID);
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "Networking";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CPS", "CPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task NtworkingWorkloadServerExecutorExecutesAsExpectedOnStartInstructionsReceived(PlatformID platformID)
        {
            this.SetupDefaultMockApiBehavior(platformID);
 
            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            TestNetworkingWorkloadServerExecutor component = new TestNetworkingWorkloadServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Mock<object> mockSender = new Mock<object>();

            Item<Instructions> instructions = new Item<Instructions>(
                "mockId",
                new Instructions(InstructionsType.ClientServerStartExecution,
                new Dictionary<string, IConvertible>()
                {
                    ["Workload"] = "Test",
                    ["Scenario"] = "mock",
                }));

            JObject mockJobject = JObject.FromObject(instructions);

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            component.ServerCancellationSource = new CancellationTokenSource();
            VirtualClientRuntime.SetEventingApiOnline(true);
            component.OnClientInstructionsReceived.Invoke(mockSender.Object, mockJobject);

            // Not failing signies that component is found and created
        }

        private class TestNetworkingWorkloadServerExecutor : NetworkingWorkloadProxy
        {
            public TestNetworkingWorkloadServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public Action<object, JObject> OnClientInstructionsReceived => base.OnInstructionsReceived;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
