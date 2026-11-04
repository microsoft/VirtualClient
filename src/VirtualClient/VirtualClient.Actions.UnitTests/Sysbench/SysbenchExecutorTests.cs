// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class SysbenchExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string originalPassword;

        [SetUp]
        public void SetUpTests()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            Environment.SetEnvironmentVariable("PGPASSWORD", null);
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("sysbench", this.fixture.PlatformSpecifics.GetPackagePath("sysbench"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [TearDown]
        public void RestorePasswordEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
        }

        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task PostgreSQLPasswordIsReadFromTheArchitectureSpecificPackage(Architecture architecture)
        {
            this.fixture.Setup(PlatformID.Unix, architecture);
            DependencyPath package = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(package);
            string platformPath = this.fixture.ToPlatformSpecificPath(package, PlatformID.Unix, architecture).Path;
            this.fixture.Directory.Setup(directory => directory.Exists(platformPath)).Returns(true);
            string passwordFile = this.fixture.PlatformSpecifics.Combine(platformPath, "Password.json");
            this.fixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file password with 'quotes' and $symbols\"}");

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("file password with 'quotes' and $symbols", await executor.OnGetPostgreSQLPassword(CancellationToken.None));
            Assert.IsNull(Environment.GetEnvironmentVariable("PGPASSWORD"));
            this.fixture.File.Verify(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PostgreSQLPasswordExplicitFileTakesPrecedenceOverEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            const string passwordFile = "/secure/Password.json";
            this.fixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file-password\"}");

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("file-password", await executor.OnGetPostgreSQLPassword(CancellationToken.None));
            Assert.AreEqual("environment-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
        }

        [Test]
        public async Task PostgreSQLPasswordEnvironmentFallbackDoesNotReadAFile()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("environment-password", await executor.OnGetPostgreSQLPassword(CancellationToken.None));
            this.fixture.File.Verify(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"Password\":\"\"}")]
        [TestCase("{\"Password\":null}")]
        [TestCase("{\"Password\":123}")]
        [TestCase("{\"Password\":\"sensitive-sentinel\",")]
        public void PostgreSQLPasswordRejectsInvalidFilesWithoutDisclosingTheirContents(string contents)
        {
            const string passwordFile = "/secure/Password.json";
            this.fixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>())).ReturnsAsync(contents);

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnGetPostgreSQLPassword(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordRejectsMissingExplicitFileWithoutFallingBackToEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            this.fixture.Parameters["PostgreSQLPasswordFile"] = "/secure/missing.json";
            this.fixture.File.Setup(file => file.Exists("/secure/missing.json")).Returns(false);

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnGetPostgreSQLPassword(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("environment-password", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordReadErrorsDoNotExposeUnderlyingExceptionDetails()
        {
            const string passwordFile = "/secure/Password.json";
            this.fixture.Parameters["PostgreSQLPasswordFile"] = passwordFile;
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("sensitive-sentinel"));

            using TestSysbenchExecutor executor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnGetPostgreSQLPassword(CancellationToken.None));
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void SysbenchExecutorThrowsOnUnsupportedDistroAsync()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "TestUbuntu",
                Distribution = LinuxDistribution.OpenSuse
            };
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestSysbenchExecutor SysbenchOLTPExecutor = new TestSysbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => SysbenchOLTPExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        private class TestSysbenchExecutor : SysbenchExecutor
        {
            public TestSysbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<CancellationToken, Task<string>> OnGetPostgreSQLPassword => base.GetPostgreSQLPasswordAsync;
        }
    }
}
