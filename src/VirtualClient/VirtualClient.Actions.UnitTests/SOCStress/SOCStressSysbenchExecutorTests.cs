// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;
    using Renci.SshNet;

    [TestFixture]
    [Category("Unit")]
    public class SOCStressSysbenchExecutorTests
    {
        private MockFixture fixture;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();

            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.Host)] = "mockHost";
            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.UserName)] = "mockUserName";
            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.Password)] = "mockPassword";
            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.SysbenchMemoryCommandLine)] = "--test=memory --memory-block-size=1M --memory-total-size=200T --num-threads=8 --max-time=[sysbenchtimeout] run";
            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.SysbenchCPUCommandLine)] = "--test=cpu --num-threads=8 --cpu-max-prime=2000000 --max-time=[sysbenchtimeout] run";
            this.fixture.Parameters[nameof(SOCStressSysbenchExecutor.SysbenchTimeout)] = "300";

            this.rawText = File.ReadAllText(@"Examples\SOCStress\SysbenchOutputExample.txt");
        }

        [Test]
        public void SOCStressSysbenchExecutorThrowsOnNonZeroExitStatus()
        {
            this.fixture.SshClientManager.OnCreateSshClient = (host, userName, password) =>
            {
                InMemorySshClient sshClient = new InMemorySshClient();

                sshClient.OnCreateCommand = (commandText) =>
                {
                    InMemorySshCommand sshCommand = new InMemorySshCommand();
                    sshCommand.CommandText = commandText;
                    sshCommand.ExitStatus = 1;
                    sshCommand.Error = "MockErrorMessage";

                    sshCommand.OnExecute = () =>
                    {
                        return this.rawText;
                    };

                    return sshCommand;
                };

                return sshClient;
            };

            using (SOCStressSysbenchExecutor socStressSysbenchExecutor = new SOCStressSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException expection = Assert.ThrowsAsync<WorkloadException>(() => socStressSysbenchExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(expection.Reason, ErrorReason.WorkloadFailed);
            }
        }

        [Test]
        public async Task SOCStressSysbenchExecutorRunningAsExpected()
        { 
            List <string> expectedCommads = new List <string>
            {
                "sysbench --test=memory --memory-block-size=1M --memory-total-size=200T --num-threads=8 --max-time=300 run > mem.txt & sysbench --test=cpu --num-threads=8 --cpu-max-prime=2000000 --max-time=300 run > cpu.txt",
                "cat mem.txt",
                "cat cpu.txt",
                "rm mem.txt & rm cpu.txt"
            };
            this.fixture.SshClientManager.OnCreateSshClient = (host, userName, password) =>
            {
                InMemorySshClient sshClient = new InMemorySshClient();

                Assert.IsTrue(host.Equals("mockHost"));
                Assert.IsTrue(userName.Equals("mockUserName"));
                Assert.IsTrue(password.Equals("mockPassword"));
                
                sshClient.OnCreateCommand = (commandText) =>
                {
                    Assert.IsTrue(expectedCommads.Contains(commandText));
                    InMemorySshCommand sshCommand = new InMemorySshCommand();
                    sshCommand.CommandText = commandText;

                    sshCommand.OnExecute = () =>
                    {
                        return this.rawText;
                    };

                    return sshCommand;
                };

                return sshClient;
            };

            using (SOCStressSysbenchExecutor socStressSysbenchExecutor = new SOCStressSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await socStressSysbenchExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }
}
