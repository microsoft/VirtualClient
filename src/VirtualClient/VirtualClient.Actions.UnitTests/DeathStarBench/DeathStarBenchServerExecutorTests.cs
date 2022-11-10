// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
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
    public class DeathStarBenchServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private const string GraphType = "socfb-Reed98";

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.SetupDefaultMockFileSystemBehavior();
            this.SetupDefaultMockApiBehavior();
            this.fixture.Parameters["PackageName"] = "DeathStarBench";
        }

        [Test]
        [TestCase("socialNetwork", @"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml socialNetwork""")]
        [TestCase("mediaMicroservices", @"bash -c ""docker stack deploy --compose-file=docker-compose.yml mediaMicroservices""")]
        [TestCase("hotelReservation", @"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml hotelReservation""")]
        public async Task DeathStarBenchServerExecutorExecutesExpectedProcessInMultiVMScenario(string ServiceName, string ExpectedArgument)
        {
            int processExecuted = 0;
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.Scenario)] = ServiceName;
            this.fixture.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = GraphType;
            using TestDeathStarBenchServerExecutor executor = new TestDeathStarBenchServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            DeathStarBenchState serverState = new DeathStarBenchState(ServiceName, true);
            Item<JObject> expectedStateItem = new Item<JObject>(nameof(DeathStarBenchState), JObject.FromObject(serverState));

            Item<JObject> expectedCommandStateItem = new Item<JObject>(nameof(DeathStarBenchExecutor.SwarmCommand), JObject.FromObject(expectedStateItem));
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedCommandStateItem));

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (arguments == ExpectedArgument)
                {
                    processExecuted++;
                }
                return this.fixture.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        [TestCase("socialNetwork")]
        [TestCase("mediaMicroservices")]
        [TestCase("hotelReservation")]
        public async Task DeathStarBenchServerExecutorExecutesExpectedProcessInSingleVMScenario(string ServiceName)
        {
            int processExecuted = 0;
            string expectedArguments = "docker-compose -f docker-compose.yml up -d";
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.Scenario)] = ServiceName;
            this.fixture.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = "socfb-Reed98";
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}", "1.2.3.4", "Server"),
            });
            using TestDeathStarBenchServerExecutor executor = new TestDeathStarBenchServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            DeathStarBenchState serverState = new DeathStarBenchState(ServiceName, true);
            Item<JObject> expectedStateItem = new Item<JObject>(nameof(DeathStarBenchState), JObject.FromObject(serverState));

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (command == "sudo" && arguments == expectedArguments)
                {
                    processExecuted++;

                    this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                        .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));
                }
                if (arguments == "bash -c \"docker ps | wc -l\"")
                {
                    this.fixture.Process.StandardOutput.Append("1");
                }
                return this.fixture.Process;
            };

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                        .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound, expectedStateItem));

            await executor.ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, processExecuted);
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string mockCommand = "mockCommad";
            this.fixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCommand);

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
        }

        private void SetupDefaultMockApiBehavior()
        {
            State command = new State(new Dictionary<string, IConvertible>
            {
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "mock Command"
            });
        }
        private class TestDeathStarBenchServerExecutor : DeathStarBenchServerExecutor
        {
            public TestDeathStarBenchServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
