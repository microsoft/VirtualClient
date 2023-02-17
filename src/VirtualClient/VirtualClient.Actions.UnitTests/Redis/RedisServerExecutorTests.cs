// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RedisServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockRedisPackage;
        private DependencyPath mockBenchmarkPackage;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockRedisPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));
            this.mockBenchmarkPackage = new DependencyPath("memtier", this.fixture.GetPackagePath("memtier"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockRedisPackage.Name,
                ["BenchmarkPackageName"] = this.mockBenchmarkPackage.Name,
                ["Copies"] = 4,
                ["bind"] = 1,
                ["Port"] = 6379
            };

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockRedisPackage);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockBenchmarkPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockBenchmarkPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task RedisServerExecutorConfirmsTheExpectedPackagesOnInitialization()
        {
            using (var component = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockRedisPackage.Name, It.IsAny<CancellationToken>()));
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockBenchmarkPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task RedisMemtierServerExecutorExecutesExpectedProcess()
        {
            using (var executor = new TestRedisServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the Redis server toolset executable
                    $"sudo chmod +x \"{this.mockRedisPackage.Path}/linux-x64/src/redis-server\"",

                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockBenchmarkPackage.Path}/linux-x64/memtier_benchmark\"",
               };

                // The default behavior is to run a server copy per logical core on the system.
                for (int coreNum = 0; coreNum < Environment.ProcessorCount; coreNum++)
                {
                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    expectedCommands.Add(
                        $"sudo bash -c \"numactl -C {coreNum} {this.mockRedisPackage.Path}/linux-x64/src/redis-server --port {executor.Port + coreNum} " +
                        $"--protected-mode no --ignore-warnings ARM64-COW-BUG --save --io-threads 4 --maxmemory-policy noeviction\"");

                    // Warmup the server. Values based on the parameters set at the top.
                    expectedCommands.Add(
                        $"sudo {this.mockBenchmarkPackage.Path}/linux-x64/memtier_benchmark --protocol=redis --server localhost --port={executor.Port + coreNum} -c 1 -t 1 " +
                        $"--pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys");
                }

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
