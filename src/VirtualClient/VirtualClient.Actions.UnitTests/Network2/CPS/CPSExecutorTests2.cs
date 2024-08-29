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
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class CPSExecutorTests2
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;
        private string apiClientId;
        private IPAddress ipAddress;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();            
        }

        private void SetupDefaultMockApiBehavior()
        {
            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns<string, IPAddress, int?>((id, ip, port) =>
                {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.fixture.ApiClient.Object;
                });
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64, String role = ClientRole.Client)
        {
            this.fixture.Setup(platform, architecture, agentId: role == ClientRole.Client ? "ClientAgent" : "ServerAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));

            this.mockPath = new DependencyPath("NetworkingWorkload", this.fixture.PlatformSpecifics.GetPackagePath("networkingworkload"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Parameters["PackageName"] = "Networking";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Network", currentDirectory);
            string resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, "Examples", "CPS", "CPS_Example_Results_Server.txt");
            string results = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            this.SetupDefaultMockApiBehavior();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64,ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenScenarioIsEmpty_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenScenarioIsEmpty_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorInitializesItsDependencyPackageAsExpected_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string expectedPackage = "Networking";
            this.fixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPath);

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await component.InitializeAsync(EventContext.None, CancellationToken.None);

            this.fixture.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorInitializesItsDependencyPackageAsExpected_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string expectedPackage = "Networking";
            this.fixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPath);

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await component.InitializeAsync(EventContext.None, CancellationToken.None);

            this.fixture.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorIntializeServerAPIClientForClientRoleOnMultiVMSetup_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            using TestCPSExecutor executor = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);

            ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
            IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

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
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorIntializeServerAPIClientForClientRoleOnMultiVMSetup_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            using TestCPSExecutor executor = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None);

            ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
            IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

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
        public void CPSExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.InitializeAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsIfAnUnsupportedRoleIsSupplied_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, exception.Reason);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsIfAnUnsupportedRoleIsSupplied_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
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
        public void CPSExecutorThrowsWhenASpecificRoleIsNotDefined_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsWhenASpecificRoleIsNotDefined_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheServerRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsCPSClientExecuted);
                Assert.IsTrue(component.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheServerRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            using (TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsCPSClientExecuted);
                Assert.IsTrue(component.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheClientRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(component.IsCPSClientExecuted);
            Assert.IsTrue(!component.IsNetworkingWorkloadServerExecuted);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheClientRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupDefaultMockBehavior(platformID, architecture, role);

            TestCPSExecutor component = new TestCPSExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(component.IsCPSClientExecuted);
            Assert.IsTrue(!component.IsNetworkingWorkloadServerExecuted);
        }

        private class TestCPSExecutor : CPSExecutor2
        {
            public TestCPSExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public bool IsNetworkingWorkloadServerExecuted { get; set; } = false;

            public bool IsCPSClientExecuted { get; set; } = false;

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
                var mockCPSClientExecutor = new MockCPSClientExecutor(this.Dependencies, this.Parameters);
                mockCPSClientExecutor.OnExecuteAsync = () =>
                {
                    this.IsCPSClientExecuted = true;
                    return true;
                };
                return mockCPSClientExecutor;
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

        private class MockCPSClientExecutor : VirtualClientComponent
        {
            public MockCPSClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
