// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DeathStarBenchServerExecutorTests
    {
        private MockFixture mock;
        private DependencyPath mockPath;
        private const string GraphType = "socfb-Reed98";

        public void SetupTest(string serviceName)
        {
            // Setup:
            // The expected files exist on the file system.
            string mockCommand = "--join-swarm";

            this.mock.Parameters[nameof(DeathStarBenchServerExecutor.Scenario)] = serviceName;
            this.mock.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = "socfb-Reed98";

            this.mock.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCommand);

            this.mock.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mock.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            // Setup:
            // The server-side component successfully updates the state.
            this.mock.ApiClient.OnUpdateState<DeathStarBenchState>(nameof(DeathStarBenchState))
                .ReturnsAsync(this.mock.CreateHttpResponse(HttpStatusCode.OK));

            Item<State> joinSwarmState = new Item<State>(
                nameof(DeathStarBenchExecutor.SwarmCommand),
                new State(new Dictionary<string, IConvertible>
                {
                    [nameof(DeathStarBenchExecutor.SwarmCommand)] = "--join-swarm"
                }));

            this.mock.ApiClient
                 .OnGetState(nameof(DeathStarBenchExecutor.SwarmCommand))
                .ReturnsAsync(this.mock.CreateHttpResponse(HttpStatusCode.OK, joinSwarmState));
        }

        [SetUp]
        public void SetupTests()
        {
            this.mock = new MockFixture();
            this.mock.Setup(PlatformID.Unix);

            this.mockPath = this.mock.Create<DependencyPath>();
            this.mock.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.mock.Parameters[nameof(DeathStarBenchExecutor.PackageName)] = "deathstarbench";
            this.mock.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = "socialnetwork";
        }

        [Test]
        [TestCase("socialnetwork", @"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml socialnetwork""")]
        [TestCase("mediamicroservices", @"bash -c ""docker stack deploy --compose-file=docker-compose.yml mediamicroservices""")]
        [TestCase("hotelreservation", @"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml hotelreservation""")]
        public async Task DeathStarBenchServerExecutorExecutesExpectedProcessInMultiVMScenario(string serviceName, string expectedArguments)
        {
            this.SetupTest(serviceName);

            int processExecuted = 0;
            this.mock.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;
            this.mock.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = GraphType;
            using TestDeathStarBenchServerExecutor executor = new TestDeathStarBenchServerExecutor(this.mock.Dependencies, this.mock.Parameters);


            this.mock.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (arguments == expectedArguments)
                {
                    processExecuted++;
                }
                return this.mock.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(1, processExecuted);
        }

        [Test]
        [TestCase("socialnetwork")]
        [TestCase("mediamicroservices")]
        [TestCase("hotelreservation")]
        public async Task DeathStarBenchServerExecutorExecutesExpectedProcessInSingleVMScenario(string serviceName)
        {
            this.SetupTest(serviceName);

            int processExecuted = 0;
            string expectedArguments = "docker-compose -f docker-compose.yml up -d";

            this.mock.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}", "1.2.3.4", "Server"),
            });

            using (var executor = new TestDeathStarBenchServerExecutor(this.mock.Dependencies, this.mock.Parameters))
            {
                DeathStarBenchState serverState = new DeathStarBenchState(serviceName, true);
                Item<JObject> expectedStateItem = new Item<JObject>(nameof(DeathStarBenchState), JObject.FromObject(serverState));

                this.mock.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    if (command == "sudo" && arguments == expectedArguments)
                    {
                        processExecuted++;
                    }

                    if (arguments == "bash -c \"docker ps | wc -l\"")
                    {
                        this.mock.Process.StandardOutput.Append("1");
                    }

                    return this.mock.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.AreEqual(1, processExecuted);
            }
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
