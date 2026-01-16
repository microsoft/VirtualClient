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
    using Microsoft.Extensions.DependencyInjection.Extensions;
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
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(DeathStarBenchClientExecutorTests), "Examples", "DeathStarBench");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private IPAddress ipAddress;
        private string exampleResults;

        public void SetupApiCalls(string serviceName)
        {
            DeathStarBenchState expectedState = new DeathStarBenchState(serviceName, true);
            Item<DeathStarBenchState> expectedStateItem = new Item<DeathStarBenchState>(nameof(DeathStarBenchState), expectedState);

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns<string, IPAddress, int?>((id, ip, port) =>
                {
                    this.apiClientId = id;
                    this.ipAddress = ip;
                    return this.mockFixture.ApiClient.Object;
                });

            State executionState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "--join-swarm"
            });

            Item<JObject> expectedCommand = new Item<JObject>(nameof(DeathStarBenchExecutor.SwarmCommand), JObject.FromObject(executionState));

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedCommand));

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
        }

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.PackageName)] = "deathstarbench";
            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = "socialnetwork";
            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.ThreadCount)] = "20";
            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.ConnectionCount)] = "1000";
            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = "00:05:00";
            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.RequestPerSec)] = "1000";
            this.mockFixture.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = "socfb-Reed98";
            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.SwarmCommand)] = "--join-swarm";

            this.mockPackage = new DependencyPath("deathstarbench", this.mockFixture.GetPackagePath("deathstarbench"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.exampleResults = File.ReadAllText(this.mockFixture.Combine(DeathStarBenchClientExecutorTests.ExamplesDirectory, "DeathStarBenchOutputExample.txt"));
            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.SetupApiCalls("socialnetwork");
        }

        [Test]
        public async Task DeathStarBenchClientExecutorInitializeLocalAPIClientOnSingleVMSetup()
        {
            this.mockFixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.IsTrue(this.apiClientId.Equals(IPAddress.Loopback.ToString()));
                Assert.AreEqual(this.ipAddress, IPAddress.Loopback);
            }
        }

        [Test]
        public async Task DeathStarBenchClientExecutorInitializeServerAPIClientOnMultiVMSetup()
        {
            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
            string binaryPath = this.mockFixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "work2");
            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo apt install python3-venv -y",
                $"sudo python3 -m venv {this.mockPackage.Path}/linux-x64/venv",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U pip",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U setuptools",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make clean",
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/social-network/compose-post.lua http://localhost:8080/wrk2-api/post/compose -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/social-network/read-home-timeline.lua http://localhost:8080/wrk2-api/home-timeline/read -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/social-network/read-user-timeline.lua http://localhost:8080/wrk2-api/user-timeline/read -R 1000 >> results.txt\"",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/social-network/mixed-workload.lua http://localhost:8080 -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh"
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.SetupApiCalls(serviceName);
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.mockFixture.Process;
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
            string binaryPath = this.mockFixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "wrk2");
            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo chmod +x \"{this.mockPackage.Path}/linux-x64/mediamicroservices/wrk2/wrk\"",
                $"sudo chmod +x \"{this.mockPackage.Path}/linux-x64/mediamicroservices/wrk2/deps/luajit/src/luajit\"",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo apt install python3-venv -y",
                $"sudo python3 -m venv {this.mockPackage.Path}/linux-x64/venv",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U pip",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U setuptools",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make clean",
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/media-microservices/compose-review.lua http://localhost:8080/wrk2-api/review/compose -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.SetupApiCalls(serviceName);
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.mockFixture.Process;
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
            string binaryPath = this.mockFixture.PlatformSpecifics.Combine("linux-x64", serviceName.ToLower(), "wrk2");
            this.mockFixture.Parameters[nameof(DeathStarBenchExecutor.ServiceName)] = serviceName;

            List<string> expectedCommands = new List<string>
            {
                // On Unix/Linux systems, everything will be case-sensitive. As such the commands below are expected to be
                // exactly the same as what is executed.
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/dockerComposeScript.sh",
                $"sudo chmod +x \"/usr/local/bin/docker-compose\"",
                $"sudo apt install python3-venv -y",
                $"sudo python3 -m venv {this.mockPackage.Path}/linux-x64/venv",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U pip",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install -U setuptools",
                $"sudo {this.mockPackage.Path}/linux-x64/venv/bin/pip install aiohttp asyncio",
                $"sudo luarocks install luasocket",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo --join-swarm", // mock command but illustrates the idea of the command that should be called
                $"sudo make clean",
                $"sudo make",
                $"sudo bash -c \"./wrk -D exp -t 20 -c 1000 -d 300s -L -s ./scripts/hotel-reservation/mixed-workload_type_1.lua http://0.0.0.0:5000 -R 1000 >> results.txt\"",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh",
                $"sudo bash {this.mockPackage.Path}/linux-x64/scripts/isSwarmNode.sh"
            };

            List<string> actualCommands = new List<string>();

            using (TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.SetupApiCalls(serviceName);
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    actualCommands.Add($"{command} {arguments}".Trim());
                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None)
                   .ConfigureAwait(false);

                CollectionAssert.AreEqual(expectedCommands, actualCommands);
            }
        }

        [Test]
        public void DeathStarBenchClientExecutorSupportsIntegerAndTimeSpanDurationFormats()
        {
            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = 300;

            TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(TimeSpan.FromSeconds(300), executor.Duration);

            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = "00:05:00";

            executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.AreEqual(TimeSpan.FromMinutes(5), executor.Duration);

            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = 180;
            executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan integerBasedDuration = executor.Duration;

            this.mockFixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = "00:03:00";
            executor = new TestDeathStarBenchClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            TimeSpan timespanBasedDuration = executor.Duration;

            Assert.AreEqual(integerBasedDuration, timespanBasedDuration);
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
