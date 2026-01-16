// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
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
    public class CPSExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests2), "Examples", "CPS");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private IPAddress ipAddress;

        public void SetupApiCalls()
        {
            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns<string, IPAddress, int?>((id, ip, port) =>
                {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.mockFixture.ApiClient.Object;
                });
        }

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64, String role = ClientRole.Client)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture, agentId: role == ClientRole.Client ? "ClientAgent" : "ServerAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));

            this.mockFixture.Parameters["PackageName"] = "cps";

            this.mockPackage = new DependencyPath("cps", this.mockFixture.PlatformSpecifics.GetPackagePath("cps"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(CPSExecutorTests2.ExamplesDirectory, "CPS_Example_Results_Server.txt"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.SetupApiCalls();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64,ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenScenarioIsEmpty_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenScenarioIsEmpty_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task CPSExecutorInitializesItsDependencyPackageAsExpected_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string expectedPackage = "cps";
            this.mockFixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPackage);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                this.mockFixture.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorInitializesItsDependencyPackageAsExpected_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string expectedPackage = "cps";
            this.mockFixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPackage);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                this.mockFixture.PackageManager.Verify(d => d.GetPackageAsync(expectedPackage, It.IsAny<CancellationToken>()), Times.Once());
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorInitializeServerAPIClientForClientRoleOnMultiVMSetup_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorInitializeServerAPIClientForClientRoleOnMultiVMSetup_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void CPSExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void CPSExecutorThrowsIfAnUnsupportedRoleIsSupplied_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LayoutInvalid, exception.Reason);
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
            this.SetupTest(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LayoutInvalid, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void CPSExecutorThrowsWhenASpecificRoleIsNotDefined_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LayoutNotDefined, exception.Reason);
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
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheServerRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!executor.IsCPSClientExecuted);
                Assert.IsTrue(executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheServerRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!executor.IsCPSClientExecuted);
                Assert.IsTrue(executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheClientRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(executor.IsCPSClientExecuted);
                Assert.IsTrue(!executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        public async Task CPSExecutorExecutesTheExpectedLogicForTheClientRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestCPSExecutor executor = new TestCPSExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(executor.IsCPSClientExecuted);
                Assert.IsTrue(!executor.IsNetworkingWorkloadServerExecuted);
            }
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
