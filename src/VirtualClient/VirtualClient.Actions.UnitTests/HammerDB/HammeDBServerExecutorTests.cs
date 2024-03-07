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
    using static VirtualClient.Actions.HammerDBExecutor;

    [TestFixture]
    [Category("Unit")]
    public class HammerDBServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            string agentId = $"{Environment.MachineName}-Server";
            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.mockPackage = new DependencyPath("HammerDB", this.fixture.PlatformSpecifics.GetPackagePath("HammerDB"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.Parameters["PackageName"] = "HammerDB";
        }

        [Test]
        public async Task HammerDBServerExecutorSkipsHammerDBInitializationWhenInitialized()
        {
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState()
            {
                HammerDBInitialized = true
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
            cancellationTokenSource.CancelAfter(1500);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            using (TestHammerDBServerExecutor HammerDBExecutor = new TestHammerDBServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await HammerDBExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        private class TestHammerDBServerExecutor : HammerDBServerExecutor
        {
            public TestHammerDBServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
