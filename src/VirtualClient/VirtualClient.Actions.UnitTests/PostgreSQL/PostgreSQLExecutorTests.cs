// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposal is taken care of in tear down.")]
    public class PostgreSQLExecutorTests
    {
        private MockFixture fixture;
        private IDictionary<string, IConvertible> parameters;
        private DependencyPath mockPath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.parameters = new Dictionary<string, IConvertible>
            {
                { nameof(PostgreSQLExecutor.PackageName), "postgresql" }
            };

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task PostgreSQLExecutorOnInitializationGetsExpectedPackageLocation()
        {
            int executed = 0;
            string expectedPackage = "postgresql";
            this.fixture.PackageManager.OnGetPackage(expectedPackage)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.mockPath);

            using TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters);
            await component.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.AreEqual(1, executed);
        }

        [Test]
        public void PostgreSQLExecutorOnInitializationThrowsWhenTheWorkloadPackageIsNotFound()
        {
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            using TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => component.OnInitialize(EventContext.None, CancellationToken.None));
            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
        }

        [Test]
        public void PostgreSQLExecutorThrowsIfAnUnsupportedRoleIsSupplied()
        {
            string agentId = $"{Environment.MachineName}-Other";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            using (TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesTheExpectedLogicWhenASpecificRoleIsNotDefined()
        {
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(component.IsPostgreSQLClientExecuted);
                Assert.IsTrue(component.IsPostgreSQLServerExecuted);
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesTheExpectedLogicForTheServerRole()
        {
            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
            using (TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters))
            {
                await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(!component.IsPostgreSQLClientExecuted);
                Assert.IsTrue(component.IsPostgreSQLServerExecuted);
            }
        }

        [Test]
        public async Task PostgreSQLExecutorExecutesTheExpectedLogicForTheClientRole()
        {
            TestPostgreSQLExecutor component = new TestPostgreSQLExecutor(this.fixture.Dependencies, this.parameters);
            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(component.IsPostgreSQLClientExecuted);
            Assert.IsTrue(!component.IsPostgreSQLServerExecuted);
        }

        protected class TestPostgreSQLExecutor : PostgreSQLExecutor
        {
            public TestPostgreSQLExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public bool IsPostgreSQLServerExecuted { get; set; } = false;

            public bool IsPostgreSQLClientExecuted { get; set; } = false;

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            // public Action<object, JObject> OnInstructionsReceivedExecutes => base.OnInstructionsReceived;

            protected override VirtualClientComponent CreateClientExecutor()
            {
                var mockTPCCClientExecutor = new MockPostgreSQLClientExecutor(this.Dependencies, this.Parameters);
                mockTPCCClientExecutor.OnExecuteAsync = () => this.IsPostgreSQLClientExecuted = true;

                return mockTPCCClientExecutor;
            }

            protected override VirtualClientComponent CreateServerExecutor()
            {
                var mockTPCCServerExecutor = new MockPostgreSQLServerExecutor(this.Dependencies, this.Parameters);
                mockTPCCServerExecutor.OnExecuteAsync = () => this.IsPostgreSQLServerExecuted = true;

                return mockTPCCServerExecutor;
            }
        }

        private class MockPostgreSQLServerExecutor : VirtualClientComponent
        {
            public MockPostgreSQLServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Action OnExecuteAsync { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.Run(() => this.OnExecuteAsync?.Invoke());
            }
        }

        private class MockPostgreSQLClientExecutor : VirtualClientComponent
        {
            public MockPostgreSQLClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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