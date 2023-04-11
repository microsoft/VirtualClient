// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.SOCStress
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
    public class SOCStressFPGAFactoryTesterExecutorTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();

            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.Host)] = "mockHost";
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.UserName)] = "mockUserName";
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.Password)] = "mockPassword";
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.FPGAFactoryTesterTimeout)] = "300";
        }

        [Test]
        public void SOCStressFPGAFactoryTesterExecutorThrowsOnNonZeroExitStatus()
        {
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCDRAM)] = false;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPCIe)] = false;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPowerVirus)] = false;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.FPGAStressVerbose)] = true;

            this.fixture.SshClientManager.OnCreateSshClient = (host, userName, password) =>
            {
                InMemorySshClient sshClient = new InMemorySshClient();

                sshClient.OnCreateCommand = (commandText) =>
                {
                    InMemorySshCommand sshCommand = new InMemorySshCommand();
                    sshCommand.CommandText = commandText;
                    sshCommand.ExitStatus = 1;
                    sshCommand.Error = "MockErrorMessage";

                    return sshCommand;
                };

                return sshClient;
            };

            using (SOCStressFPGAFactoryTesterExecutor executor = new SOCStressFPGAFactoryTesterExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException expection = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(expection.Reason, ErrorReason.WorkloadFailed);
            }
        }

        [Test]
        [TestCase(false, false, false, false, "sudo fpgafactorytester -duration 300")]
        [TestCase(false, false, false, true, "sudo fpgafactorytester -duration 300 -verbose")]
        [TestCase(false, false, true, false, "sudo fpgafactorytester -duration 300 -virus 0")]
        [TestCase(false, false, true, true, "sudo fpgafactorytester -duration 300 -virus 0 -verbose")]
        [TestCase(false, true, false, false, "sudo fpgafactorytester -duration 300 -dram 0")]
        [TestCase(false, true, false, true, "sudo fpgafactorytester -duration 300 -dram 0 -verbose")]
        [TestCase(false, true, true, false, "sudo fpgafactorytester -duration 300 -dram 0 -virus 0")]
        [TestCase(false, true, true, true, "sudo fpgafactorytester -duration 300 -dram 0 -virus 0 -verbose")]
        [TestCase(true, false, false, false, "sudo fpgafactorytester -duration 300 -pcie 0")]
        [TestCase(true, false, false, true, "sudo fpgafactorytester -duration 300 -pcie 0 -verbose")]
        [TestCase(true, false, true, false, "sudo fpgafactorytester -duration 300 -pcie 0 -virus 0")]
        [TestCase(true, false, true, true, "sudo fpgafactorytester -duration 300 -pcie 0 -virus 0 -verbose")]
        [TestCase(true, true, false, false, "sudo fpgafactorytester -duration 300 -pcie 0 -dram 0")]
        [TestCase(true, true, false, true, "sudo fpgafactorytester -duration 300 -pcie 0 -dram 0 -verbose")]
        [TestCase(true, true, true, false, "sudo fpgafactorytester -duration 300 -pcie 0 -dram 0 -virus 0")]
        [TestCase(true, true, true, true, "sudo fpgafactorytester -duration 300 -pcie 0 -dram 0 -virus 0 -verbose")]
        public async Task SOCStressFPGAFactoryTesterExecutorRunningAsExpected(bool disableSOCPCIe, bool disableSOCDRAM, bool disableSOCPowerVirus, bool fpgaStressVerbose, string expectedCommand)
        {
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCDRAM)] = disableSOCDRAM;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPCIe)] = disableSOCPCIe;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPowerVirus)] = disableSOCPowerVirus;
            this.fixture.Parameters[nameof(SOCStressFPGAFactoryTesterExecutor.FPGAStressVerbose)] = fpgaStressVerbose;

            this.fixture.SshClientManager.OnCreateSshClient = (host, userName, password) =>
            {
                InMemorySshClient sshClient = new InMemorySshClient();

                Assert.IsTrue(host.Equals("mockHost"));
                Assert.IsTrue(userName.Equals("mockUserName"));
                Assert.IsTrue(password.Equals("mockPassword"));

                sshClient.OnCreateCommand = (commandText) =>
                {
                    Assert.IsTrue(expectedCommand.Contains(commandText));
                    InMemorySshCommand sshCommand = new InMemorySshCommand();
                    sshCommand.CommandText = commandText;

                    return sshCommand;
                };

                return sshClient;
            };

            using (SOCStressFPGAFactoryTesterExecutor socStressFPGAFactoryTesterExecutor = new SOCStressFPGAFactoryTesterExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await socStressFPGAFactoryTesterExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }
    }
}
