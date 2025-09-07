// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Renci.SshNet;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    internal class ExecuteSshCommandTests : MockFixture
    {
        private ConnectionInfo mockConnection;
        private Mock<ISshClientProxy> mockSshClient;

        public void SetupTest(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);

            string mockCommand = "bash -c \"anycommand.sh -argument1 1234 -argument2 5678\"";
            this.Parameters[nameof(ExecuteSshCommand.Command)] = mockCommand;

            this.mockConnection = new ConnectionInfo("192.168.1.15", "user01", new PasswordAuthenticationMethod("user01", "pw"));
            this.mockSshClient = new Mock<ISshClientProxy>();
            
            // Setup:
            // The SSH client executes a command and returns a valid result.
            this.mockSshClient.Setup(client => client.ConnectionInfo)
                .Returns(this.mockConnection);

            this.mockSshClient.Setup(client => client.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<Action<SshCommandOutputInfo>>()))
                .ReturnsAsync(new ProcessDetails
                {
                    Id = 1234,
                    CommandLine = mockCommand,
                    StandardOutput = $"Executed command"
                });

            this.Dependencies.AddSingleton<IEnumerable<ISshClientProxy>>(new List<ISshClientProxy> { this.mockSshClient.Object });
        }

        [Test]
        public void ExecuteSshCommandCannotBeRanAsAMonitor()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestExecuteSshCommand(this))
            {
                component.ComponentType = ComponentType.Monitor;
                Assert.ThrowsAsync<NotSupportedException>(() => component.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase("bash -c \"anycommand.sh -argument1 11 -argument2 22\"")]
        [TestCase("cmd -c \"anycommand.cmd -argument1 11 -argument2 22\"")]
        public async Task ExecuteSshCommandExecutesTheExpectedCommand(string expectedCommand)
        {
            this.SetupTest(PlatformID.Unix);
            this.Parameters[nameof(ExecuteSshCommand.Command)] = expectedCommand;

            using (var component = new TestExecuteSshCommand(this))
            {
                this.mockSshClient.Setup(client => client.ExecuteCommandAsync(expectedCommand, It.IsAny<CancellationToken>(), It.IsAny<Action<SshCommandOutputInfo>>()))
                    .ReturnsAsync(new ProcessDetails
                    {
                        Id = 1234,
                        CommandLine = expectedCommand,
                        StandardOutput = $"Executed '{expectedCommand}'"
                    });

                await component.ExecuteAsync(CancellationToken.None);

                this.mockSshClient.Verify(client => client.ExecuteCommandAsync(expectedCommand, It.IsAny<CancellationToken>(), It.IsAny<Action<SshCommandOutputInfo>>()), Times.Once);
            }
        }

        private class TestExecuteSshCommand : ExecuteSshCommand
        {
            public TestExecuteSshCommand(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }
        }
    }
}
