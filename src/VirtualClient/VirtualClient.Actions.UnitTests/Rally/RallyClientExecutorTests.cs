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
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.RallyClientExecutor;

    [TestFixture]
    [Category("Unit")]
    public class RallyClientExecutorTests
    {
        private MockFixture fixture;
        private string apiClientId;
        private ClientInstance clientInstance;
        private string packagePath;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();

            string clientAgentId = $"{Environment.MachineName}-Client";
            string serverAgentId = $"{Environment.MachineName}-Server";
            
            this.fixture.Setup(PlatformID.Unix, Architecture.X64, clientAgentId).SetupLayout(
                new ClientInstance(clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(serverAgentId, "1.2.3.5", "Server"));

            DependencyPath mockPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            this.packagePath = this.fixture.ToPlatformSpecificPath(mockPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            IEnumerable<Disk> disks = this.fixture.CreateDisks(PlatformID.Unix, withVolume: true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(RallyClientExecutor.PackageName), "esrally" },
                { nameof(RallyClientExecutor.TrackName), "geonames" }
            };

            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                .Returns("mockuser");

            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(clientAgentId);

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
        }

        [Test]
        public async Task RallyClientExecutorRunsTheExpectedWorkloadCommand()
        {
            string[] expectedCommands =
            {
                @$"python3 {this.packagePath}/install.py",
                @$"python3 {this.packagePath}/configure-client.py --directory /dev/sdd1 --user mockuser --clientIp 1.2.3.4",
                $"esrally race --track=geonames --distribution-version=8.0.0 --target-hosts=1.2.3.5:39200 --race-id=[0-9A-Fa-f]{{8}}-([0-9A-Fa-f]{{4}}-){{3}}[0-9A-Fa-f]{{12}}",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
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

                return process;
            };

            using (TestRallyClientExecutor RallyClientExecutor = new TestRallyClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyClientExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        public async Task RallyClientExecutorOnlyRunsWorkloadOnInitializedConfiguredState()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new RallyExecutor.RallyState()
            {
                RallyInitialized = true,
                RallyConfigured = true
            }));

            string[] expectedCommands =
            {
                $"esrally race --track=geonames --distribution-version=8.0.0 --target-hosts=1.2.3.5:39200 --race-id=[0-9A-Fa-f]{{8}}-([0-9A-Fa-f]{{4}}-){{3}}[0-9A-Fa-f]{{12}}",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Regex.Match($"{exe} {arguments}", expectedCommand).Success)
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

                return process;
            };

            using (TestRallyClientExecutor RallyClientExecutor = new TestRallyClientExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyClientExecutor.ExecuteAsync(CancellationToken.None);
            }
        }

        private class TestRallyClientExecutor : RallyClientExecutor
        {
            public TestRallyClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}