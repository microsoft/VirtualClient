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
    public class HammerDBClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private ClientInstance clientInstance;
        private string mockPackagePath;

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task HammerDBClientExecutorRunsTheExpectedWorkloadCommand(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehavior(platform, architecture);

            string tempPackagePath;

            if (platform == PlatformID.Win32NT)
            {
                tempPackagePath = this.mockPackagePath.Replace(@"\", @"\\");
            }
            else
            {
                tempPackagePath = this.mockPackagePath;
            }

            string[] expectedCommands =
            {
                $"python3 {tempPackagePath}/configure-workload-generator.py --workload tpcc --sqlServer postgresql --port 5432 --virtualUsers 1 --warehouseCount 1 --password [A-Za-z0-9+/=]+ --dbName hammerdbtest --hostIPAddress [0-9.]+",
                $"python3 {tempPackagePath}/run-workload.py --runTransactionsTCLFilePath runTransactions.tcl"
        };
            int commandNumber = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];
                string executedCommand = $"{exe} {arguments}";
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

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
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
                { nameof(HammerDBClientExecutor.SuperUserPassword), "pass" },
                { nameof(HammerDBClientExecutor.Workload), "tpcc" },
                { nameof(HammerDBClientExecutor.SQLServer), "postgresql" },
                { nameof(HammerDBClientExecutor.VirtualUsers), "1"},
                { nameof(HammerDBClientExecutor.WarehouseCount), "1"},
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
