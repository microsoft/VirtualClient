// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using NUnit.Framework;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    internal class ProcessExtensionsTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        public void CreateElevatedProcessExtensionCreatesTheExpectedProcessOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, command, commandArguments, workingDirectory))
            {
                Assert.IsNotNull(process.StartInfo);
                Assert.AreEqual(command, process.StartInfo.FileName);
                Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
            }
        }

        [Test]
        public void CreateElevatedProcessExtensionCreatesTheExpectedProcessOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcess(PlatformID.Unix, command, commandArguments, workingDirectory))
            {
                Assert.IsNotNull(process.StartInfo);
                Assert.AreEqual("sudo", process.StartInfo.FileName);
                Assert.AreEqual($"{command} {commandArguments}", process.StartInfo.Arguments);
                Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
            }
        }

        [Test]
        public void CreateElevatedProcessExtensionCreatesTheExpectedProcessOnUnixSystemsWhenAUsernameIsProvided()
        {
            this.SetupDefaults(PlatformID.Unix);

            string username = "anyuser";
            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcess(PlatformID.Unix, command, commandArguments, workingDirectory, username))
            {
                Assert.IsNotNull(process.StartInfo);
                Assert.AreEqual("sudo", process.StartInfo.FileName);
                Assert.AreEqual($"-u {username} {command} {commandArguments}", process.StartInfo.Arguments);
                Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
            }
        }

        [Test]
        public void CreateElevatedProcessExtensionThrowsWhenAUsernameIsProvidedOnWindowsSystems_NotSupported()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            Assert.Throws<NotSupportedException>(
                () => this.mockFixture.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, "anycommand", "anyarguments", "anydir", "anyusername"));
        }
    }
}
