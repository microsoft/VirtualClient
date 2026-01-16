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
    public class NTttcpExecutorTests2
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(NTttcpExecutorTests2), "Examples", "NTttcp");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private IPAddress ipAddress;

        private void SetupApiCalls()
        {
            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns<string, IPAddress, int?>((id, ip, port) =>
                {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.mockFixture.ApiClient.Object;
                });
        }

        private void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64, String role = ClientRole.Client)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture, agentId: role == ClientRole.Client ? "ClientAgent" : "ServerAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));

            this.mockPackage = new DependencyPath("ntttcp", this.mockFixture.PlatformSpecifics.GetPackagePath("ntttcp"));
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters["PackageName"] = "ntttcp";
            this.mockFixture.Parameters["Protocol"] = ProtocolType.Tcp.ToString();

            string exampleResults = File.ReadAllText(this.mockFixture.Combine(NTttcpExecutorTests2.ExamplesDirectory, "ClientOutput.xml"));

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(exampleResults);

            this.SetupApiCalls();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void NTttcpExecutorThrowsOnInitializationWhenProtocolIsInvalid_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters["Protocol"] = ProtocolType.Unspecified;

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<NotSupportedException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public void NTttcpExecutorThrowsOnInitializationWhenProtocolIsInvalid_Win(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters["Protocol"] = ProtocolType.Unspecified;

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<NotSupportedException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public void NTttcpExecutorThrowsOnInitializationWhenScenarioIsEmpty_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorThrowsOnInitializationWhenScenarioIsEmpty_Win(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Parameters[nameof(VirtualClientComponent.Scenario)] = string.Empty;

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task NTttcpExecutorInitializesItsDependencyPackageAsExpected_Win(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string expectedPackage = "ntttcp";
            this.mockFixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPackage);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task NTttcpExecutorInitializesItsDependencyPackageAsExpected_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string expectedPackage = "ntttcp";
            this.mockFixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                })
                .ReturnsAsync(this.mockPackage);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task NTttcpExecutorInitializeServerAPIClientForClientRoleOnMultiVMSetup_Win(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public async Task NTttcpExecutorInitializeServerAPIClientForClientRoleOnMultiVMSetup_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Win(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.ResetPackages();

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.ResetPackages();

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorThrowsIfAnUnsupportedRoleIsSupplied_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorThrowsIfAnUnsupportedRoleIsSupplied_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorExecutesTheExpectedLogicWhenASpecificRoleIsNotDefined_Unix(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        public void NTttcpExecutorExecutesTheExpectedLogicWhenASpecificRoleIsNotDefined_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exception = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LayoutNotDefined, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Server)]
        public async Task NTttcpExecutorExecutesTheExpectedLogicForTheServerRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(!executor.IsNTttcpClientExecuted);
                Assert.IsTrue(executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Server)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Server)]
        public async Task NTttcpExecutorExecutesTheExpectedLogicForTheServerRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(!executor.IsNTttcpClientExecuted);
                Assert.IsTrue(executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Unix, Architecture.Arm64, ClientRole.Client)]
        public async Task NTttcpExecutorExecutesTheExpectedLogicForTheClientRole_Linux(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(executor.IsNTttcpClientExecuted);
                Assert.IsTrue(!executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, ClientRole.Client)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, ClientRole.Client)]
        public async Task NTttcpExecutorExecutesTheExpectedLogicForTheClientRole_Windows(PlatformID platformID, Architecture architecture, string role)
        {
            this.SetupTest(platformID, architecture, role);

            using (TestNTttcpExecutor executor = new TestNTttcpExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(executor.IsNTttcpClientExecuted);
                Assert.IsTrue(!executor.IsNetworkingWorkloadServerExecuted);
            }
        }

        protected class TestNTttcpExecutor : NTttcpExecutor2
        {
            public TestNTttcpExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                  : base(dependencies, parameters)
            {
            }

            public bool IsNetworkingWorkloadServerExecuted { get; set; } = false;

            public bool IsNTttcpClientExecuted { get; set; } = false;

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
                var mockNTttcpClientExecutor = new MockNTttcpClientExecutor(this.Dependencies, this.Parameters);
                mockNTttcpClientExecutor.OnExecuteAsync = () =>
                {
                    this.IsNTttcpClientExecuted = true;
                    return true;
                };
                return mockNTttcpClientExecutor;
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

        private class MockNTttcpClientExecutor : VirtualClientComponent
        {
            public MockNTttcpClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
