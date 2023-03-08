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
    using static VirtualClient.Actions.RedisExecutor;

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
                ["ClientsPerThread"] = 2,
                ["PipelineDepth"] = 32,
                ["RequestCount"] = 10000,
                ["Port"] = 6379
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.results = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, @"Examples\Redis\RedisBenchmarkResults.txt"));

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.results);

            var state = new Item<ServerState>(nameof(ServerState), new ServerState(new Dictionary<string, IConvertible>
            {
                [nameof(ServerState.Ports)] = 6379
            }));

            this.fixture.ApiClient.OnGetState(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, state));

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
            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the benchmark toolset executable
                    $"sudo chmod +x \"{this.mockPackage.Path}/linux-x64/src/redis-benchmark\"",

                    // Run the Redis benchmark. Values based on the default parameter values set at the top
                    $"sudo bash -c \"{this.mockPackage.Path}/linux-x64/src/redis-benchmark -h 1.2.3.5 -p 6379 -c 2 -n 10000 -P 32 -q --csv\"" 
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
        public async Task RedisBenchmarkClientExecutorUsesThePortDefinedToCommunicateWithTheServer()
        {
            using (var executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters["Port"] = 1234;

                List<string> expectedCommands = new List<string>()
                {
                    // Run the Redis benchmark. Values based on the default parameter values set at the top
                    $"sudo bash -c \"{this.mockPackage.Path}/linux-x64/src/redis-benchmark -h 1.2.3.5 -p 1234 -c 2 -n 10000 -P 32 -q --csv\""
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
