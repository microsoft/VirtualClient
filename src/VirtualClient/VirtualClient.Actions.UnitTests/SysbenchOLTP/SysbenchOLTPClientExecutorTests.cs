// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchOLTPClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private ClientInstance clientInstance;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("sysbench", this.mockFixture.PlatformSpecifics.GetPackagePath("sysbench"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SysbenchOLTPClientExecutor.DatabaseName), "sbtest" },
                { nameof(SysbenchOLTPClientExecutor.Threads), "1" },
                { nameof(SysbenchOLTPClientExecutor.RecordCount), "10000" },
                { nameof(SysbenchOLTPClientExecutor.DurationSecs), "10" },
                { nameof(SysbenchOLTPClientExecutor.Workload), "oltp_read_write" },
                { nameof(SysbenchOLTPClientExecutor.PackageName), "sysbench" },
                { nameof(SysbenchOLTPClientExecutor.NumTables), "1" }
            };

            string agentId = $"{Environment.MachineName}";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);
        }

        [Test]
        public async Task SysbenchOLTPClientExecutorRunsTheExpectedWorkloadCommand()
        {
            string mockPackagePath = this.mockPackage.Path;
            SetupDefaultBehavior();

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) =>
                {
                    this.apiClientId = id;
                    this.clientInstance = instance;
                    return this.mockFixture.ApiClient.Object;
                });

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            string[] expectedCommands =
            {
                $"sudo ./autogen.sh",
                $"sudo ./configure",
                $"sudo make -j",
                $"sudo make install",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_common --tables=1 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup"
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {

                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }
                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "SysbenchOLTP", "SysbenchOLTPExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchOLTPClientExecutor SysbenchExecutor = new TestSysbenchOLTPClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchOLTPClientExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            string mockPackagePath = this.mockPackage.Path;
            SetupDefaultBehavior();

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) =>
                {
                    this.apiClientId = id;
                    this.clientInstance = instance;
                    return this.mockFixture.ApiClient.Object;
                });

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            string[] expectedCommands =
            {
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_common --tables=1 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",
                $"sudo {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "src/sysbench")} oltp_read_write --threads=1 --time=10 --tables=1 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup"
            };

            int commandNumber = 0;
            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {

                string expectedCommand = expectedCommands[commandNumber];
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }
                Assert.IsTrue(commandExecuted);
                commandExecuted = false;
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "SysbenchOLTP", "SysbenchOLTPExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPClientExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = true
            }));

            using (TestSysbenchOLTPClientExecutor SysbenchExecutor = new TestSysbenchOLTPClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private class TestSysbenchOLTPClientExecutor : SysbenchOLTPClientExecutor
        {
            public TestSysbenchOLTPClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
