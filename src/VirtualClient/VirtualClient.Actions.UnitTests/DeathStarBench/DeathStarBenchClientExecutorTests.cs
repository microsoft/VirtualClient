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
    public class DeathStarBenchClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;
        private string apiClientId;
        private IPAddress ipAddress;

        private string resultsPath;
        private string rawString;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);

            this.SetupDefaultMockFileSystemBehavior();
            this.SetUpDefaultParameters();
            this.SetupDefaultMockApiBehavior();

            this.fixture.Parameters[nameof(DeathStarBenchExecutor.PackageName)] = "DeathStarBench";
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = "socialnetwork";
        }

        [Test]
        public async Task DeathStarBenchClientExecutorIntializeLocalAPIClientOnSingleVMSetup()
        {
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.IsTrue(this.apiClientId.Equals(IPAddress.Loopback.ToString()));
                Assert.AreEqual(this.ipAddress, IPAddress.Loopback);
            }
        }

        [Test]
        public async Task DeathStarBenchClientExecutorIntializeServerAPIClientOnMultiVMSetup()
        {
            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                Assert.IsTrue(this.apiClientId.Equals(serverIPAddress.ToString()));
                Assert.AreEqual(this.ipAddress, serverIPAddress);
            }
        }

        [Test]
        public async Task DeathStarBenchClientExecutorExecutesExpectedCommands_SocialNetworkScenario_MultiVM()
        {
            string serviceName = "socialnetwork";
            string binaryPath = this.fixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "wrk2");
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo python3 -m pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/social-network/compose-post.lua http://localhost:8080/wrk2-api/post/compose -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/social-network/read-home-timeline.lua http://localhost:8080/wrk2-api/home-timeline/read -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/social-network/read-user-timeline.lua http://localhost:8080/wrk2-api/user-timeline/read -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/social-network/mixed-workload.lua http://localhost:8080 -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh"
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.SetupDefaultMockApiBehavior(serviceName);
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                CollectionAssert.AreEqual(expectedCommands, actualCommands);
            }
        }

        [Test]
        public async Task DeathStarBenchClientExecutorExecutesExpectedCommands_MediaMicroservicesScenario_MultiVM()
        {
            string serviceName = "mediamicroservices";
            string binaryPath = this.fixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "wrk2");
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo chmod +x \"{this.mockPath.Path}/linux-x64/mediamicroservices/wrk2/wrk\"",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo python3 -m pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/media-microservices/compose-review.lua http://localhost:8080/wrk2-api/review/compose -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.SetupDefaultMockApiBehavior(serviceName);
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                   .ConfigureAwait(false);

                CollectionAssert.AreEqual(expectedCommands, actualCommands);
            }
        }

        [Test]
        public async Task DeathStarBenchClientExecutorExecutesExpectedCommands_HotelReservationScenario_MultiVM()
        {
            string serviceName = "hotelreservation";
            string binaryPath = this.fixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "wrk2");
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo python3 -m pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 600s -L -s ./scripts/hotel-reservation/mixed-workload_type_1.lua http://0.0.0.0:5000 -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPath.Path}/linux-x64/scripts/isSwarmNode.sh"
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.SetupDefaultMockApiBehavior(serviceName);
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                   .ConfigureAwait(false);

                CollectionAssert.AreEqual(expectedCommands, actualCommands);
            }
        }

        private void SetUpDefaultParameters()
        {
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.ThreadCount)] = "20";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.ConnectionCount)] = "1000";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = "600s";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.RequestPerSec)] = "1000";
            this.fixture.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = "socfb-Reed98";
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.SwarmCommand)] = "--join-swarm";
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("DeathStarBench", currentDirectory);

            this.fixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\DeathStarBench\DeathStarBenchOutputExample.txt");

            this.rawString = File.ReadAllText(resultsPath);
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);
        }

        private void SetupDefaultMockApiBehavior()
        {
            this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns<string, IPAddress, int?>((id, ip, port) =>
                {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.fixture.ApiClient.Object;
                });

            var swarmCommand = new State(new Dictionary<string, IConvertible>
            {
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "--join-swarm"
            });

            Item<JObject> expectedCommand = new Item<JObject>(nameof(DeathStarBenchExecutor.SwarmCommand), JObject.FromObject(swarmCommand));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedCommand));
        }

        private void SetupDefaultMockApiBehavior(string serviceName)
        {
            DeathStarBenchState expectedState = new DeathStarBenchState(serviceName, true);
            Item<DeathStarBenchState> expectedStateItem = new Item<DeathStarBenchState>(nameof(DeathStarBenchState), expectedState);

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        private class TestDeathStarBenchClientExecutor : DeathStarBenchClientExecutor
        {
            public TestDeathStarBenchClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
