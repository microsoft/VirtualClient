using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class MemcachedExecutorTests
    {
        private MockFixture fixture;
        private IDictionary<string, IConvertible> parameters;
        private DependencyPath mockPath;
        private DependencyPath memcachedPath;
        private DependencyPath memtierPath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.memcachedPath = new DependencyPath("memcached", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memcached"));
            this.memtierPath = new DependencyPath("memtier", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memtier"));
            this.parameters = new Dictionary<string, IConvertible>
            {
                { nameof(MemcachedExecutor.PackageName), "Memcached" },
                { nameof(MemcachedExecutor.Copies),"1" }
            };

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemcachedPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memcachedPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void MemcachedMemtierExecutorThrowsOnUnsupportedPlatformAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT);

            using (TestMemcachedMemtierExecutor memcachedMemtierExecutor = new TestMemcachedMemtierExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => memcachedMemtierExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
            }
        }

        [Test]
        public void MemcachedMemtierExecutorThrowsOnUnsupportedDistroAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestMemcachedMemtierExecutor memcachedMemtierExecutor = new TestMemcachedMemtierExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => memcachedMemtierExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task MemcachedMemtierExecutorOnInitializationGetsExpectedPackagesLocationOnServerRole()
        {
            int executed = 0;

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            string expectedPackage1 = "MemcachedPackage";
            string expectedPackage2 = "MemtierPackage";

            this.fixture.PackageManager.OnGetPackage(expectedPackage1)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage1, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.memcachedPath );

            this.fixture.PackageManager.OnGetPackage(expectedPackage2)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage2, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.memtierPath);

            using TestMemcachedMemtierExecutor component = new TestMemcachedMemtierExecutor(this.fixture.Dependencies, this.parameters);
            await component.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.AreEqual(2, executed);
        }

        [Test]
        public async Task MemcachedMemtierExecutorOnInitializationGetsExpectedPackagesLocationOnClientRole()
        {
            int executed = 0;

            string agentId = $"{Environment.MachineName}-Client";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            string expectedPackage2 = "MemtierPackage";

            this.fixture.PackageManager.OnGetPackage(expectedPackage2)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage2, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.memtierPath);

            using TestMemcachedMemtierExecutor component = new TestMemcachedMemtierExecutor(this.fixture.Dependencies, this.parameters);
            await component.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.AreEqual(1, executed);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.fixture.Setup(platformID);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;
        }

        private class TestMemcachedMemtierExecutor : MemcachedExecutor
        {
            public TestMemcachedMemtierExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
