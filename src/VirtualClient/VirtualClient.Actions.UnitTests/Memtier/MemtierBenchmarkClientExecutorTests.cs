// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.MemcachedExecutor;

    [TestFixture]
    [Category("Unit")]
    public class MemtierBenchmarkClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
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
                ["CommandLine"] = "--protocol memcache_text --threads 8 --clients 32 --ratio 1:1 --data-size 32 --pipeline 100 --key-minimum 1 --key-maximum 10000000 --key-prefix sm --key-pattern R:R",
                ["ClientInstances"] = 1,
                ["Duration"] = "00:03:00"
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"Memtier\MemcachedResults_1.txt"));

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            // Setup:
            // A single Memcached server instance running on port 6379.
            this.fixture.ApiClient.OnGetState(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, new Item<ServerState>(nameof(ServerState), new ServerState(new int[] { 6379 }))));

            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) => this.fixture.ApiClient.Object);

            this.fixture.ApiClient.OnGetHeartbeat()
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorOnInitializationGetsTheExpectedPackage()
        {
            using (var component = new TestMemtierBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Run the Memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}"
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
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommandsWhenAUsernameIsDefined()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.Username)] = "testuser";

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Run the Memtier benchmark. Values based on the default parameter values set at the top
                    $"sudo -u testuser {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}"
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
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands_2_Client_Instances()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                // 2 client instances running in-parallel to target the server.
                executor.Parameters[nameof(executor.ClientInstances)] = 2;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}",

                     // Client instance #2
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}"
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
        public async Task MemtierBenchmarkClientExecutorExecutesExpectedCommands_4_Client_Instances()
        {
            using (var executor = new TestMemtierBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                // 4 client instances running in-parallel to target the server.
                executor.Parameters[nameof(executor.ClientInstances)] = 4;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/memtier_benchmark\"",

                    // Client instance #1
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}",

                     // Client instance #2
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}",

                    // Client instance #3
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}",

                     // Client instance #4
                    $"sudo {this.mockPackage.Path}/memtier_benchmark --server 1.2.3.5 --port 6379 {executor.CommandLine} --test-time {executor.Duration.TotalSeconds}"
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

        private class TestMemtierBenchmarkClientExecutor : MemtierBenchmarkClientExecutor
        {
            public TestMemtierBenchmarkClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
