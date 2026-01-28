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
    public class CtsTrafficExecutorTests : MockFixture
    {
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);
            this.mockPackage = new DependencyPath("ctstraffic", this.GetPackagePath("ctstraffic"));

            this.Parameters = new Dictionary<string, IConvertible>
            {
                ["PackageName"] = this.mockPackage.Name,
                ["PrimaryPort"] = "4445",
                ["SecondaryPort"] = "4444",
                ["NumaNode"] = 0,
                ["BufferInBytes"] = 36654,
                ["Connections"] = 1,
                ["Iterations"] = 1,
                ["ServerExitLimit"] = 1
            };

            this.SetupPackage(this.mockPackage);

            this.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void CtsTrafficDatabaseStateObjectsAreJsonSerializable()
        {
            CtsTrafficServerState state = new CtsTrafficServerState();
            SerializationAssert.IsJsonSerializable(state);
        }

        [Test]
        public async Task CtsTrafficClientExecutorInitializesTheExpectedAPIClients_MultiVM_Environment()
        {
            this.SetupTest();
            using (var executor = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // Setup: API calls.
                this.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(serverInstance.IPAddress, It.IsAny<IPAddress>(), It.IsAny<int?>()))
                   .Returns<string, IPAddress, int?>((id, ip, port) =>
                   {
                       Assert.IsTrue(id.Equals(serverInstance.IPAddress.ToString()));
                       Assert.AreEqual(ip, serverIPAddress);

                       return this.ApiClient.Object;
                   });

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CtsTrafficExecutorConfirmsTheExpectedWorkloadPackagesOnInitialization(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            using (var component = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                this.PackageManager.Verify(mgr => mgr.GetPackageAsync("ctstraffic", It.IsAny<CancellationToken>()));

                Assert.AreEqual(
                    this.ToPlatformSpecificPath(this.mockPackage, platform, architecture).Path,
                    component.CtsTrafficPackagePath);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsWhenTheCtsTrafficPackageIsNotFound()
        {
            this.SetupTest();
            this.PackageManager.OnGetPackage("ctstraffic").ReturnsAsync(null as DependencyPath);

            using (var component = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                        () => component.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfAnUnsupportedRoleIsSupplied()
        {
            this.SetupTest();

            string agentId = $"{Environment.MachineName}-Other";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (var component = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task CtsTrafficExecutorExecutesOnTheClientRoleSystem()
        {
            this.SetupTest();

            using (var component = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(component.IsClientExecuted);
                Assert.IsTrue(!component.IsServerExecuted);
            }
        }

        [Test]
        public async Task CtsTrafficExecutorExecutesOnTheServerRoleSystem()
        {
            this.SetupTest();

            // Make the current system look like it is performing the server role.
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns($"{Environment.MachineName}-Server");

            using (TestCtsTrafficExecutor component = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(!component.IsClientExecuted);
                Assert.IsTrue(component.IsServerExecuted);
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfTheExpectedCtsTrafficExeDoesNotExist()
        {
            this.SetupTest(PlatformID.Win32NT);
            using (var executor = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                // ctsTraffic.exe not found.
                this.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("ctsTraffic.exe")))).Returns(false);
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void CtsTrafficExecutorThrowsIfTheExpectedProcessInNumaNodeExeDoesNotExist()
        {
            this.SetupTest(PlatformID.Win32NT);
            using (var executor = new TestCtsTrafficExecutor(this.Dependencies, this.Parameters))
            {
                // ctsTraffic.exe not found.
                this.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith("StartProcessInNumaNode.exe")))).Returns(false);
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