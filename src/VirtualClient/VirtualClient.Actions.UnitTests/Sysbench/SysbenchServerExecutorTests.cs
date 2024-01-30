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
    using static VirtualClient.Actions.SysbenchExecutor;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchServerExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.mockFixture.Parameters["PackageName"] = "sysbench";
        }

        [Test]
        public async Task SysbenchOLTPServerExecutorInitializesScripts()
        {
            SetupDefaultBehavior();

            bool commandExecuted = false;

            using TestSysbenchOLTPServerExecutor executor = new TestSysbenchOLTPServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            string scriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("sysbench");

            string expectedCommand = $"sudo chmod -R 2777 \"{scriptPath}\"";

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                return this.mockFixture.Process;
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1500);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await executor.ExecuteAsync(cancellationToken);

            Assert.IsTrue(commandExecuted);
        }

        private class TestSysbenchOLTPServerExecutor : SysbenchServerExecutor
        {
            public TestSysbenchOLTPServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
