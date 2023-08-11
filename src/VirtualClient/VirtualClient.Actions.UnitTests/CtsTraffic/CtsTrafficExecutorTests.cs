// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;
    using static VirtualClient.Actions.CtsTrafficExecutor;

    [TestFixture]
    [Category("Unit")]
    public class CtsTrafficExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockCtsTrafficPackage;

        public void SetupDefaults(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockCtsTrafficPackage = new DependencyPath("ctstraffic", this.mockFixture.GetPackagePath("ctstraffic"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                ["PackageName"] = this.mockCtsTrafficPackage.Name,
                ["PrimaryPort"] = "4445",
                ["SecondaryPort"] = "4444",
                ["NumaNode"] = 0,
                ["BufferInBytes"] = 36654,
                ["Connections"] = 1,
                ["Iterations"] = 1,
                ["ServerExitLimit"] = 1
            };

            this.mockFixture.PackageManager.OnGetPackage("ctstraffic").ReturnsAsync(this.mockCtsTrafficPackage);

            this.mockFixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void CtsTrafficDatabaseStateObjectsAreJsonSerializable()
        {
            CtsTrafficServerState state = new CtsTrafficServerState();
            SerializationAssert.IsJsonSerializable(state);
        }

        [Test]
        public async Task CtsTrafficClientExecutorIntializesTheExpectedAPIClients_MultiVM_Environment()
        {
            this.SetupDefaults();
            using (var executor = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // Setup: API calls.
                this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(serverInstance.IPAddress, It.IsAny<IPAddress>(), It.IsAny<int?>()))
                   .Returns<string, IPAddress, int?>((id, ip, port) =>
                   {
                       Assert.IsTrue(id.Equals(serverInstance.IPAddress.ToString()));
                       Assert.AreEqual(ip, serverIPAddress);

                       return this.mockFixture.ApiClient.Object;
                   });

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CtsTrafficExecutorConfirmsTheExpectedWorkloadPackagesOnInitialization(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            using (var component = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                this.mockFixture.PackageManager.Verify(mgr => mgr.GetPackageAsync("ctstraffic", It.IsAny<CancellationToken>()));

                Assert.AreEqual(
                    this.mockFixture.ToPlatformSpecificPath(this.mockCtsTrafficPackage, platform, architecture).Path,
                    component.CtsTrafficPackagePath);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsWhenTheCtsTrafficPackageIsNotFound()
        {
            this.SetupDefaults();
            this.mockFixture.PackageManager.OnGetPackage("ctstraffic").ReturnsAsync(null as DependencyPath);

            using (var component = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                        () => component.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfAnUnsupportedRoleIsSupplied()
        {
            this.SetupDefaults();

            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (var component = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task CtsTrafficExecutorExecutesOnTheClientRoleSystem()
        {
            this.SetupDefaults();

            using (var component = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(component.IsClientExecuted);
                Assert.IsTrue(!component.IsServerExecuted);
            }
        }

        [Test]
        public async Task CtsTrafficExecutorExecutesOnTheServerRoleSystem()
        {
            this.SetupDefaults();

            // Make the current system look like it is performing the server role.
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns($"{Environment.MachineName}-Server");

            using (TestCtsTrafficExecutor component = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(!component.IsClientExecuted);
                Assert.IsTrue(component.IsServerExecuted);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfTheExpectedCtsTrafficExeDoesNotExist()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            using (var executor = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // ctsTraffic.exe not found.
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("ctsTraffic.exe")))).Returns(false);
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfTheExpectedProcessInNumaNodeExeDoesNotExist()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            using (var executor = new TestCtsTrafficExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // ctsTraffic.exe not found.
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("StartProcessInNumaNode.exe")))).Returns(false);
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        protected class TestCtsTrafficExecutor : CtsTrafficExecutor
        {
            public TestCtsTrafficExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public bool IsServerExecuted { get; set; } = false;

            public bool IsClientExecuted { get; set; } = false;

            public new string CtsTrafficPackagePath => base.CtsTrafficPackagePath;

            public new string CtsTrafficExe => base.CtsTrafficExe;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            protected override VirtualClientComponent CreateClientExecutor()
            {
                var mockTPCCClientExecutor = new TestCtsTrafficClientExecutor(this.Dependencies, this.Parameters);
                mockTPCCClientExecutor.OnExecuteAsync = () => this.IsClientExecuted = true;

                return mockTPCCClientExecutor;
            }

            protected override VirtualClientComponent CreateServerExecutor()
            {
                var mockTPCCServerExecutor = new TestCtsTrafficServerExecutor(this.Dependencies, this.Parameters);
                mockTPCCServerExecutor.OnExecuteAsync = () => this.IsServerExecuted = true;

                return mockTPCCServerExecutor;
            }
        }

        private class TestCtsTrafficServerExecutor : VirtualClientComponent
        {
            public TestCtsTrafficServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Action OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() => this.OnExecuteAsync?.Invoke());
            }
        }

        private class TestCtsTrafficClientExecutor : VirtualClientComponent
        {
            public TestCtsTrafficClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Action OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() => this.OnExecuteAsync?.Invoke());
            }
        }
    }
}