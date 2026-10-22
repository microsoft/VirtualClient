// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
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
    [NonParallelizable]
    internal class VirtualClientComponentExtensionsTests
    {
        private MockFixture mockFixture;
        private string originalPassword;

        [SetUp]
        public void SavePasswordEnvironment()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", null);
        }

        [TearDown]
        public void RestorePasswordEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task PostgreSQLPasswordIsReadFromTheArchitectureSpecificPackage(Architecture architecture)
        {
            this.SetupDefaults(PlatformID.Unix);
            this.mockFixture.Setup(PlatformID.Unix, architecture);
            DependencyPath package = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(package);
            string platformPath = this.mockFixture.ToPlatformSpecificPath(package, PlatformID.Unix, architecture).Path;
            string passwordFile = this.mockFixture.PlatformSpecifics.Combine(platformPath, "Password.json");
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file password with 'quotes' and $symbols\"}");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("file password with 'quotes' and $symbols", await component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            Assert.IsNull(Environment.GetEnvironmentVariable("PGPASSWORD"));
            this.mockFixture.File.Verify(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PostgreSQLPasswordExplicitFileTakesPrecedenceOverEnvironment()
        {
            this.SetupDefaults(PlatformID.Unix);
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file-password\"}");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("file-password", await component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            Assert.AreEqual("environment-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
        }

        [Test]
        public async Task PostgreSQLPasswordEnvironmentFallbackDoesNotReadAFile()
        {
            this.SetupDefaults(PlatformID.Unix);
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("environment-password", await component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            this.mockFixture.File.Verify(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"Password\":\"\"}")]
        [TestCase("{\"Password\":null}")]
        [TestCase("{\"Password\":123}")]
        [TestCase("{\"Password\":\"sensitive-sentinel\",")]
        public void PostgreSQLPasswordRejectsInvalidFilesWithoutDisclosingTheirContents(string contents)
        {
            this.SetupDefaults(PlatformID.Unix);
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>())).ReturnsAsync(contents);

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordRejectsMissingExplicitFileWithoutFallingBackToEnvironment()
        {
            this.SetupDefaults(PlatformID.Unix);
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = "/secure/missing.json";
            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("environment-password", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordReadErrorsDoNotExposeUnderlyingExceptionDetails()
        {
            this.SetupDefaults(PlatformID.Unix);
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("sensitive-sentinel"));

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => component.GetPostgreSQLPasswordAsync(CancellationToken.None));
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
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
