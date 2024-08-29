// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SystemManagementExtensionsTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            this.fixture.SetupMocks();
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            bool confirmed = false;
            string expectedBinary = "/home/any/path/to/VirtualClient";

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
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

            this.fixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeAllFilesExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            bool confirmed = false;
            string expectedDirectory = "/home/any/path/to/scripts";

            this.fixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
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

            this.fixture.SystemManagement.Object.MakeFilesExecutableAsync(expectedDirectory, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionDoesNothingToMakeABinaryExecutableOnAWindowsSystem()
        {
            bool processExecuted = false;
            string expectedBinary = @"C:\any\path\to\VirtualClient.exe";

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                processExecuted = true;
                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.fixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Win32NT, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsFalse(processExecuted);
        }
    }
}
