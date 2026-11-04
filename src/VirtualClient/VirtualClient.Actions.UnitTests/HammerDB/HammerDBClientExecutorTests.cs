// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using NUnit.Framework.Interfaces;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.HammerDBExecutor;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class HammerDBClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private ClientInstance clientInstance;
        private string mockPackagePath;
        private string originalPassword;

        [SetUp]
        public void SetPasswordEnvironmentVariable()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", "password with 'quotes' and $symbols");
        }

        [TearDown]
        public void RestorePasswordEnvironmentVariable()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpcc", "postgresql", false)]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpch", "postgresql", false)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "tpcc", "postgresql", false)]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "tpch", "postgresql", false)]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpcc", "PostgreSQL", true)]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpcc", "mysql", false)]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpch", "MySQL", false)]
        [TestCase(PlatformID.Unix, Architecture.X64, "tpch", "mysql", true)]
        [TestCase(PlatformID.Unix, Architecture.X64, BenchmarkName.TPCC, DatabaseServer.PostgreSQL, false)]
        [TestCase(PlatformID.Unix, Architecture.X64, BenchmarkName.TPCH, DatabaseServer.MySQL, false)]
        public async Task HammerDBClientExecutorRunsTheExpectedWorkloadCommand(
            PlatformID platform, Architecture architecture, string workload, string sqlServer, bool runningInContainer)
        {
            this.SetupDefaultBehavior(platform, architecture);
            bool isPostgreSQL = string.Equals(sqlServer, DatabaseServer.PostgreSQL, StringComparison.OrdinalIgnoreCase);
            int port = isPostgreSQL ? 5432 : 3306;
            this.fixture.SystemManagement.SetupGet(manager => manager.RunningInContainer).Returns(runningInContainer);
            this.fixture.Parameters[nameof(HammerDBClientExecutor.Workload)] = workload;
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = sqlServer;
            this.fixture.Parameters[nameof(HammerDBClientExecutor.Port)] = port;
            const string filePassword = "file-password-with-$symbols";
            string databasePackageName = isPostgreSQL ? "postgresql" : "mysql-server";
            this.fixture.Parameters[nameof(HammerDBClientExecutor.DatabasePackageName)] = databasePackageName;
            DependencyPath databasePackage = new DependencyPath(databasePackageName, this.fixture.GetPackagePath(databasePackageName));
            this.fixture.PackageManager.OnGetPackage(databasePackageName).ReturnsAsync(databasePackage);
            string passwordFile = this.fixture.PlatformSpecifics.Combine(
                this.fixture.ToPlatformSpecificPath(databasePackage, platform, architecture).Path, "Password.json");
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file-password-with-$symbols\"}");

            string passwordEnvironmentVariable = isPostgreSQL ? "PGPASSWORD" : "MYSQLPASSWORD";
            string configurationCommand = platform == PlatformID.Unix
                ? $"sudo --preserve-env={passwordEnvironmentVariable} python3"
                : "python3";
            string workloadArguments = string.Equals(workload, BenchmarkName.TPCC, StringComparison.OrdinalIgnoreCase)
                ? "--warehouseCount 1"
                : "--scaleFactor 10";
            List<string> expectedCommands = new List<string>
            {
                $"{configurationCommand} {this.fixture.PlatformSpecifics.Combine(this.mockPackagePath, "configure-workload-generator.py")} --workload {workload.ToLowerInvariant()} --sqlServer {sqlServer.ToLowerInvariant()} --port {port} --virtualUsers 1 --dbName hammerdbtest --hostIPAddress [0-9.]+ {workloadArguments} --duration 1",
                $"python3 {this.fixture.PlatformSpecifics.Combine(this.mockPackagePath, "run-workload.py")} --runTransactionsTCLFilePath runTransactions.tcl"
            };

            if (this.fixture.Platform == PlatformID.Win32NT)
            {
                for (int i = 0; i < expectedCommands.Count; i++)
                {
                    expectedCommands[i] = expectedCommands[i].Replace(@"\", @"\\");
                }
            }

            int commandNumber = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                string executedCommand = $"{exe} {arguments}";
                StringAssert.DoesNotContain("--password", arguments);
                StringAssert.DoesNotContain(Environment.GetEnvironmentVariable("PGPASSWORD"), arguments);
                StringAssert.DoesNotContain(filePassword, arguments);
                bool executed = Regex.IsMatch(executedCommand,expectedCommands[commandNumber]);
                Assert.IsTrue(executed);
                commandNumber += 1;

                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };

                process.OnStart = () =>
                {
                    Assert.AreEqual(
                        isPostgreSQL ? filePassword : Environment.GetEnvironmentVariable("PGPASSWORD"),
                        process.EnvironmentVariables["PGPASSWORD"]);
                    Assert.AreEqual(filePassword, process.EnvironmentVariables[passwordEnvironmentVariable]);
                    return true;
                };

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }

            Assert.AreEqual(expectedCommands.Count, commandNumber);
            this.fixture.File.Verify(
                file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()),
                Times.Once);
        }
        public void SetupDefaultBehavior(PlatformID platform, Architecture architecture)
        {
            {
                this.fixture = new MockFixture();
                this.fixture.Setup(platform, architecture);
                this.mockPackage = new DependencyPath("hammerdb", this.fixture.PlatformSpecifics.GetPackagePath("hammerdb"));
                this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
                this.mockPackagePath = this.fixture.ToPlatformSpecificPath(mockPackage, platform, architecture).Path;

                this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(HammerDBClientExecutor.Port), "5432"},
                { nameof(HammerDBClientExecutor.DatabaseName), "hammerdbtest" },
                { nameof(HammerDBClientExecutor.Workload), "tpcc" },
                { nameof(HammerDBExecutor.SQLServer), DatabaseServer.PostgreSQL },
                { nameof(HammerDBClientExecutor.VirtualUserCount), "1"},
                { nameof(HammerDBClientExecutor.WarehouseCount), "1"},
                { nameof(HammerDBClientExecutor.ScaleFactor), "10"},
                { nameof(HammerDBClientExecutor.Duration), "00:01:00" },
                { "ServerIpAddress", "localhost"},
                { nameof(HammerDBClientExecutor.PackageName), "hammerdb" },
            };

                string agentId = $"{Environment.MachineName}";
                this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

                this.fixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                this.fixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                this.fixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                    .Returns<string, ClientInstance>((id, instance) =>
                    {
                        this.apiClientId = id;
                        this.clientInstance = instance;
                        return this.fixture.ApiClient.Object;
                    });

                this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState()
                {
                    HammerDBInitialized = true
                }));

                this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
                this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

                this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new CpuInfo("cpu", "description", 4, 8, 4, 4, false));

                IEnumerable<Disk> disks;
                disks = this.fixture.CreateDisks(platform, true);
                this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);
            }
        }

        private class TestHammerDBClientExecutor : HammerDBClientExecutor
        {
            public TestHammerDBClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
