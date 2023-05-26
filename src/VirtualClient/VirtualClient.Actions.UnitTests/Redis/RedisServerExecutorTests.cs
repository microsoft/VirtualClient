// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.RedisExecutor;

    [TestFixture]
    [Category("Unit")]
    public class RedisServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockRedisPackage;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockRedisPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockRedisPackage.Name,
                ["CommandLine"] = "--protected-mode no --io-threads {ServerThreadCount} --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes",
                ["BindToCores"] = true,
                ["Port"] = 6379,
                ["ServerInstances"] = 1,
                ["ServerThreadCount"] = 4
            };

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("AnyName", "AnyDescription", 1, 4, 1, 0, true));

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockRedisPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            // Setup:
            // Server saves state once it is up and running.
            this.fixture.ApiClient.OnUpdateState<ServerState>(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK));
        }

        [Test]
        public async Task RedisServerExecutorConfirmsTheExpectedPackagesOnInitialization()
        {
            using (var component = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenBindingToCores()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    $"sudo bash -c \"numactl -C 0 {this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenBindingToCores_2_Server_Instances()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.ServerInstances)] = 2;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Server instance #1 bound to core 0 and running on port 6379
                    $"sudo bash -c \"numactl -C 0 {this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\"",

                    // Server instance #2 bound to core 1 and running on port 6380
                    $"sudo bash -c \"numactl -C 1 {this.mockRedisPackage.Path}/src/redis-server --port 6380 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcessWhenNotBindingToCores()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.BindToCores)] = false;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/src/redis-server\"",

                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    $"sudo bash -c \"{this.mockRedisPackage.Path}/src/redis-server --port 6379 --protected-mode no --io-threads 4 --maxmemory-policy noeviction --ignore-warnings ARM64-COW-BUG --save --daemonize yes\""
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestRedisServerExecutor : RedisServerExecutor
        {
            public TestRedisServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
