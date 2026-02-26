// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RedisBenchmarkClientExecutorTests
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
            this.mockPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Redis_Scenario",
                ["PackageName"] = this.mockPackage.Name,
                ["CommandLine"] = "-c 8 -d 32 -n 10000 -P 4 -q --csv",
                ["ClientInstances"] = 1,
                ["PipelineDepth"] = 32
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.results = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Examples", "Redis", "RedisBenchmarkResults.txt"));

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            // Setup:
            // Single server instance running on port 6379 with affinity to a single logical processor
            this.fixture.ApiClient.OnGetState(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(
                    HttpStatusCode.OK,
                    new Item<ServerState>(nameof(ServerState), new ServerState(new List<PortDescription>
                    {
                        new PortDescription
                        {
                            CpuAffinity = "0",
                            Port = 6379
                        }
                    }))));

            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) =>
                {
                    this.apiClientId = id;
                    this.clientInstance = instance;
                    return this.fixture.ApiClient.Object;
                });

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorOnInitializationGetsTheExpectedPackage()
        {
            using (var component = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorExecutesExpectedCommands()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandsExecutedInOrder(
                    $"sudo chmod \\+x \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 6379 {Regex.Escape(executor.CommandLine)}\\\""
                );
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorEstablishesTheExpectedClientServerPairings_1_Server_Instance()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                ServerState serverState = new ServerState(new List<PortDescription>
                {
                    new PortDescription
                    {
                        CpuAffinity = "0",
                        Port = 1234
                    }
                });

                this.fixture.ApiClient.OnGetState(nameof(ServerState))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, new Item<ServerState>(nameof(ServerState), serverState)));

                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandsExecutedInOrder(
                    $"sudo chmod \\+x \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 1234 {Regex.Escape(executor.CommandLine)}\\\""
                );
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorEstablishesTheExpectedClientServerPairings_2_Server_Instances()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                ServerState serverState = new ServerState(new List<PortDescription>
                {
                    new PortDescription
                    {
                        CpuAffinity = "0",
                        Port = 1234
                    },
                    new PortDescription
                    {
                        CpuAffinity = "1",
                        Port = 5678
                    }
                });

                this.fixture.ApiClient.OnGetState(nameof(ServerState))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, new Item<ServerState>(nameof(ServerState), serverState)));

                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandsExecutedInOrder(
                    $"sudo chmod \\+x \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 1234 {Regex.Escape(executor.CommandLine)}\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 5678 {Regex.Escape(executor.CommandLine)}\\\""
                );
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorEstablishesTheExpectedClientServerPairings_4_Server_Instances()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                ServerState serverState = new ServerState(new List<PortDescription>
                {
                    new PortDescription { CpuAffinity = "0", Port = 1234 },
                    new PortDescription { CpuAffinity = "1", Port = 5678 },
                    new PortDescription { CpuAffinity = "2", Port = 1111 },
                    new PortDescription { CpuAffinity = "3", Port = 2222 }
                });

                this.fixture.ApiClient.OnGetState(nameof(ServerState))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, new Item<ServerState>(nameof(ServerState), serverState)));

                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandsExecutedInOrder(
                    $"sudo chmod \\+x \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 1234 {Regex.Escape(executor.CommandLine)}\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 5678 {Regex.Escape(executor.CommandLine)}\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 1111 {Regex.Escape(executor.CommandLine)}\\\"",
                    $"sudo bash -c \\\"{Regex.Escape(this.mockPackage.Path)}/src/redis-benchmark -h 1.2.3.5 -p 2222 {Regex.Escape(executor.CommandLine)}\\\""
                );
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorEstablishesTheExpectedClientServerPairings_1_Server_Instance_2_Client_Instances()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters["ClientInstances"] = 2;

                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandExecutedTimes(
                    $"sudo bash -c.*redis-benchmark -h 1.2.3.5 -p 6379",
                    2
                );
            }
        }

        [Test]
        public async Task RedisBenchmarkClientExecutorEstablishesTheExpectedClientServerPairings_2_Server_Instances_2_Client_Instances()
        {
            this.fixture
                .TrackProcesses()
                .SetupProcessOutput(".*", this.results);

            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters["ClientInstances"] = 2;

                ServerState serverState = new ServerState(new List<PortDescription>
                {
                    new PortDescription { CpuAffinity = "0", Port = 1234 },
                    new PortDescription { CpuAffinity = "1", Port = 5678 }
                });

                this.fixture.ApiClient.OnGetState(nameof(ServerState))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, new Item<ServerState>(nameof(ServerState), serverState)));

                await executor.ExecuteAsync(CancellationToken.None);

                this.fixture.Tracking.AssertCommandExecutedTimes("redis-benchmark -h 1.2.3.5 -p 1234", 2);
                this.fixture.Tracking.AssertCommandExecutedTimes("redis-benchmark -h 1.2.3.5 -p 5678", 2);
            }
        }

        private class TestRedisBenchmarkClientExecutor : RedisBenchmarkClientExecutor
        {
            public TestRedisBenchmarkClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
