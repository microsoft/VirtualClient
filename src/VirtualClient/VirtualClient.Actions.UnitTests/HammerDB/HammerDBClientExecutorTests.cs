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

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("HammerDB", this.fixture.PlatformSpecifics.GetPackagePath("HammerDB"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockPackagePath = this.mockPackage.Path;

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(HammerDBClientExecutor.DatabaseName), "sbtest" },
                { nameof(HammerDBClientExecutor.Duration), "00:00:10" },
                { nameof(HammerDBClientExecutor.Workload), "oltp_read_write" },
                { nameof(HammerDBClientExecutor.PackageName), "HammerDB" },
                { nameof(HammerDBClientExecutor.Scenario), "oltp_read_write_testing" }
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

        [Test]
        public async Task HammerDBClientExecutorRunsTheExpectedWorkloadCommand()
        {
            SetupDefaultBehavior();

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB", "HammerDBExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBClientExecutorUsesDefinedParametersWhenRunningTheWorkload()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(HammerDBClientExecutor.Threads)] = "64";
            this.fixture.Parameters[nameof(HammerDBClientExecutor.Scenario)] = "Configure";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 64 --tableCount 40 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB", "HammerDBExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBClientExecutorRunsTheExpectedBalancedScenario()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(HammerDBClientExecutor.DatabaseScenario)] = "Balanced";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB", "HammerDBExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task HammerDBClientExecutorRunsInMemoryScenario()
        {
            SetupDefaultBehavior();

            this.fixture.Parameters[nameof(HammerDBClientExecutor.DatabaseScenario)] = "InMemory";

            string expectedCommand = $"python3 {this.mockPackagePath}/run-workload.py --dbName sbtest --workload oltp_read_write --threadCount 8 --tableCount 10 --recordCount 1000 --hostIpAddress 1.2.3.5 --durationSecs 10";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
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

                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB", "HammerDBExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));

                return process;
            };

            using (TestHammerDBClientExecutor HammerDBExecutor = new TestHammerDBClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
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
