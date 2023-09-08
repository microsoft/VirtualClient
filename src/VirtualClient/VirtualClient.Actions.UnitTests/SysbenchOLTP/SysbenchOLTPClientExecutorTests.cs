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
    using static VirtualClient.Actions.SysbenchOLTPExecutor;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchOLTPClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private ClientInstance clientInstance;
        private string scriptPath;
        private string mockPackagePath;

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
                { nameof(SysbenchOLTPClientExecutor.Duration), "00:00:10" },
                { nameof(SysbenchOLTPClientExecutor.Workload), "oltp_read_write" },
                { nameof(SysbenchOLTPClientExecutor.PackageName), "sysbench" },
            };

            string agentId = $"{Environment.MachineName}";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.mockFixture.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) =>
                {
                    this.apiClientId = id;
                    this.clientInstance = instance;
                    return this.mockFixture.ApiClient.Object;
                });

            this.mockPackagePath = this.mockPackage.Path;
            this.scriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("sysbencholtp");

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 4, 8, 4, 4, false));
        }

        [Test]
        public async Task SysbenchOLTPClientExecutorRunsTheExpectedWorkloadCommand()
        {
            SetupDefaultBehavior();

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = false,
                NumTables = -1,
                RecordCount = -1
            }));

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{this.scriptPath}/balanced-server.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/balanced-client.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/in-memory.sh\"",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=64 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 cleanup",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_common --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=64 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 run"
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
        public async Task SysbenchOLTPClientExecutorUsesDefinedParameters()
        {
            SetupDefaultBehavior();

            this.mockFixture.Parameters[nameof(SysbenchOLTPClientExecutor.Threads)] = "8";
            this.mockFixture.Parameters[nameof(SysbenchOLTPClientExecutor.RecordCount)] = "1000";
            this.mockFixture.Parameters[nameof(SysbenchOLTPClientExecutor.NumTables)] = "40";

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = false
            }));

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{this.scriptPath}/balanced-server.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/balanced-client.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/in-memory.sh\"",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=40 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 cleanup",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_common --tables=40 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=8 --tables=40 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 run"
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
        public async Task SysbenchOLTPClientExecutorRunsTheExpectedWorkloadCommandBalancedScenario()
        {
            SetupDefaultBehavior();

            SysbenchOLTPState expectedState = new SysbenchOLTPState(new Dictionary<string, IConvertible>
            {
                [nameof(SysbenchOLTPState.DiskPathsArgument)] = "/testdrive1 /testdrive2"
            });

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(nameof(SysbenchOLTPState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, expectedState));

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = false
            }));

            this.mockFixture.Parameters["DatabaseScenario"] = "Balanced";

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{this.scriptPath}/balanced-server.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/balanced-client.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/in-memory.sh\"",
                "sudo ./autogen.sh",
                "sudo ./configure",
                "sudo make -j",
                "sudo make install",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=1 --tables=10 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 cleanup",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_common --tables=10 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo {this.scriptPath}/balanced-client.sh 1.2.3.5 10 sbtest /testdrive1 /testdrive2",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=1 --tables=10 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 run"
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
            SetupDefaultBehavior();

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{this.scriptPath}/balanced-server.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/balanced-client.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/in-memory.sh\"",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=64 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 cleanup",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_common --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=64 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 run"
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = true,
                NumTables = -1,
                RecordCount = -1
            }));

            using (TestSysbenchOLTPClientExecutor SysbenchExecutor = new TestSysbenchOLTPClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SysbenchOLTPClientExecutorSkipsPrepareAndCleanupSteps()
        {
            SetupDefaultBehavior();

            this.mockFixture.Parameters["SkipInitialize"] = true;

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                SysbenchInitialized = true,
                DatabaseScenarioInitialized = true,
                NumTables = 10,
                RecordCount = 100000
            }));

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{this.scriptPath}/balanced-server.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/balanced-client.sh\"",
                $"sudo chmod +x \"{this.scriptPath}/in-memory.sh\"",
                $"sudo /home/user/tools/VirtualClient/packages/sysbench/src/sysbench oltp_read_write --threads=64 --tables=10 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 --time=10 run",
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
