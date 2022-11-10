// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using AutoFixture;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;
    using Moq;
    using System.Threading;
    using VirtualClient.Common.Telemetry;
    using Microsoft.Extensions.DependencyInjection;
    using System.Runtime.InteropServices;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using System.Net;
    using Polly;
    using System.Net.Http;
    using System.IO;
    using System.Reflection;
    using System.Diagnostics.Metrics;
    using System.Security.Policy;
    using VirtualClient.Actions;

    [TestFixture]
    [Category("Unit")]
    public class RedisMemtierClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath memtierPath;
        private string scriptsPath;
        private DependencyPath currentDirectoryPath;
        private string apiClientId;
        private ClientInstance clientInstance;

        private string resultsPath;
        private string rawString;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.memtierPath = new DependencyPath("memtier", this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "memtier"));
            this.scriptsPath = this.fixture.PlatformSpecifics.GetScriptPath();
            string agentId = $"{Environment.MachineName}";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);

            var serverCopiesCount = new State(new Dictionary<string, IConvertible>
            {
                [nameof(RedisExecutor.ServerCopiesCount)] = "2"
            });

            Item<JObject> expectedCopies = new Item<JObject>(nameof(RedisExecutor.ServerCopiesCount), JObject.FromObject(serverCopiesCount));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(RedisExecutor.ServerCopiesCount), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedCopies));

            this.SetUpDefaultParameters();
            this.SetupDefaultMockFileSystemBehavior();
        }

        [Test]
        public async Task RedisMemtierClientExecutorExecutesExpectedProcess()
        {
            int commandExecuted = 0;
            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) =>
                {
                    this.apiClientId = id;
                    this.clientInstance = instance;
                    return this.fixture.ApiClient.Object;
                });

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetEventingOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            using TestRedisMemtierClientExecutor executor = new TestRedisMemtierClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            string expectedScript = this.fixture.PlatformSpecifics.Combine(this.scriptsPath, this.mockPath.Name, "RunClient.sh");

            string memtierBenchmarkPath = this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path, "memtier_benchmark");

            List<string> expectedCommands = new List<string>()
            {
                $"sudo {memtierBenchmarkPath} --server 1.2.3.5 --port 6379 --protocol redis --clients 2 --threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 30 --print-percentile 50,90,95,99,99.9 --random-data",
            };

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                if (arguments == $"{memtierBenchmarkPath} --server 1.2.3.5 --port 6379 --protocol redis --clients 2 --threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 30 --print-percentile 50,90,95,99,99.9 --random-data")
                {
                    this.fixture.Process.StandardOutput.Append(this.rawString);
                }
                return this.fixture.Process;
            };
            await executor.ExecuteAsync(CancellationToken.None);
            Assert.AreEqual(1, commandExecuted);
        }

        private void SetUpDefaultParameters()
        {
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name,
                ["Copies"] = "4",
                ["Bind"] = "1",
                ["NumberOfClients"] = "2",
                ["NumberOfThreads"] = "1",
                ["PipelineDepth"] = "32",
                ["NumberOfRuns"] = "1",
                ["DurationInSecs"] = "30",
                ["Port"] = "6379",
                [nameof(RedisExecutor.Scenario)] = "Memtier"
            };
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Redis", currentDirectory);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\Redis\redis-memtier-results.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);
        }

        private class TestRedisMemtierClientExecutor : RedisMemtierClientExecutor
        {
            public TestRedisMemtierClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
