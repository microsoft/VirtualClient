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

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_1()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>());

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_2()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed."));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_3()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed.", ErrorReason.WorkloadFailed));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_4()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardOutput.Append("Unable to complete operation.");
            this.mockFixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>());

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardOutput: {this.mockFixture.Process.StandardOutput}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedException_5()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardOutput.Append("Unable to complete operation.");
            this.mockFixture.Process.StandardError.Append("An error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>("Command execution failed."));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Command execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardOutput: {this.mockFixture.Process.StandardOutput}" +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedExceptionWhenMatchesAreFound_1()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("A specific error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>(expressions: new Regex("specific error")));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.Undefined, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionThrowsTheExpectedExceptionWhenMatchesAreFound_2()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("A specific error occurred.");

            WorkloadException exception = Assert.Throws<WorkloadException>(
                () => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>(errorReason: ErrorReason.WorkloadFailed, expressions: new Regex("specific error")));

            Assert.IsNotNull(exception);
            Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);

            Assert.AreEqual(
                $"Process execution failed (error/exit code=1, command={this.mockFixture.Process.StartInfo.FileName} {this.mockFixture.Process.StartInfo.Arguments})." +
                $"{Environment.NewLine}{Environment.NewLine}" +
                $"StandardError: {this.mockFixture.Process.StandardError}",
                exception.Message);
        }

        [Test]
        public void ThrowOnStandardErrorExtensionDoesNotThrowAnExceptionWhenMatchesAreNotFound()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            this.mockFixture.Process.ExitCode = 1;
            this.mockFixture.Process.StandardError.Append("A specific error occurred.");

            Assert.DoesNotThrow(() => this.mockFixture.Process.ThrowOnStandardError<WorkloadException>(expressions: new Regex("some other error")));
        }
    }
}
