// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using AutoFixture;
    using VirtualClient.Common.Telemetry;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RedisBenchmarkClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
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
            this.scriptsPath = this.fixture.PlatformSpecifics.GetScriptPath();
            string agentId = $"{Environment.MachineName}";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);

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

            using TestRedisBenchmarkClientExecutor executor = new TestRedisBenchmarkClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            string expectedScript = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "src", "redis-benchmark");


            List<string> expectedCommands = new List<string>()
            {
                $"sudo chmod +x \"{expectedScript}\"",
                $"sudo bash -c \"{expectedScript} -h 1.2.3.5 -p 6379 -c 2 -n 1 -P 32 -q --csv\"",
            };

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                if (arguments == $"bash -c \"{expectedScript} -h 1.2.3.5 -p 6379 -c 2 -n 1 -P 32 -q --csv\"")
                {
                    this.fixture.Process.StandardOutput.Append(this.rawString);
                }
                return this.fixture.Process;
            };
            await executor.ExecuteAsync(CancellationToken.None);
            Assert.AreEqual(2, commandExecuted);
        }

        private void SetUpDefaultParameters()
        {
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name,
                ["ClientCount"] = "2",
                ["PipelineDepth"] = "32",
                ["RequestCount"] = "1",
                ["Port"] = "6379",
                [nameof(RedisExecutor.Scenario)] = "RedisBenchmark"
            };
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Redis", currentDirectory);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\Redis\RedisBenchmarkResults.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);
        }

        private class TestRedisBenchmarkClientExecutor : RedisBenchmarkClientExecutor
        {
            public TestRedisBenchmarkClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
