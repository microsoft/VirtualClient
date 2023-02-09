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
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using Microsoft.AspNetCore.Authentication;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLClientExecutorTests
    {
        private const string MockUserName = "MockUserName";
        private const string MockPassword = "MockPassword";
        private MockFixture fixture;
        private DependencyPath mockPath;
        private string apiClientId;
        private IPAddress ipAddress;
        private string rawString;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public async Task PostgreSQLClientExecutorIntializeLocalAPIClientOnSingleVMSetup()
        {
            this.SetupDefaultBehaviour();
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            using (TestPostgreSQLClientExecutor executor = new TestPostgreSQLClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                Assert.IsTrue(this.apiClientId.Equals(IPAddress.Loopback.ToString()));
                Assert.AreEqual(this.ipAddress, IPAddress.Loopback);
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorIntializeServerAPIClientOnMultiVMSetup()
        {
            this.SetupDefaultBehaviour();
            using (TestPostgreSQLClientExecutor executor = new TestPostgreSQLClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                ClientInstance serverInstance = executor.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                Assert.IsTrue(this.apiClientId.Equals(serverIPAddress.ToString()));
                Assert.AreEqual(this.ipAddress, serverIPAddress);
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorThrowsOnFailingToGetParameters()
        {
            this.SetupDefaultBehaviour();
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLParameters), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));
            using (TestPostgreSQLClientExecutor executor = new TestPostgreSQLClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                var dependencyException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(dependencyException.Message == "API Request Error (status code = NotFound): ");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task PostgreSQLClientExecutorExecutesAsExpected(PlatformID platformID, Architecture architecture)
        {
            if (platformID == PlatformID.Unix) 
            {
                this.SetupDefaultBehaviour(PlatformID.Unix);
            }
            else
            {
                this.SetupDefaultBehaviour();
            }
            using (TestPostgreSQLClientExecutor executor = new TestPostgreSQLClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                int processExecuted = 0;

                List<string> expectedCommands;

                await executor.OnInitialize(EventContext.None, CancellationToken.None);
                await executor.OnSetClientParameters(CancellationToken.None);
            
                if (platformID == PlatformID.Unix) 
                {
                   expectedCommands = new List<string>()
                    {
                        $"sudo bash runTransactionsScript.sh"

                    };
                }
                else if (platformID == PlatformID.Win32NT)
                {
                    expectedCommands = new List<string>()
                    {
                        $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "hammerdbcli.bat")} auto runTransactions.tcl",
    
                    };
                }
                else
                {
                    expectedCommands = new List<string>();
                }
                
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    if (expectedCommands.Any(c => c == $"{file} {arguments}"))
                    {
                        processExecuted++;
                    }
                    
                    this.fixture.Process.RedirectStandardOutput= true;
                    this.fixture.Process.StandardOutput.Append(this.rawString);
                    return this.fixture.Process;
                };

                await executor.OnExecute(EventContext.None, CancellationToken.None);

                Assert.AreEqual(1, processExecuted);
            }
        }

        private void SetupDefaultMockFileSystemBehavior()
        {
            string parametersString = JsonConvert.SerializeObject(this.fixture.Parameters, new ParameterDictionaryJsonConverter());

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(parametersString);

            this.fixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.File.Setup(f => f.Copy(It.IsAny<string>(), It.IsAny<string>()));

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            DependencyPath currentDirectoryPath = new DependencyPath("postgresql", currentDirectory);
            string outputPath = Path.Combine(currentDirectory, @"Examples\PostgreSQL\PostgresqlresultsExample.txt");
            this.rawString = File.ReadAllText(outputPath);
      
        }

        private void SetupDefaultBehaviour(PlatformID platformID = PlatformID.Win32NT) 
        {
            this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.Setup(platformID);
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);
            this.fixture.PackageManager.Setup(epa => epa.ExtractPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), ArchiveType.Zip))
                .Returns(Task.CompletedTask);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.SetupDefaultMockFileSystemBehavior();
            this.SetupDefaultMockApiBehavior();
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

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.DBCreated
            });
            Item<JObject> expectedStateItem = new Item<JObject>(nameof(PostgreSQLState), JObject.FromObject(expectedState));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedStateItem));

            PostgreSQLParameters expectedPostgreSQLParameters = new PostgreSQLParameters()
            {
                UserName = MockUserName,
                Password = MockPassword,
                WarehouseCount = 1000,
                NumOfVirtualUsers= 1000
            };
            Item<JObject> expectedPostgreSQLParametersItem = new Item<JObject>(nameof(PostgreSQLParameters), JObject.FromObject(expectedPostgreSQLParameters));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLParameters), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedPostgreSQLParametersItem));
        }

        private class TestPostgreSQLClientExecutor : PostgreSQLClientExecutor
        {
            public TestPostgreSQLClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            public Func<EventContext, CancellationToken, Task> OnExecute => base.ExecuteAsync;

            public Func<CancellationToken, Task> OnSetClientParameters => base.SetScriptParameters;
        }
    }
}