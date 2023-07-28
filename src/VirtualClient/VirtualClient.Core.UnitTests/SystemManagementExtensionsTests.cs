// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Threading;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SystemManagementExtensionsTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            bool confirmed = false;
            string expectedBinary = "/home/any/path/to/VirtualClient";

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo");
                Assert.IsTrue(arguments == $"chmod +x \"{expectedBinary}\"");
                confirmed = true;

                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeAllFilesExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            bool confirmed = false;
            string expectedDirectory = "/home/any/path/to/scripts";

            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo");
                Assert.IsTrue(arguments == $"chmod -R 2777 \"{expectedDirectory}\"");
                confirmed = true;

                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFilesExecutableAsync(expectedDirectory, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionDoesNothingToMakeABinaryExecutableOnAWindowsSystem()
        {
            bool processExecuted = false;
            string expectedBinary = @"C:\any\path\to\VirtualClient.exe";

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                processExecuted = true;
                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Win32NT, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsFalse(processExecuted);
        }
    }
}
