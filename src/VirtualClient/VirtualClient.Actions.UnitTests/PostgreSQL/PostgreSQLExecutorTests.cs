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
    using static VirtualClient.Actions.PostgreSQLExecutor;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPostgreSqlPackage;
        private DependencyPath mockHammerDbPackage;

        public void SetupDefaults(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockPostgreSqlPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"), metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-linux-arm64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
                [$"{PackageMetadata.InstallationPath}-win-arm64"] = "C:\\Program Files\\PostgreSQL\\14"
            });

            this.mockHammerDbPackage = new DependencyPath("hammerdb", this.mockFixture.GetPackagePath("hammerdb"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                ["PackageName"] = this.mockPostgreSqlPackage.Name,
                ["HammerDBPackageName"] = this.mockHammerDbPackage.Name,
                ["Benchmark"] = "tpcc",
                ["DatabaseName"] = "tpcc",
                ["ReuseDatabase"] = true,
                ["Username"] = "anyuser",
                ["Password"] = "anyvalue",
                ["UserCount"] = 100,
                ["WarehouseCount"] = 100,
                ["Port"] = 5432
            };

            this.mockFixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(this.mockPostgreSqlPackage);
            this.mockFixture.PackageManager.OnGetPackage("hammerdb").ReturnsAsync(this.mockHammerDbPackage);

            this.mockFixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void PostgreSQLDatabaseStateObjectsAreJsonSerializable()
        {
            PostgreSQLServerState state = new PostgreSQLServerState();
            SerializationAssert.IsJsonSerializable(state);
        }

        [Test]
        public async Task PostgreSQLClientExecutorIntializesTheExpectedAPIClients_SingleVM_Environment()
        {
            this.SetupDefaults();
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();

            using (var executor = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // Setup: API calls.
                this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                   .Returns<string, IPAddress, int?>((id, ip, port) =>
                   {
                       Assert.IsTrue(id.Equals(IPAddress.Loopback.ToString()));
                       Assert.AreEqual(IPAddress.Loopback, ip);

                       return this.mockFixture.ApiClient.Object;
                   });

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorIntializesTheExpectedAPIClients_MultiVM_Environment()
        {
            this.SetupDefaults();
            using (var executor = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLExecutorConfirmsTheExpectedWorkloadPackagesOnInitialization(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                this.mockFixture.PackageManager.Verify(mgr => mgr.GetPackageAsync("postgresql", It.IsAny<CancellationToken>()));
                this.mockFixture.PackageManager.Verify(mgr => mgr.GetPackageAsync("hammerdb", It.IsAny<CancellationToken>()));

                Assert.AreEqual(
                    this.mockFixture.ToPlatformSpecificPath(this.mockPostgreSqlPackage, platform, architecture).Path,
                    component.PostgreSqlPackagePath);

                Assert.AreEqual(
                    this.mockFixture.ToPlatformSpecificPath(this.mockHammerDbPackage, platform, architecture).Path,
                    component.HammerDBPackagePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task PostgreSQLExecutorInitializesWithTheExpectedPostgreSQLInstallationLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                string expectedPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-{PlatformSpecifics.GetPlatformArchitectureName(platform, architecture)}"]
                    .ToString();

                Assert.AreEqual(expectedPath, component.PostgreSqlInstallationPath);
            }
        }

        [Test]
        public void PostgreSQLExecutorThrowsWhenThePostgreSQLPackageIsNotFound()
        {
            this.SetupDefaults();
            this.mockFixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(null as DependencyPath);

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                        () => component.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        public void PostgreSQLExecutorThrowsWhenTheHammerDBPackageIsNotFound()
        {
            this.SetupDefaults();
            this.mockFixture.PackageManager.OnGetPackage("hammerdb").ReturnsAsync(null as DependencyPath);

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                        () => component.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        public void PostgreSQLExecutorThrowsIfAnUnsupportedRoleIsSupplied()
        {
            this.SetupDefaults();

            string agentId = $"{Environment.MachineName}-Other";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesBothClientAndServerRolesWhenALayoutIsNotDefined()
        {
            this.SetupDefaults();
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(component.IsClientExecuted);
                Assert.IsTrue(component.IsServerExecuted);
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesOnTheClientRoleSystem()
        {
            this.SetupDefaults();

            using (var component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(component.IsClientExecuted);
                Assert.IsTrue(!component.IsServerExecuted);
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesOnTheServerRoleSystem()
        {
            this.SetupDefaults();

            // Make the current system look like it is performing the server role.
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns($"{Environment.MachineName}-Server");

            using (TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(!component.IsClientExecuted);
                Assert.IsTrue(component.IsServerExecuted);
            }
        }

        protected class TestPostgreSQLExecutor : PostgreSQLExecutor
        {
            public TestPostgreSQLExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public bool IsServerExecuted { get; set; } = false;

            public bool IsClientExecuted { get; set; } = false;

            public new string HammerDBPackagePath => base.HammerDBPackagePath;

            public new string PostgreSqlPackagePath => base.PostgreSqlPackagePath;

            public new string PostgreSqlInstallationPath => base.PostgreSqlInstallationPath;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            protected override VirtualClientComponent CreateClientExecutor()
            {
                var mockTPCCClientExecutor = new TestPostgreSQLClientExecutor(this.Dependencies, this.Parameters);
                mockTPCCClientExecutor.OnExecuteAsync = () => this.IsClientExecuted = true;

                return mockTPCCClientExecutor;
            }

            protected override VirtualClientComponent CreateServerExecutor()
            {
                var mockTPCCServerExecutor = new TestPostgreSQLServerExecutor(this.Dependencies, this.Parameters);
                mockTPCCServerExecutor.OnExecuteAsync = () => this.IsServerExecuted = true;

                return mockTPCCServerExecutor;
            }
        }

        private class TestPostgreSQLServerExecutor : VirtualClientComponent
        {
            public TestPostgreSQLServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Action OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() => this.OnExecuteAsync?.Invoke());
            }
        }

        private class TestPostgreSQLClientExecutor : VirtualClientComponent
        {
            public TestPostgreSQLClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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