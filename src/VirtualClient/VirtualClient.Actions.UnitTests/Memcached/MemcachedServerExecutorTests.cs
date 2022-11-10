// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemcachedServerExecutorTests
    {
        private const string ExampleUsername = "my-username"; 
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath memcachedPath;
        private DependencyPath memtierPath;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.memcachedPath = new DependencyPath("memcached", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memcached"));
            this.memtierPath = new DependencyPath("memtier", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memtier"));
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name,
                ["Copies"] = "4",
                ["Bind"] = "1",
                ["Port"] = "6379",
                ["ServerItemMemoryMB"] = "64",
                ["Protocol"] = "memcache_text",
                ["Username"] = MemcachedServerExecutorTests.ExampleUsername
            };

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemcachedPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memcachedPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);
            this.SetupDefaultMockFileSystemBehavior();
        }

        [Test]
        [Ignore("Need fix: properly mock the system core count.")]
        public async Task MemcachedMemtierServerExecutorExecutesExpectedProcess()
        {
            int commandExecuted = 0;

            using TestMemcachedMemtierServerExecutor executor = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            List<string> expectedCommands = new List<string>()
            {
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} bash -c \"numactl -C 0 {this.memcachedPath.Path}/memcached -d -p 6379 -t 4 -m 64\"",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} bash -c \"numactl -C 1 {this.memcachedPath.Path}/memcached -d -p 6380 -t 4 -m 64\"",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} bash -c \"numactl -C 2 {this.memcachedPath.Path}/memcached -d -p 6381 -t 4 -m 64\"",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} bash -c \"numactl -C 3 {this.memcachedPath.Path}/memcached -d -p 6382 -t 4 -m 64\"",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} {this.memtierPath.Path}/memtier_benchmark --protocol=memcache_text --server localhost --port=6379 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} {this.memtierPath.Path}/memtier_benchmark --protocol=memcache_text --server localhost --port=6380 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} {this.memtierPath.Path}/memtier_benchmark --protocol=memcache_text --server localhost --port=6381 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo -u {MemcachedServerExecutorTests.ExampleUsername} {this.memtierPath.Path}/memtier_benchmark --protocol=memcache_text --server localhost --port=6382 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys"
           };

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }
                
                return this.fixture.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None);

            Assert.AreEqual(8, commandExecuted);
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
        }

        private class TestMemcachedMemtierServerExecutor : MemcachedServerExecutor
        {
            public TestMemcachedMemtierServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
