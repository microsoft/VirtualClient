// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    internal class ProcessExtensionsTests
    {
        private MockFixture fixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform);
        }

        [Test]
        public void CreateElevatedProcessExtensionCreatesTheExpectedProcessOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            string command = "anycommand.exe";
            string commandArguments = "--option1=123 --option2=456";
            string workingDirectory = MockFixture.TestAssemblyDirectory;

            using (IProcessProxy process = this.fixture.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, command, commandArguments, workingDirectory))
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

            using (IProcessProxy process = this.fixture.ProcessManager.CreateElevatedProcess(PlatformID.Unix, command, commandArguments, workingDirectory))
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

            using (IProcessProxy process = this.fixture.ProcessManager.CreateElevatedProcess(PlatformID.Unix, command, commandArguments, workingDirectory, username))
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
                () => this.fixture.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, "anycommand", "anyarguments", "anydir", "anyusername"));
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_1()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>());

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_2()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed."));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_3()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed.", ErrorReason.WorkloadFailed));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_4()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardOutput.Append("Unable to complete operation.");
            this.fixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>());

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardOutput: {this.fixture.Process.StandardOutput}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_5()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardOutput.Append("Unable to complete operation.");
            this.fixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed."));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardOutput: {this.fixture.Process.StandardOutput}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedExceptionWhenMatchesAreFound_1()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("A specific error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>(expressions: new Regex("specific error")));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedExceptionWhenMatchesAreFound_2()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("A specific error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.fixture.Process.ThrowOnStandardError<WorkloadException>(errorReason: ErrorReason.WorkloadFailed, expressions: new Regex("specific error")));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.fixture.Process.StartInfo.FileName} {this.fixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.fixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionDoesNotThrowAnExceptionWhenMatchesAreNotFound()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.fixture.Process.ExitCode = 1;
            this.fixture.Process.StandardError.Append("A specific error occurred.");

            Assert.DoesNotThrow(() => this.fixture.Process.ThrowOnStandardError<WorkloadException>(expressions: new Regex("some other error")));
        }
    }
}
