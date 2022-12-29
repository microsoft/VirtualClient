// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Contracts;
    using Moq;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Threading;
    using Polly;
    using System.Net.Http;
    using Microsoft.Extensions.DependencyInjection;
    using System.IO;
    using System.Reflection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common.Contracts;

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

            this.fixture.Parameters["PackageName"] = "DeathStarBench";
        }

        [Test]
        public async Task DeathStarBenchClientExecutorIntializeLocalAPIClientOnSingleVMSetup()
        {
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.OnInitialize(EventContext.None, CancellationToken.None);

            Assert.IsTrue(this.apiClientId.Equals(IPAddress.Loopback.ToString()));
            Assert.AreEqual(this.ipAddress, IPAddress.Loopback);
        }

        [Test]
        public async Task DeathStarBenchClientExecutorIntializeServerAPIClientOnMultiVMSetup()
        {
            using TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.OnInitialize(EventContext.None, CancellationToken.None);

            ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
            IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

            Assert.IsTrue(this.apiClientId.Equals(serverIPAddress.ToString()));
            Assert.AreEqual(this.ipAddress, serverIPAddress);
        }

        [Test]
        [TestCase("socialNetwork")]
        [TestCase("mediaMicroservices")]
        [TestCase("hotelReservation")]
        public async Task DeathStarBenchClientExecutorExecutesExpectedProcessMultiVM(string ServiceName)
        {
            int processExecuted = 0;
            string binaryPath = this.fixture.PlatformSpecifics.Combine("linux-x64", ServiceName, "wrk2");
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.Scenario)] = ServiceName;
            string expectedWorkingDirectory = this.fixture.PlatformSpecifics.Combine(mockPath.Path, binaryPath);
            using TestDeathStarBenchClientExecutor executor = new TestDeathStarBenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            this.SetupDefaultMockApiBehavior(ServiceName);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                if (workingDirectory == expectedWorkingDirectory)
                {
                    processExecuted++;
                }
                return this.fixture.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None)
                .ConfigureAwait(false);
            if (ServiceName == "socialNetwork")
            {
                Assert.AreEqual(5, processExecuted);
            }
            else
            {
                Assert.AreEqual(2, processExecuted);
            }

        }

        private void SetUpDefaultParameters()
        {
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.NumberOfThreads)] = "20";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.NumberOfConnections)] = "1000";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.Duration)] = "600s";
            this.fixture.Parameters[nameof(DeathStarBenchClientExecutor.RequestPerSec)] = "1000";
            this.fixture.Parameters[nameof(DeathStarBenchServerExecutor.GraphType)] = "socfb-Reed98";
            this.fixture.Parameters[nameof(DeathStarBenchExecutor.SwarmCommand)] = "mockCommand";
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
                [nameof(DeathStarBenchExecutor.SwarmCommand)] = "mockCommand"
            });

            Item<JObject> expectedCommand = new Item<JObject>(nameof(DeathStarBenchExecutor.SwarmCommand), JObject.FromObject(swarmCommand));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchExecutor.SwarmCommand), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedCommand));
        }

        private void SetupDefaultMockApiBehavior(string ServiceName)
        {
            DeathStarBenchState expectedState = new DeathStarBenchState(ServiceName, true);

            Item<JObject> expectedStateItem = new Item<JObject>(nameof(DeathStarBenchState), JObject.FromObject(expectedState));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

            this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.Setup(client => client.GetEventingOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.fixture.ApiClient.SetupSequence(client => client.GetStateAsync(nameof(DeathStarBenchState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, new Item<DeathStarBenchState>(nameof(DeathStarBenchState), expectedState)))
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
