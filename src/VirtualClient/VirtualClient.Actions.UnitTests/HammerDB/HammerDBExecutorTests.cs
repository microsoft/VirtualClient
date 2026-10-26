// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class HammerDBExecutorTests
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
            this.mockPackage = new DependencyPath("hammerdb", this.fixture.PlatformSpecifics.GetPackagePath("hammerdb"));
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

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
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

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("file-password", await executor.OnGetPostgreSQLPassword(CancellationToken.None));
            Assert.AreEqual("environment-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
        }

        [Test]
        public async Task PostgreSQLPasswordEnvironmentFallbackDoesNotReadAFile()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
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

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
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

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
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

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnGetPostgreSQLPassword(CancellationToken.None));
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void HammerDBExecutorThrowsOnUnsupportedDistroAsync()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "TestUbuntu",
                Distribution = LinuxDistribution.OpenSuse
            };
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestHammerDBExecutor HammerDBExecutor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => HammerDBExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [TestCase(0, "password with 'quotes' and $symbols")]
        [TestCase(1, "password with 'quotes' and $symbols")]
        [TestCase(0, "1")]
        public async Task ProcessOutputObscuresCredentialInTelemetryFilesAndFailures(int exitCode, string password)
        {
            this.fixture.Parameters[nameof(HammerDBExecutor.LogToFile)] = true;
            List<string> logFiles = new List<string>();
            List<string> loggedOutput = new List<string>();
            this.fixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.EndsWith(".log", StringComparison.Ordinal))
                    {
                        logFiles.Add(content);
                    }
                })
                .Returns(Task.CompletedTask);
            this.fixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                if (eventInfo.Name.EndsWith(".ProcessDetails", StringComparison.Ordinal))
                {
                    JObject details = JObject.FromObject(((EventContext)state).Properties["process"]);
                    loggedOutput.Add(details["standardOutput"].Value<string>());
                    loggedOutput.Add(details["standardError"].Value<string>());
                }
            };

            using InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo { FileName = "hammerdbcli", Arguments = "auto runTransactions.tcl" },
                ExitCode = exitCode,
                OnHasExited = () => true
            };
            string output = $"Changed tpcc:pg_pass from postgres to {password} for PostgreSQL\nTransactions/sec: 120";
            process.StandardOutput.Append(output);
            process.StandardError.Append($"Connection details: {password}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.OnLogPostgreSQLProcessDetails(process, password, EventContext.None, "HammerDB");

            Assert.AreEqual(1, logFiles.Count);
            Assert.AreEqual(2, loggedOutput.Count);
            Assert.AreEqual(output.Replace(password, "...", StringComparison.Ordinal), loggedOutput[0]);
            Assert.AreEqual("Connection details: ...", loggedOutput[1]);
            StringAssert.Contains(loggedOutput[0], logFiles[0]);
            StringAssert.Contains(loggedOutput[1], logFiles[0]);
            StringAssert.DoesNotContain($"to {password} for PostgreSQL", logFiles[0]);
            StringAssert.DoesNotContain($"Connection details: {password}", logFiles[0]);

            if (exitCode == 0)
            {
                Assert.AreEqual(output, process.StandardOutput.ToString(), "Metric input must remain unchanged.");
            }
            else
            {
                WorkloadException exception = Assert.Throws<WorkloadException>(() =>
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed));
                StringAssert.DoesNotContain(password, exception.ToString());
            }
        }

        private class TestHammerDBExecutor : HammerDBExecutor
        {
            public TestHammerDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<CancellationToken, Task<string>> OnGetPostgreSQLPassword => base.GetPostgreSQLPasswordAsync;

            public Func<IProcessProxy, string, EventContext, string, Task> OnLogPostgreSQLProcessDetails => base.LogPostgreSQLProcessDetailsAsync;
        }
    }
}
