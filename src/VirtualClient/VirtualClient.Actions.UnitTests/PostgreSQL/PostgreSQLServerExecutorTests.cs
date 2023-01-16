// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
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
    using System.Linq;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerExecutorTests
    {
        private const string MockPassword = "MockPassword";
        private const string PasswordKey = "Password";
        private const long WarehouseCount = 1000;
        private const int NumOfUsersForDBCreation = 100;
        private MockFixture fixture;
        private DependencyPath mockPath;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            // this.fixture.Setup(PlatformID.Win32NT);
            /*this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.DBCreated
            });
            Item<JObject> expectedStateItem = new Item<JObject>(nameof(PostgreSQLState), JObject.FromObject(expectedState));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));*/

            // this.SetupDefaultMockFileSystemBehavior();
        }

        [Test]
        public async Task PostgreSQLServerExecutorInitializeDependenciesAsExpectedOnUbuntuAsync()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, Architecture.X64);
            using (TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.OnInitialize(EventContext.None, CancellationToken.None);

                string expectedPath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, "linux-x64", "createDBScript.sh");
                Assert.AreEqual(expectedPath, executor.GetServerScriptPath);
            }
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToCreateWorkloadState()
        {
            this.SetupDefaultMockBehavior();
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));

            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(nameof(PostgreSQLState), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));

            var dependencyException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            Assert.IsTrue(dependencyException.Message == "API Request Error (status code = NotFound): ");
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToCreateParametersStateIfNotCreatedEarlier()
        {
            this.SetupDefaultMockBehavior();

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLParameters), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));
            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(nameof(PostgreSQLParameters), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));
            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            var dependencyException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            Assert.IsTrue(dependencyException.Message == "API Request Error (status code = NotFound): ");
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToGetExistingWorkloadState()
        {
            this.SetupDefaultMockBehavior();
            
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));

            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            var dependencyException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            Assert.IsTrue(dependencyException.Message == "API Request Error (status code = NotFound): ");
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToUpdateWorkloadState()
        {
            this.SetupDefaultMockBehavior();
            
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));

            this.fixture.ApiClient.Setup(client => client.UpdateStateAsync(nameof(PostgreSQLState), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.NotFound));

            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            var dependencyException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            Assert.IsTrue(dependencyException.Message == "API Request Error (status code = NotFound): ");
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedProcessOnWindows()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT,Architecture.X64);
            int processExecuted = 0;
            // string expectedFile = "powershell";
            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            // string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, PlatformID.Win32NT, Architecture.X64).Path;
            // string expectedScript = Path.Combine(expectedPath, "Server.ps1");

            List<string> expectedCommands = new List<string>()
            {
                $"powershell -Command \"& {{Add-Content -Path '{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "data", "pg_hba.conf")}' -Value 'host  all  all  0.0.0.0/0  md5'}}\"",
                // $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "bin", "pg_ctl.exe")} restart -D \"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "data")}\"",
                $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "bin", "psql.exe")} -U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"",
                // $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "bin", "psql.exe")} -U postgres psql -c \"DROP ROLE IF EXISTS {executor.PsqlUserName};\"",
                // $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "bin", "psql.exe")} -U postgres psql -c \"CREATE USER {executor.PsqlUserName} PASSWORD '{executor.PsqlPassword}';\"",
                $"{this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, "hammerdbcli.bat")} auto createDB.tcl"
            };

            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{file} {arguments}"))
                {
                    processExecuted++;
                }
            
                // Assert.AreEqual(expectedFile, file);

                // string expectedParameters = $" -WarehouseCount {executor.NumberWarehouses} -NumOfUsersForDBCreation {executor.NumberUsersForDbCreation}";
                /*string expectedArguments = $"-ExecutionPolicy unrestricted -File \"{expectedScript}\" {expectedParameters}";
                Assert.AreEqual(expectedArguments, arguments);*/

                // Assert.IsNull(workingDirectory);
                return this.fixture.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None);

            Assert.AreEqual(3, processExecuted);
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedProcessOnUnix()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, Architecture.X64);
            int processExecuted = 0;
            string expectedCommand = "sudo bash createDBScript.sh";

            // string expectedFile = "bash createDBScript.sh";
            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            /*string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, PlatformID.Win32NT, Architecture.X64).Path;
            string expectedScript = Path.Combine(expectedPath, "Server.ps1");*/

            this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                if (expectedCommand == $"{file} {arguments}")
                {
                    processExecuted++;
                }
                // Assert.AreEqual(expectedFile, file);

                // string expectedParameters = $" -WarehouseCount {executor.NumberWarehouses} -NumOfUsersForDBCreation {executor.NumberUsersForDbCreation}";
                /*string expectedArguments = $"-ExecutionPolicy unrestricted -File \"{expectedScript}\" {expectedParameters}";
                Assert.AreEqual(expectedArguments, arguments);*/

                Assert.IsNotNull(workingDirectory);
                return this.fixture.Process;
            };

            await executor.ExecuteAsync(CancellationToken.None);

            Assert.AreEqual(1, processExecuted);
        }

        /*[Test]
        public async Task PostgreSQLServerExecutorGetsPasswordFromExpectedFile()
        {
            bool fileVisited = false;
            string expectedPath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPath, PlatformID.Win32NT, Architecture.X64).Path;
            string expectedLogFile = Path.Combine(expectedPath, "Password.json");

            IDictionary<string, IConvertible> mockPasswordParameters = new Dictionary<string, IConvertible>();
            mockPasswordParameters[PasswordKey] = MockPassword;
            string passwordJSONString = JsonConvert.SerializeObject(mockPasswordParameters, new ParameterDictionaryJsonConverter());

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((file, canToken) =>
                {
                    if (file.Equals(expectedLogFile, StringComparison.OrdinalIgnoreCase))
                    {
                        fileVisited = true;
                    }
                })
                .ReturnsAsync(passwordJSONString);

            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(fileVisited);
        }*/

        /*[Test]
        public async Task PostgreSQLServerExecutorCreatesExpectedParameters()
        {
            int executed = 0;
            string makeFileString = "mock Makefile";

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(makeFileString);
            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLParameters), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            using TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(nameof(PostgreSQLParameters), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<string, JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((str, obj, can, pol) =>
                {
                    var expectedParameters = new PostgreSQLParameters()
                    {
                        UserName = Environment.MachineName,
                        Password = MockPassword,
                        WarehouseCount = executor.NumberWarehouses,
                        NumOfVirtualUsers = executor.NumberOfVirtualUsers
                        *//*NumOfUsersForTransactions = executor.NumberUsersForTransactions,
                        UserTransactions = executor.UserTransactionsCount,
                        Iterations = executor.IterationCount*//*
                    };

                    executed++;
                    Assert.IsTrue(obj.ToString() == JObject.FromObject(expectedParameters).ToString());
                })
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(executed, 1);
        }*/

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);

            string makeFileString = "mock Makefile";

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(makeFileString);

            this.mockPath = this.fixture.Create<DependencyPath>();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPath.Name
            };

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockPath);

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.DBCreated
            });
            Item<JObject> expectedStateItem = new Item<JObject>(nameof(PostgreSQLState), JObject.FromObject(expectedState));

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(nameof(PostgreSQLState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem));

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            /*IDictionary<string, IConvertible> mockPasswordParameters = new Dictionary<string, IConvertible>();
            mockPasswordParameters[PasswordKey] = MockPassword;
            string passwordJSONString = JsonConvert.SerializeObject(mockPasswordParameters, new ParameterDictionaryJsonConverter());

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(passwordJSONString);*/

            /*this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "14" },
                { "PackageName", "postgresql" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // this.currentDirectoryPath = new DependencyPath("LAPACK", currentDirectory);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            // this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);
            *//*resultsPath = this.mockFixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\LAPACK\LAPACKResultsExample.txt");
            this.rawString = File.ReadAllText(resultsPath);
            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllText(It.IsAny<string>()))
                .Returns(this.rawString);*//*

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;*/
        }
/*
        private void SetupDefaultMockFileSystemBehavior()
        {
            IDictionary<string, IConvertible> mockPasswordParameters = new Dictionary<string, IConvertible>();
            mockPasswordParameters[PasswordKey] = MockPassword;
            string passwordJSONString = JsonConvert.SerializeObject(mockPasswordParameters, new ParameterDictionaryJsonConverter());

            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(passwordJSONString);
        }*/

        private class TestPostgreSQLServerExecutor : PostgreSQLServerExecutor
        {
            public TestPostgreSQLServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            // public Func<string> OnGetPowershellScriptParameters => base.GetPowershellScriptParametersAsync;

            public string GetServerScriptPath => this.ServerScriptPath;

            public long NumberWarehouses => this.WarehouseCount;

            public int NumberOfVirtualUsers => this.NumOfVirtualUsers;

            public string PsqlUserName => this.UserName;

            public string PsqlPassword => this.Password;
/*
            public int NumberUsersForTransactions => this.NumOfUsersForTransactions;

            public int UserTransactionsCount => this.UserTransactions;

            public int IterationCount => this.Iterations;*/
        }
    }
}