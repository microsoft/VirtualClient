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
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.SysbenchExecutor;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchClientExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string apiClientId;
        private ClientInstance clientInstance;
        private string mockPackagePath;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("sysbench", this.fixture.PlatformSpecifics.GetPackagePath("sysbench"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SysbenchClientExecutor.DatabaseSystem), "MySQL" },
                { nameof(SysbenchClientExecutor.Benchmark), "OLTP" },
                { nameof(SysbenchClientExecutor.DatabaseName), "sbtest" },
                { nameof(SysbenchClientExecutor.Duration), "00:00:10" },
                { nameof(SysbenchClientExecutor.Workload), "oltp_read_write" },
                { nameof(SysbenchClientExecutor.PackageName), "sysbench" },
                { nameof(SysbenchClientExecutor.Scenario), "oltp_read_write_testing" }
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

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchExecutor.SysbenchState()
            {
                SysbenchInitialized = true
            }));

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 4, 8, 4, 4, false));
        }

        [Test]
        public async Task SysbenchClientExecutorRunsTheExpectedWorkloadCommand()
        {
            SetupDefaultBehavior();

            string expectedCommand = @$"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorUsesDefinedParametersWhenRunningTheWorkload()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.Threads)] = "64";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.RecordCount)] = "1000";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.TableCount)] = "40";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Configure";

            string expectedCommand = @$"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 64 --tableCount 40 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorRunsTheExpectedBalancedScenario()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "Balanced";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorRunsInMemoryScenario()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseScenario)] = "InMemory";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 100000 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorRunsTheExpectedTPCCWorkloadCommand()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.Benchmark)] = "TPCC";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem MySQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorRunsTheExpectedPostgreSQLOLTPWorkloadCommand()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseSystem)] = "PostgreSQL";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark OLTP --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task SysbenchClientExecutorRunsTheExpectedPostgreSQLTPCCWorkloadCommand()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(SysbenchClientExecutor.DatabaseSystem)] = "PostgreSQL";
            this.fixture.Parameters[nameof(SysbenchClientExecutor.Benchmark)] = "TPCC";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --databaseSystem PostgreSQL --benchmark TPCC --workload tpcc --threadCount 8 --tableCount 10 --warehouses 100 --hostIpAddress 1.2.3.5 --durationSecs 10 --password [A-Za-z0-9+/=]+";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Sysbench", "SysbenchExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestSysbenchClientExecutor SysbenchExecutor = new TestSysbenchClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await SysbenchExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        private class TestSysbenchClientExecutor : SysbenchClientExecutor
        {
            public TestSysbenchClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
