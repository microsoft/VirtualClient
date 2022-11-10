// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemcachedMemtierClientExecutorTests
    {
        private const string ExampleUsername = "my-username";
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath memtierPath;
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
            string agentId = $"{Environment.MachineName}";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync("MemtierPackage", It.IsAny<CancellationToken>()))
                .ReturnsAsync(memtierPath);

            var serverCopiesCount = new State(new Dictionary<string, IConvertible>
            {
                [nameof(MemcachedExecutor.ServerCopiesCount)] = "2"
            });

            Item<JObject> expectedCopies = new Item<JObject>(nameof(MemcachedExecutor.ServerCopiesCount), JObject.FromObject(serverCopiesCount));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(MemcachedExecutor.ServerCopiesCount), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedCopies));

            this.SetUpDefaultParameters();
            this.SetupDefaultMockFileSystemBehavior();
        }

        [Test]
        public async Task MemcachedMemtierClientExecutorExecutesExpectedProcess()
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

            using TestMemcachedMemtierClientExecutor executor = new TestMemcachedMemtierClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            string memtierBenchmarkPath = this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path, "memtier_benchmark");

            string resultsPath = this.fixture.PlatformSpecifics.Combine(this.memtierPath.Path, "memtier_results");

            List<string> expectedCommands = new List<string>()
            {
                $"sudo -u {MemcachedMemtierClientExecutorTests.ExampleUsername} {memtierBenchmarkPath} --server 1.2.3.5 --port 6379 --protocol memcache_text --clients 2 --threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 30 --print-percentiles 50,90,95,99,99.9 --random-data",
            };

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                if (arguments == $"-u {MemcachedMemtierClientExecutorTests.ExampleUsername} {memtierBenchmarkPath} --server 1.2.3.5 --port 6379 --protocol memcache_text --clients 2 --threads 1 --ratio 1:9 --data-size 32 --pipeline 32 --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count 1 --test-time 30 --print-percentiles 50,90,95,99,99.9 --random-data")
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
                ["NumberOfClientsPerThread"] = "2",
                ["NumberOfThreads"] = "1",
                ["PipelineDepth"] = "32",
                ["NumberOfRuns"] = "1",
                ["DurationInSecs"] = "30",
                ["Port"] = "6379",
                ["Protocol"] = "memcache_text",
                [nameof(MemcachedExecutor.Scenario)] = "Memtier",
                ["Username"] = MemcachedMemtierClientExecutorTests.ExampleUsername
            };
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Memcached", currentDirectory);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\Memcached\MemcachedExample.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);
        }

        private class TestMemcachedMemtierClientExecutor : MemcachedMemtierClientExecutor
        {
            public TestMemcachedMemtierClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
