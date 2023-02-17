// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemcachedServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockMemcachedPackage;
        private DependencyPath mockBenchmarkPackage;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockMemcachedPackage = new DependencyPath("memcached", this.fixture.GetPackagePath("memcached"));
            this.mockBenchmarkPackage = new DependencyPath("memtier", this.fixture.GetPackagePath("memtier"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Memtier_Scenario",
                ["PackageName"] = this.mockMemcachedPackage.Name,
                ["BenchmarkPackageName"] = this.mockBenchmarkPackage.Name,
                ["Bind"] = 1,
                ["Port"] = 6379,
                ["ServerMemoryCacheSizeInMB"] = 64,
                ["Protocol"] = "memcache_text",
                ["Username"] = "testuser"
            };

            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId)
                .Returns($"{Environment.MachineName}-Server");

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockMemcachedPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockMemcachedPackage);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockBenchmarkPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockBenchmarkPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
        }


        [Test]
        public async Task MemcachedMemtierServerExecutorOnInitializationGetsExpectedPackagesLocationOnServerRole()
        {
            using (var component = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);

                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockMemcachedPackage.Name, It.IsAny<CancellationToken>()));
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockBenchmarkPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task MemcachedMemtierServerExecutorExecutesExpectedProcess()
        {
            using (var executor = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the memcached server toolset executable
                    $"sudo chmod +x \"{this.mockMemcachedPackage.Path}/linux-x64/memcached\"",

                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockBenchmarkPackage.Path}/linux-x64/memtier_benchmark\"",
               };

                // The default behavior is to run a server copy per logical core on the system.
                for (int coreNum = 0; coreNum < Environment.ProcessorCount; coreNum++)
                {
                    // Start the server binded to the logical core. Values based on the parameters set at the top.
                    expectedCommands.Add(
                        $"sudo -u testuser bash -c \"numactl -C {coreNum} {this.mockMemcachedPackage.Path}/linux-x64/memcached -d -p {executor.Port + coreNum} -t 4 -m 64\"");

                    // Warmup the server. Values based on the parameters set at the top.
                    expectedCommands.Add(
                        $"sudo -u testuser {this.mockBenchmarkPackage.Path}/linux-x64/memtier_benchmark --protocol=memcache_text --server localhost --port={executor.Port + coreNum} -c 1 -t 1 " +
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

        private class TestMemcachedMemtierServerExecutor : MemcachedServerExecutor
        {
            public TestMemcachedMemtierServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
