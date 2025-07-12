// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Renci.SshNet;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    internal class SshClientProxyTests : MockFixture
    {
        private IFileSystem fileSystem;

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);
            this.fileSystem = new FileSystem();
        }

        [Test]
        public void SshClientProxyConstructorsValidatesRequiredProperties()
        {
            Assert.Throws<ArgumentException>(() => new SshClientProxy(null));
        }

        [Test]
        public void SshClientProxyConstructorsSetPropertiesToExpectedValues()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new SshClientProxy(expectedInfo))
            {
                Assert.IsNotNull(client.SessionClient);
                Assert.IsNotNull(client.SessionScpClient);
                Assert.IsTrue(object.ReferenceEquals(expectedInfo, client.ConnectionInfo));
                Assert.IsTrue(object.ReferenceEquals(expectedInfo, client.SessionClient.ConnectionInfo));
                Assert.IsTrue(object.ReferenceEquals(expectedInfo, client.SessionScpClient.ConnectionInfo));
            }
        }

        [Test]
        public async Task SshClientProxyCopiesTheExpectedFileFromTheRemoteSystemToAStream()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                using (var expectedStream = new MemoryStream())
                {
                    bool verified = false;
                    string expectedFilePath = "/remote/any-toolset.log";

                    client.OnCopyFileFromRemoteSystem = (remoteFilePath, stream, file) =>
                    {
                        Assert.AreEqual(expectedFilePath, remoteFilePath);
                        Assert.IsTrue(object.ReferenceEquals(expectedStream, stream));
                        verified = true;
                    };

                    await client.CopyFromAsync(expectedFilePath, expectedStream);
                    Assert.IsTrue(verified);
                }
            }
        }

        [Test]
        public async Task SshClientProxyCopiesTheExpectedFileFromTheRemoteSystemToAFileOnTheLocalSystem()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                bool verified = false;
                string expectedFilePath = "/remote/any-toolset.log";
                IFileInfo expectedFile = this.fileSystem.FileInfo.New("/local/any-toolset.log");

                client.OnCopyFileFromRemoteSystem = (remoteFilePath, stream, localFile) =>
                {
                    Assert.AreEqual(expectedFilePath, remoteFilePath);
                    Assert.IsTrue(object.ReferenceEquals(expectedFile, localFile));
                    verified = true;
                };

                await client.CopyFromAsync(expectedFilePath, expectedFile);
                Assert.IsTrue(verified);
            }
        }

        [Test]
        public async Task SshClientProxyCopiesTheExpectedDirectoryFromTheRemoteSystemToADirectoryOnTheLocalSystem()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                bool verified = false;
                string expectedDirectoryPath = "/remote";
                IDirectoryInfo expectedDirectory = this.fileSystem.DirectoryInfo.New("/local");

                client.OnCopyDirectoryFromRemoteSystem = (remoteDirectoryPath, actualDirectory) =>
                {
                    Assert.AreEqual(expectedDirectoryPath, remoteDirectoryPath);
                    Assert.IsTrue(object.ReferenceEquals(expectedDirectory, actualDirectory));
                    verified = true;
                };

                await client.CopyFromAsync(expectedDirectoryPath, expectedDirectory);
                Assert.IsTrue(verified);
            }
        }

        [Test]
        public async Task SshClientProxyCopiesFromAStreamToTheExpectedFileOnTheRemoteSystem()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                using (var expectedStream = new MemoryStream())
                {
                    bool verified = false;
                    string expectedRemoteFilePath = "/remote/any-toolset.log";

                    client.OnCopyFileToRemoteSystem = (stream, file, remoteFilePath) =>
                    {
                        Assert.AreEqual(expectedRemoteFilePath, remoteFilePath);
                        Assert.IsTrue(object.ReferenceEquals(expectedStream, stream));
                        verified = true;
                    };

                    await client.CopyToAsync(expectedStream, expectedRemoteFilePath);
                    Assert.IsTrue(verified);
                }
            }
        }

        [Test]
        public async Task SshClientProxyCopiesFromALocalFileToTheExpectedFileOnTheRemoteSystem()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                bool verified = false;
                string expectedRemoteFilePath = "/remote/any-toolset.log";
                IFileInfo expectedLocalFile = this.fileSystem.FileInfo.New("/local/any-toolset.log");

                client.OnCopyFileToRemoteSystem = (stream, localFile, remoteFilePath) =>
                {
                    Assert.AreEqual(expectedRemoteFilePath, remoteFilePath);
                    Assert.IsTrue(object.ReferenceEquals(expectedLocalFile, localFile));
                    verified = true;
                };

                await client.CopyToAsync(expectedLocalFile, expectedRemoteFilePath);
                Assert.IsTrue(verified);
            }
        }

        [Test]
        public async Task SshClientProxyCopiesFromALocalDirectoryToTheExpectedDirectoryOnTheRemoteSystem()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                bool verified = false;
                string expectedRemoteDirectoryPath = "/remote";
                IDirectoryInfo expectedLocalDirectory = this.fileSystem.DirectoryInfo.New("/local");

                client.OnCopyDirectoryToRemoteSystem = (localDirectory, remoteDirectoryPath) =>
                {
                    Assert.AreEqual(expectedRemoteDirectoryPath, remoteDirectoryPath);
                    Assert.IsTrue(object.ReferenceEquals(expectedLocalDirectory, localDirectory));
                    verified = true;
                };

                await client.CopyToAsync(expectedLocalDirectory, expectedRemoteDirectoryPath);
                Assert.IsTrue(verified);
            }
        }

        [Test]
        public async Task SshClientProxyExecutesTheExpectedCommandThroughTheSshSession()
        {
            ConnectionInfo expectedInfo = new ConnectionInfo("192.168.1.15", "anyuser", new TestAuthenticationMethod("anyuser"));
            using (var client = new TestSshClientProxy(expectedInfo))
            {
                bool verified = false;
                string expectedCommand = "python execute_this_or_that.py --log-dir /home/user/logs";

                client.OnExecuteCommandOnRemoteSystem = (sshCommand) =>
                {
                    Assert.AreEqual(expectedCommand, sshCommand);
                    verified = true;

                    return new ProcessDetails
                    {
                        Id = 1234,
                        CommandLine = expectedCommand,
                        ExitCode = 0,
                        StandardOutput = "Command executed",
                        ToolName = "SSH"
                    };
                };

                ProcessDetails result = await client.ExecuteCommandAsync(expectedCommand, CancellationToken.None);

                Assert.IsTrue(verified);
                Assert.IsNotNull(result);
                Assert.AreEqual(expectedCommand, result.CommandLine);
            }
        }

        internal class TestAuthenticationMethod : AuthenticationMethod
        {
            public TestAuthenticationMethod(string username) 
                : base(username)
            {
            }

            public override string Name { get; } = nameof(TestAuthenticationMethod);

            public override AuthenticationResult Authenticate(Session session)
            {
                return AuthenticationResult.Success;
            }
        }

        private class TestSshClientProxy : SshClientProxy
        {
            public TestSshClientProxy(ConnectionInfo connectionInfo)
                : base(connectionInfo)
            {
            }

            /// <summary>
            /// Delegate defines logic to execute when the 'CopyDirectoryFromRemoteSystemAsync' method is called.
            /// </summary>
            public Action<string, IDirectoryInfo> OnCopyDirectoryFromRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'CopyFileFromRemoteSystemAsync' method is called.
            /// </summary>
            public Action<string, Stream, IFileInfo> OnCopyFileFromRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'CopyDirectoryToRemoteSystemAsync' method is called.
            /// </summary>
            public Action<IDirectoryInfo, string> OnCopyDirectoryToRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'CopyFileToRemoteSystem' method is called.
            /// </summary>
            public Action<Stream, IFileInfo, string> OnCopyFileToRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'CreateDirectoryOnRemoteSystemAsync' method is called.
            /// </summary>
            public Action<string> OnCreateDirectoryOnRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'DeleteDirectoryOnRemoteSystemAsync' method is called.
            /// </summary>
            public Action<string> OnDeleteDirectoryOnRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'DeleteFileOnRemoteSystemAsync' method is called.
            /// </summary>
            public Action<string> OnDeleteFileOnRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'ExecuteCommandOnRemoteSystemAsync' method is called.
            /// </summary>
            public Func<string, ProcessDetails> OnExecuteCommandOnRemoteSystem { get; set; }

            /// <summary>
            /// Delegate defines logic to execute when the 'ExistsOnRemoteSystemAsync' method is called.
            /// </summary>
            public Func<string, bool> OnExistsOnRemoteSystem { get; set; }

            protected override Task CopyDirectoryFromRemoteSystemAsync(string remoteDirectoryPath, IDirectoryInfo destination, CancellationToken cancellationToken)
            {
                this.OnCopyDirectoryFromRemoteSystem?.Invoke(remoteDirectoryPath, destination);
                return Task.CompletedTask;
            }

            protected override Task CopyFileFromRemoteSystemAsync(string remoteFilePath, Stream destination, CancellationToken cancellationToken)
            {
                this.OnCopyFileFromRemoteSystem?.Invoke(remoteFilePath, destination, null);
                return Task.CompletedTask;
            }


            protected override Task CopyFileFromRemoteSystemAsync(string remoteFilePath, IFileInfo destination, CancellationToken cancellationToken)
            {
                this.OnCopyFileFromRemoteSystem?.Invoke(remoteFilePath, null, destination);
                return Task.CompletedTask;
            }

            protected override Task CopyToDirectoryOnRemoteSystemAsync(IDirectoryInfo source, string remoteDirectoryPath, CancellationToken cancellationToken)
            {
                this.OnCopyDirectoryToRemoteSystem?.Invoke(source, remoteDirectoryPath);
                return Task.CompletedTask;
            }

            protected override Task CopyToFileOnRemoteSystemAsync(Stream source, string remoteFilePath, CancellationToken cancellationToken)
            {
                this.OnCopyFileToRemoteSystem?.Invoke(source, null, remoteFilePath);
                return Task.CompletedTask;
            }

            protected override Task CopyToFileOnRemoteSystemAsync(IFileInfo source, string remoteFilePath, CancellationToken cancellationToken)
            {
                this.OnCopyFileToRemoteSystem?.Invoke(null, source, remoteFilePath);
                return Task.CompletedTask;
            }

            protected override Task CreateDirectoryOnRemoteSystemAsync(string remoteDirectoryPath, PlatformID targetPlatform, CancellationToken cancellationToken)
            {
                this.OnCreateDirectoryOnRemoteSystem?.Invoke(remoteDirectoryPath);
                return Task.CompletedTask;
            }

            protected override Task DeleteDirectoryOnRemoteSystemAsync(string remoteDirectoryPath, PlatformID targetPlatform, CancellationToken cancellationToken)
            {
                this.OnDeleteDirectoryOnRemoteSystem?.Invoke(remoteDirectoryPath);
                return Task.CompletedTask;
            }

            protected override Task DeleteFileOnRemoteSystemAsync(string remoteFilePath, PlatformID targetPlatform, CancellationToken cancellationToken)
            {
                this.OnDeleteFileOnRemoteSystem?.Invoke(remoteFilePath);
                return Task.CompletedTask;
            }

            protected override Task<ProcessDetails> ExecuteCommandOnRemoteSystemAsync(string command, CancellationToken cancellationToken, Action<SshCommandOutputInfo> outputReceived = null)
            {
                outputReceived?.Invoke(new SshCommandOutputInfo($"Command '{command}' executed successfully", TimeSpan.FromSeconds(10)));

                return Task.FromResult(this.OnExecuteCommandOnRemoteSystem?.Invoke(command) ?? new ProcessDetails
                {
                    Id = command.GetHashCode(),
                    CommandLine = command,
                    ExitCode = 0,
                    StandardOutput = $"Command '{command}' executed successfully",
                    ToolName = "SSH"
                });
            }

            protected override Task<bool> ExistsOnRemoteSystemAsync(string remotePath, PlatformID targetPlatform, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.OnExistsOnRemoteSystem?.Invoke(remotePath) ?? false);
            }

            protected override void ValidateSessionConnection()
            {
                // Allow unit tests to continue.
            }
        }
    }
}
