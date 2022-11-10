using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoFixture;
using System.Threading.Tasks;
using VirtualClient.Contracts;
using Moq;
using System.Threading;
using VirtualClient.Common.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class RedisExecutorTests
    {
        private MockFixture fixture;
        private IDictionary<string, IConvertible> parameters;
        private DependencyPath mockPath;
        private DependencyPath redisPath;
        private DependencyPath memtierPath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.redisPath = new DependencyPath("redis", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "redis"));
            this.memtierPath = new DependencyPath("memtier", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memtier"));
            this.parameters = new Dictionary<string, IConvertible>
            {
                { nameof(RedisExecutor.PackageName), "redismemtier" },
                {nameof(RedisExecutor.Copies),"1" }
            };

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("RedisPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(redisPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);
            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void RedisMemtierExecutorThrowsOnUnsupportedPlatformAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT);

            using (TestRedisMemtierExecutor redisMemtierExecutor = new TestRedisMemtierExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => redisMemtierExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
            }
        }

        [Test]
        public void RedisMemtierExecutorThrowsOnUnsupportedDistroAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestRedisMemtierExecutor redisMemtierExecutor = new TestRedisMemtierExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => redisMemtierExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task RedisMemtierExecutorOnInitializationGetsExpectedPackagesLocationOnServerRole()
        {
            int executed = 0;

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            string expectedPackage1 = "RedisPackage";
            string expectedPackage2 = "MemtierPackage";

            this.fixture.PackageManager.OnGetPackage(expectedPackage1)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage1, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.redisPath);

            this.fixture.PackageManager.OnGetPackage(expectedPackage2)
                .Callback<string, CancellationToken>((actualPackage, token) =>
                {
                    Assert.AreEqual(expectedPackage2, actualPackage);
                    executed++;
                })
                .ReturnsAsync(this.memtierPath);

            using TestRedisMemtierExecutor component = new TestRedisMemtierExecutor(this.fixture.Dependencies, this.parameters);
            await component.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.AreEqual(2, executed);
        }

        [Test]
        public async Task RedisMemtierExecutorOnInitializationGetsExpectedPackagesLocationOnClientRole()
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

            using TestRedisMemtierExecutor component = new TestRedisMemtierExecutor(this.fixture.Dependencies, this.parameters);
            await component.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.AreEqual(1, executed);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.fixture.Setup(platformID);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;

        }
        private class TestRedisMemtierExecutor : RedisExecutor
        {
            public TestRedisMemtierExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
