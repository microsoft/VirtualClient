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
    using System.Linq;
    using System.Net.Sockets;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfExecutorTests2
    {
        private MockFixture mockFixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;
        private string apiClientId;
        private IPAddress ipAddress;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();            
        }

        private void SetupDefaultMockApiBehavior()
        {
            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                 .Returns<string, IPAddress, int?>((id, ip, port) =>
                 {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.mockFixture.ApiClient.Object;
                });
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64, String role = ClientRole.Client)
        {
            this.mockFixture.Setup(platform, architecture, agentId: role == ClientRole.Client ? "ClientAgent" : "ServerAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));
            this.mockPath = new DependencyPath("NetworkingWorkload", this.mockFixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "Networking";
            this.mockFixture.Parameters["Protocol"] = "TCP";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Network", currentDirectory);
            string resultsPath = this.mockFixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, "Examples", "SockPerf", "SockPerfClientExample1.txt");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupDefaultMockApiBehavior();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenProtocolIsInvalid(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.mockFixture.Parameters["Protocol"] = ProtocolType.IP;

            using TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.ThrowsAsync<NotSupportedException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenScenarioIsEmpty(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.mockFixture.Parameters[nameof(VirtualClientComponent.Scenario)] = "";

            using TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
        }

        
        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorInitializesItsDependencyPackageAsExpected(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string expectedPackage = "Networking";
            this.mockFixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPath);

            using TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.InitializeAsync(EventContext.None, CancellationToken.None);

            this.mockFixture.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorIntializeServerAPIClientForClientRoleOnMultiVMSetup(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            using TestSockPerfExecutor executor = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);

            ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
            IPAddress.TryParse(serverInstance.PrivateIPAddress, out IPAddress serverIPAddress);

            if(role == ClientRole.Client)
            {
                Assert.IsTrue(this.apiClientId.Equals(serverIPAddress.ToString()));
                Assert.AreEqual(this.ipAddress, serverIPAddress);
            }
            else
            {
                Assert.IsTrue(this.apiClientId.Equals(IPAddress.Loopback.ToString()));
                Assert.AreEqual(this.ipAddress, IPAddress.Loopback);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsIfAnUnsupportedRoleIsSupplied(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorExecutesTheExpectedLogicWhenASpecificRoleIsNotDefined(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorExecutesTheExpectedLogicForTheServerRole(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            using (TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsSockPerfClientExecuted);
                Assert.IsTrue(component.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        public async Task SockPerfExecutorExecutesTheExpectedLogicForTheClientRole(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            TestSockPerfExecutor component = new TestSockPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(component.IsSockPerfClientExecuted);
            Assert.IsTrue(!component.IsNetworkingWorkloadServerExecuted);
        }

        private class TestSockPerfExecutor : SockPerfExecutor2
        {
            public TestSockPerfExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public bool IsNetworkingWorkloadServerExecuted { get; set; } = false;

            public bool IsSockPerfClientExecuted { get; set; } = false;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }

            protected override VirtualClientComponent CreateWorkloadClient()
            {
                var mockSockPerfClientExecutor = new MockSockPerfClientExecutor(this.Dependencies, this.Parameters);
                mockSockPerfClientExecutor.OnExecuteAsync = () =>
                {
                    this.IsSockPerfClientExecuted = true;
                    return true;
                };
                return mockSockPerfClientExecutor;
            }

            protected override VirtualClientComponent CreateWorkloadServer()
            {
                var mockNetworkingWorkloadServerExecutor = new MockNetworkingWorkloadServerExecutor(this.Dependencies, this.Parameters);
                mockNetworkingWorkloadServerExecutor.OnExecuteAsync = () =>
                {
                    this.IsNetworkingWorkloadServerExecuted = true;
                    return true;
                };
                return mockNetworkingWorkloadServerExecutor;
            }
        }

        private class MockNetworkingWorkloadServerExecutor : VirtualClientComponent
        {
            public MockNetworkingWorkloadServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<bool> OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() =>
                {
                    this.OnExecuteAsync?.Invoke();
                });
            }
        }

        private class MockSockPerfClientExecutor : VirtualClientComponent
        {
            public MockSockPerfClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<bool> OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() =>
                {
                    this.OnExecuteAsync?.Invoke();
                });
            }
        }
    }
}
