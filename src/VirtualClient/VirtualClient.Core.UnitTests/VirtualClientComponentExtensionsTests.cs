// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class VirtualClientComponentExtensionsTests
    {
        private MockFixture fixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform);
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnWindowsSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            // There is no different on Windows systems.
            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual(command, process.StartInfo.FileName);
                    Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual("sudo", process.StartInfo.FileName);
                    Assert.AreEqual($"{command} {commandArguments}", process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public async Task ExecuteCommandAsyncExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevatedAndAUsernameIsSupplied()
        {
            this.SetupDefaults(PlatformID.Unix);

            string username = "anyuser";
            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                using (IProcessProxy process = await component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, runElevated: true, username: username))
                {
                    Assert.IsNotNull(process.StartInfo);
                    Assert.AreEqual("sudo", process.StartInfo.FileName);
                    Assert.AreEqual($"-u {username} {command} {commandArguments}", process.StartInfo.Arguments);
                    Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
                }
            }
        }

        [Test]
        public void ExecuteCommandAsyncExtensionDoesNotSupportAUsernameSuppliedOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }

        [Test]
        public void ExecuteCommandAsyncExtensionDoesNotSupportAUsernameSuppliedUnlessRunningElevatedOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.fixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }
    }
}
