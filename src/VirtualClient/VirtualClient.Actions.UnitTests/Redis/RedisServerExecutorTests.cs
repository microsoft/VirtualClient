// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using VirtualClient.Common.Contracts;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using AutoFixture;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using System.Runtime.InteropServices;
    using System.IO;
    using Polly;
    using System.Net.Http;

    [TestFixture]
    [Category("Unit")]
    public class RedisServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath redisPath;
        private DependencyPath memtierPath;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.redisPath = new DependencyPath("redis", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "redis"));
            this.memtierPath = new DependencyPath("memtier", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memtier"));
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name,
                ["Copies"] = "4",
                ["bind"] = "1",
                ["Port"] = "6379"
            };

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("RedisPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(redisPath);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);
            this.SetupDefaultMockFileSystemBehavior();
        }

        [Test]
        [Ignore("Need fix: properly mock the system core count.")]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcess()
        {
            int commandExecuted = 0;

            using TestRedisMemtierServerExecutor executor = new TestRedisMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, PlatformID.Unix, Architecture.X64).Path;

            List<string> expectedCommands = new List<string>()
            {
                $"sudo pkill -f redis-server",
                $"sudo bash -c \"numactl -C 0 {this.fixture.PlatformSpecifics.Combine(this.redisPath.Path,"src","redis-server")} --port 6379 --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"",
                $"sudo bash -c \"numactl -C 1 {this.fixture.PlatformSpecifics.Combine(this.redisPath.Path,"src","redis-server")} --port 6380 --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"",
                $"sudo bash -c \"numactl -C 2 {this.fixture.PlatformSpecifics.Combine(this.redisPath.Path,"src","redis-server")} --port 6381 --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"",
                $"sudo bash -c \"numactl -C 3 {this.fixture.PlatformSpecifics.Combine(this.redisPath.Path,"src","redis-server")} --port 6382 --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"",
                $"sudo {this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path,"memtier_benchmark")} --protocol=redis --server localhost --port=6379 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo {this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path,"memtier_benchmark")} --protocol=redis --server localhost --port=6380 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo {this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path,"memtier_benchmark")} --protocol=redis --server localhost --port=6381 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys",
                $"sudo {this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path,"memtier_benchmark")} --protocol=redis --server localhost --port=6382 -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys"
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

            Assert.AreEqual(9, commandExecuted);
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
        }

        private class TestRedisMemtierServerExecutor : RedisServerExecutor
        {
            public TestRedisMemtierServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
