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
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.RallyServerExecutor;

    [TestFixture]
    [Category("Unit")]
    public class RallyServerExecutorTests
    {
        private MockFixture fixture;
        private string packagePath;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();

            this.fixture.Setup(PlatformID.Unix);

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.5", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.4", "Client")
            });

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            DependencyPath mockPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            this.packagePath = this.fixture.ToPlatformSpecificPath(mockPackage, PlatformID.Unix, Architecture.X64).Path;

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            IEnumerable<Disk> disks = this.fixture.CreateDisks(PlatformID.Unix, withVolume: true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            this.fixture.SystemManagement.Setup(mgr => mgr.GetLoggedInUserName())
                .Returns("mockuser");

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(RallyServerExecutor.PackageName), "esrally" },
            };
        }

        [Test]
        public async Task RallyServerExecutorConfiguresOnInitializationUnix()
        {
            string[] expectedCommands =
            {
                @$"python3 {this.packagePath}/install.py",
                @$"python3 {this.packagePath}/configure-server.py --directory /dev/sdd1 --user mockuser --clientIp 1.2.3.4 --serverIp 1.2.3.5",
            };

            int commandNumber = 0;
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                string expectedCommand = expectedCommands[commandNumber];

                if (Equals($"{exe} {arguments}", expectedCommand))
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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(300);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            using (TestRallyServerExecutor RallyExecutor = new TestRallyServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task RallyServerExecutorDoesNotConfigureOnConfiguredStateUnix()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new RallyExecutor.RallyState()
            {
                RallyInitialized = true,
                RallyConfigured = true
            }));

            int commandsExecuted = 0;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandsExecuted++;

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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(5000);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            using (TestRallyServerExecutor RallyExecutor = new TestRallyServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        private class TestRallyServerExecutor : RallyServerExecutor
        {
            public TestRallyServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
