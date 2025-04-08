// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SockPerfExecutorTests2 : MockFixture
    {
        private DependencyPath mockPackage;
        private string apiClientId;
        private IPAddress ipAddress;

        private void SetupApiCalls()
        {
            this.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                 .Returns<string, IPAddress, int?>((id, ip, port) =>
                 {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.ApiClient.Object;
                });
        }

        private void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64, String role = ClientRole.Client)
        {
            this.Setup(platform, architecture, agentId: role == ClientRole.Client ? "ClientAgent" : "ServerAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));

            this.mockPackage = new DependencyPath("sockperf", this.GetPackagePath("sockperf"));
            this.SetupPackage(this.mockPackage);
            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.Parameters["PackageName"] = "sockperf";
            this.Parameters["Protocol"] = "TCP";

            string exampleResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "SockPerf", "SockPerfClientExample1.txt");

            this.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.SetupApiCalls();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenProtocolIsInvalid(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.Parameters["Protocol"] = ProtocolType.IP;

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                Assert.ThrowsAsync<NotSupportedException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenScenarioIsEmpty(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.Parameters[nameof(VirtualClientComponent.Scenario)] = "";

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }

        
        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorInitializesItsDependencyPackageAsExpected(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string expectedPackage = "sockperf";
            this.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPackage);

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                this.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorIntializeServerAPIClientForClientRoleOnMultiVMSetup(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                if (role == ClientRole.Client)
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
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void SockPerfExecutorThrowsIfAnUnsupportedRoleIsSupplied(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
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
            this.SetupTest(platformID, architecture, role);
            this.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task SockPerfExecutorExecutesTheExpectedLogicForTheServerRole(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!executor.IsSockPerfClientExecuted);
                Assert.IsTrue(executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        public async Task SockPerfExecutorExecutesTheExpectedLogicForTheClientRole(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestSockPerfExecutor executor = new TestSockPerfExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(executor.IsSockPerfClientExecuted);
                Assert.IsTrue(!executor.IsNetworkingWorkloadServerExecuted);
            }
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
