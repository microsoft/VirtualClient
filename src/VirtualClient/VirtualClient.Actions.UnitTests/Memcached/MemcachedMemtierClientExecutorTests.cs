// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.MemcachedExecutor;

    [TestFixture]
    [Category("Unit")]
    public class MemcachedMemtierClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private ClientInstance clientInstance;
        private string apiClientId;
        private string results;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("memtier", this.fixture.GetPackagePath("memtier"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Memtier_Scenario",
                ["PackageName"] = this.mockPackage.Name,
                ["Bind"] = 1,
                ["ClientsPerThread"] = 2,
                ["ThreadCount"] = 1,
                ["PipelineDepth"] = 32,
                ["RunCount"] = 1,
                ["Duration"] = "00:03:00",
                ["Port"] = 6379,
                ["Protocol"] = "memcache_text",
                ["Username"] = "testuser"
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.results = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, @"Examples\Memcached\MemcachedExample.txt"));

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            var state = new Item<ServerState>(nameof(ServerState), new ServerState
            {
                ServerCopies = 1
            });

            this.fixture.ApiClient.OnGetState(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, state));

            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) => this.fixture.ApiClient.Object);

            this.fixture.ApiClient.OnGetHeartbeat()
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task MemcachedMemtierClientExecutorOnInitializationGetsTheExpectedPackage()
        {
            using (var component = new TestMemcachedMemtierClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task MemcachedMemtierClientExecutorExecutesExpectedCommands()
        {
            using (var executor = new TestMemcachedMemtierClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/linux-x64/memtier_benchmark\"",

                    // Run the memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo -u testuser {this.mockPackage.Path}/linux-x64/memtier_benchmark --server 1.2.3.5 --port 6379 --protocol memcache_text --clients 2 " +
                    $"--threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 180 --print-percentiles 50,90,95,99,99.9 " +
                    $"--random-data",
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.fixture.Process.StandardOutput.Append(this.results);

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemcachedMemtierClientExecutorUsesThePortDefinedToCommunicateWithTheServer()
        {
            using (var executor = new TestMemcachedMemtierClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters["Port"] = 1234;

                List<string> expectedCommands = new List<string>()
                {
                    // Run the memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo -u testuser {this.mockPackage.Path}/linux-x64/memtier_benchmark --server 1.2.3.5 --port 1234 --protocol memcache_text --clients 2 " +
                    $"--threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 180 --print-percentiles 50,90,95,99,99.9 " +
                    $"--random-data",
                };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    this.fixture.Process.StandardOutput.Append(this.results);

                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestMemcachedMemtierClientExecutor : MemcachedMemtierClientExecutor
        {
            public TestMemcachedMemtierClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
                this.ClientFlowRetryPolicy = Policy.NoOpAsync();
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

        }
    }
}
