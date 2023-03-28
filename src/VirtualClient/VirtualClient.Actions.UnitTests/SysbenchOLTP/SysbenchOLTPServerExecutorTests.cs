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
    public class SysbenchOLTPServerExecutorTests
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
        }

        [Test]
        public async Task SysbenchOLTPServerExecutorExcutesExpectedProcesses()
        {
            SetupDefaultBehavior();
            int commandExecuted = 0;
            using TestSysbenchOLTPServerExecutor executor = new TestSysbenchOLTPServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            string[] expectedCommands =
{
                $"sudo mysql --execute=\"DROP USER IF EXISTS 'sbtest'@'1.2.3.5'\"",
                $"sudo sed -i \"s/.*bind-address.*/bind-address = 1.2.3.4/\" /etc/mysql/mysql.conf.d/mysqld.cnf",
                $"sudo systemctl restart mysql.service",
                $"sudo mysql --execute=\"CREATE USER 'sbtest'@'1.2.3.5'\"",
                $"sudo mysql --execute=\"GRANT ALL ON sbtest.* TO 'sbtest'@'1.2.3.5'\""
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                return this.mockFixture.Process;
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1000);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await executor.ExecuteAsync(cancellationToken);

            Assert.AreEqual(5, commandExecuted);
        }

        private class TestSysbenchOLTPServerExecutor : SysbenchOLTPServerExecutor
        {
            public TestSysbenchOLTPServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
