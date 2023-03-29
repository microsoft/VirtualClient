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
    public class SOCStressExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string rawText;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();

            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.Host)] = "mockHost";
            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.UserName)] = "mockUserName";
            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.Password)] = "mockPassword";
            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.SysbenchMemoryParameters)] = "--test=memory --memory-block-size=1M --memory-total-size=200T --num-threads=8 --max-time=[sysbenchtimeout] run";
            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.SysbenchCPUParameters)] = "--test=cpu --num-threads=8 --cpu-max-prime=2000000 --max-time=[sysbenchtimeout] run";
            this.fixture.Parameters[nameof(TestSOCStressSysbenchExecutor.SysbenchTimeout)] = "300";

            this.rawText = File.ReadAllText(@"Examples\SOCStress\SysbenchOutputExample.txt");
        }

        [Test]
        public async Task SOCPackageIsNotFound(PlatformID platform)
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

            using (TestSOCStressSysbenchExecutor testSOCStressSysbenchExecutor = new TestSOCStressSysbenchExecutor(this.fixture))
            {
                await testSOCStressSysbenchExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private class TestSOCStressSysbenchExecutor : SOCStressSysbenchExecutor
        {
            public TestSOCStressSysbenchExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }
        }
    }
}
