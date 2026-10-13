// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class VirtualClientComponentExtensionsTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        public async Task ExecuteCommandExtensionExecutesTheExpectedProcessOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
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
        public async Task ExecuteCommandExtensionExecutesTheExpectedProcessOnWindowsSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            // There is no different on Windows systems.
            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
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
        public async Task ExecuteCommandExtensionExecutesTheExpectedProcessOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
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
        public async Task ExecuteCommandExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevated()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
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
        public async Task ExecuteCommandExtensionExecutesTheExpectedProcessOnUnixSystemsWhenRunningElevatedAndAUsernameIsSupplied()
        {
            this.SetupDefaults(PlatformID.Unix);

            string username = "anyuser";
            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
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
        public void ExecuteCommandExtensionDoesNotSupportAUsernameSuppliedOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }

        [Test]
        public void ExecuteCommandExtensionDoesNotSupportAUsernameSuppliedUnlessRunningElevatedOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anycommand";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                Assert.ThrowsAsync<NotSupportedException>(
                    () => component.ExecuteCommandAsync(command, commandArguments, workingDirectory, EventContext.None, CancellationToken.None, username: "notsupported"));
            }
        }
    }
}
