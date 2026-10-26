// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    internal class PostgreSQLCredentialsTests
    {
        private MockFixture mockFixture;
        private string originalPassword;

        [SetUp]
        public void SetupDefaults()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", null);
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [TearDown]
        public void RestorePasswordEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task PostgreSQLPasswordIsReadFromTheArchitectureSpecificPackage(Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Unix, architecture);
            DependencyPath package = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(package);
            string platformPath = this.mockFixture.ToPlatformSpecificPath(package, PlatformID.Unix, architecture).Path;
            this.mockFixture.Directory.Setup(directory => directory.Exists(platformPath)).Returns(true);
            string passwordFile = this.mockFixture.PlatformSpecifics.Combine(platformPath, "Password.json");
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file password with 'quotes' and $symbols\"}");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("file password with 'quotes' and $symbols", await PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
            Assert.IsNull(Environment.GetEnvironmentVariable("PGPASSWORD"));
            this.mockFixture.File.Verify(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PostgreSQLPasswordExplicitFileTakesPrecedenceOverEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file-password\"}");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("file-password", await PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
            Assert.AreEqual("environment-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
        }

        [Test]
        public async Task PostgreSQLPasswordEnvironmentFallbackDoesNotReadAFile()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");

            using TestExecutor component = new TestExecutor(this.mockFixture);
            Assert.AreEqual("environment-password", await PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
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
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>())).ReturnsAsync(contents);

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordRejectsMissingExplicitFileWithoutFallingBackToEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = "/secure/missing.json";
            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("environment-password", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordReadErrorsDoNotExposeUnderlyingExceptionDetails()
        {
            const string passwordFile = "/secure/Password.json";
            this.mockFixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.mockFixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("sensitive-sentinel"));

            using TestExecutor component = new TestExecutor(this.mockFixture);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => PostgreSQLCredentials.GetPasswordAsync(component, CancellationToken.None));
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }
    }
}
