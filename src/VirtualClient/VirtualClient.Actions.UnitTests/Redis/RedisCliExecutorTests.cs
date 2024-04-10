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
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RedisCliExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private ClientInstance clientInstance;
        private string apiClientId;
        //private string results;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("redis", this.fixture.GetPackagePath("redis"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "RedisCliExecutor",
                ["PackageName"] = this.mockPackage.Name,
                ["CommandLine"] = "-h {ServerIpAddress} -p {ServerPortNumber} FLUSHALL",
                ["ServerInstances"] = 1,
                ["ServerPort"] = 6379,
                ["Role"] = "client"
            };


            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPackage);

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
        public async Task RedisCliExecutorExecutesExpectedCommands()
        {
            using (var executor = new TestRedisCliExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Run the Redis cli. Values based on the default parameter values set at the top
                    $"redis-cli -h 1.2.3.5 -p 6379 FLUSHALL"
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

 

        private class TestRedisCliExecutor : RedisCliExecutor
        {
            public TestRedisCliExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
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
