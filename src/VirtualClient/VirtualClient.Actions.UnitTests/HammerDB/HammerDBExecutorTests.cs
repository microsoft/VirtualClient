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
    using Polly.Utilities;
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
        private Func<TimeSpan, CancellationToken, Task> originalSleepAsync;
        private string passwordFile;

        [SetUp]
        public void SetUpTests()
        {
            this.originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            this.originalSleepAsync = SystemClock.SleepAsync;
            Environment.SetEnvironmentVariable("PGPASSWORD", null);
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = HammerDBExecutor.DatabaseServer.PostgreSQL;
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = "postgresql";
            this.mockPackage = new DependencyPath("hammerdb", this.fixture.PlatformSpecifics.GetPackagePath("hammerdb"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            DependencyPath postgreSQLPackage = new DependencyPath("postgresql", this.fixture.GetPackagePath("postgresql"));
            this.fixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(postgreSQLPackage);
            this.passwordFile = this.fixture.PlatformSpecifics.Combine(
                this.fixture.ToPlatformSpecificPath(postgreSQLPackage, PlatformID.Unix, Architecture.X64).Path, "Password.json");

            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [TearDown]
        public void RestoreEnvironmentAndClock()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", this.originalPassword);
            SystemClock.SleepAsync = this.originalSleepAsync;
        }

        [TestCase(Architecture.X64, "postgresql", "postgresql")]
        [TestCase(Architecture.Arm64, "postgresql", "postgresql")]
        [TestCase(Architecture.X64, "postgresql", "custom-postgresql")]
        [TestCase(Architecture.X64, "mysql", "custom-mysql")]
        public async Task DatabasePasswordIsReadFromTheConfiguredArchitectureSpecificPackage(Architecture architecture, string sqlServer, string packageName)
        {
            this.fixture.Setup(PlatformID.Unix, architecture);
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = sqlServer;
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = packageName;
            DependencyPath package = new DependencyPath(packageName, this.fixture.GetPackagePath(packageName));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.fixture.PackageManager.OnGetPackage(packageName).ReturnsAsync(package);
            string platformPath = this.fixture.ToPlatformSpecificPath(package, PlatformID.Unix, architecture).Path;
            this.fixture.Directory.Setup(directory => directory.Exists(platformPath)).Returns(true);
            string passwordFile = this.fixture.PlatformSpecifics.Combine(platformPath, "Password.json");
            this.fixture.File.Setup(file => file.Exists(passwordFile)).Returns(true);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file password with 'quotes' and $symbols\"}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("file password with 'quotes' and $symbols", await executor.OnReadPasswordFile(CancellationToken.None));
            Assert.IsNull(Environment.GetEnvironmentVariable("PGPASSWORD"));
            this.fixture.File.Verify(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task PostgreSQLPasswordUsesThePackageFileRegardlessOfCustomPathOrEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            this.fixture.Parameters["PostgreSQLPasswordFile"] = "/secure/Password.json";
            this.fixture.File.Setup(file => file.ReadAllTextAsync(this.passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"file-password\"}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("file-password", await executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual("environment-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
            this.fixture.File.Verify(file => file.ReadAllTextAsync("/secure/Password.json", It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public void PostgreSQLPasswordRejectsMissingPackageFileWithoutFallingBackToEnvironment()
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "environment-password");
            this.fixture.File.Setup(file => file.Exists(this.passwordFile)).Returns(false);

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
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
            this.fixture.File.Setup(file => file.ReadAllTextAsync(this.passwordFile, It.IsAny<CancellationToken>())).ReturnsAsync(contents);

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [Test]
        public void PostgreSQLPasswordReadErrorsDoNotExposeUnderlyingExceptionDetails()
        {
            this.fixture.File.Setup(file => file.ReadAllTextAsync(this.passwordFile, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("sensitive-sentinel"));

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnReadPasswordFile(CancellationToken.None));
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
        }

        [TestCase("Server")]
        [TestCase("Client")]
        public async Task MySQLPasswordIsReadFromTheDefaultPackageFileOnEachRole(string role)
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "unrelated-password");
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = "MySQL";
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = "mysql-server";
            this.fixture.SystemManagement.SetupGet(manager => manager.AgentId).Returns($"{Environment.MachineName}-{role}");
            DependencyPath package = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(package);
            string packagePath = this.fixture.ToPlatformSpecificPath(package, PlatformID.Unix, Architecture.X64).Path;
            string passwordFile = this.fixture.PlatformSpecifics.Combine(packagePath, "Password.json");
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"mysql-password\"}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("mysql-password", await executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual("unrelated-password", Environment.GetEnvironmentVariable("PGPASSWORD"));
        }

        [TestCase("")]
        [TestCase("{}")]
        [TestCase("{\"Password\":123}")]
        [TestCase("{\"Password\":\"sensitive-sentinel\",")]
        public void MySQLPasswordRejectsInvalidFilesWithoutUsingPostgreSQLCredentials(string contents)
        {
            Environment.SetEnvironmentVariable("PGPASSWORD", "unrelated-password");
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = "mysql";
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = "mysql-server";
            DependencyPath package = new DependencyPath("mysql-server", this.fixture.GetPackagePath("mysql-server"));
            this.fixture.PackageManager.OnGetPackage("mysql-server").ReturnsAsync(package);
            string packagePath = this.fixture.ToPlatformSpecificPath(package, PlatformID.Unix, Architecture.X64).Path;
            string passwordFile = this.fixture.PlatformSpecifics.Combine(packagePath, "Password.json");
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(contents);

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            DependencyException exception = Assert.ThrowsAsync<DependencyException>(() => executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
            StringAssert.DoesNotContain("sensitive-sentinel", exception.ToString());
            StringAssert.DoesNotContain("unrelated-password", exception.ToString());
        }

        [TestCase("postgresql")]
        [TestCase("mysql")]
        public async Task ClientWaitsForTheDistributedPasswordBeforeReadingIt(string database)
        {
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = database;
            this.fixture.SystemManagement.SetupGet(manager => manager.AgentId).Returns($"{Environment.MachineName}-Client");
            string packageName = string.Equals(database, HammerDBExecutor.DatabaseServer.PostgreSQL, StringComparison.OrdinalIgnoreCase)
                ? "postgresql"
                : "mysql-server";
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = packageName;
            DependencyPath package = new DependencyPath(packageName, this.fixture.GetPackagePath(packageName));
            this.fixture.PackageManager.OnGetPackage(packageName).ReturnsAsync(package);
            string passwordFile = this.fixture.PlatformSpecifics.Combine(
                this.fixture.ToPlatformSpecificPath(package, PlatformID.Unix, Architecture.X64).Path, "Password.json");
            int waits = 0;
            this.fixture.File.Setup(file => file.Exists(passwordFile)).Returns(() => waits >= 2);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"distributed-password\"}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            SystemClock.SleepAsync = (timeout, token) =>
            {
                token.ThrowIfCancellationRequested();
                waits++;
                return Task.CompletedTask;
            };
            Assert.AreEqual("distributed-password", await executor.OnReadPasswordFile(CancellationToken.None));
            Assert.AreEqual(2, waits);
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(false, 0)]
        [TestCase(false, 2)]
        public void ClientPasswordWaitIsBoundedAndCancellable(bool cancel, int? maxRetries)
        {
            if (maxRetries.HasValue)
            {
                this.fixture.Parameters[nameof(HammerDBExecutor.MaxRetries)] = maxRetries.Value;
            }

            this.fixture.SystemManagement.SetupGet(manager => manager.AgentId).Returns($"{Environment.MachineName}-Client");
            this.fixture.File.Setup(file => file.Exists(this.passwordFile)).Returns(false);
            using CancellationTokenSource cancellation = new CancellationTokenSource();
            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            int waits = 0;
            SystemClock.SleepAsync = (timeout, token) =>
            {
                Assert.AreEqual(TimeSpan.FromSeconds(10), timeout);
                waits++;
                if (cancel)
                {
                    cancellation.Cancel();
                }

                token.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            };

            if (cancel)
            {
                Assert.ThrowsAsync<OperationCanceledException>(() => executor.OnReadPasswordFile(cancellation.Token));
                Assert.AreEqual(1, waits);
            }
            else
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.OnReadPasswordFile(cancellation.Token));
                Assert.AreEqual(maxRetries ?? 60, waits);
            }

            this.fixture.File.Verify(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never());
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

        [TestCase("postgresql", "tpcc")]
        [TestCase("PostgreSQL", "tpch")]
        [TestCase("mysql", "tpcc")]
        [TestCase("MySQL", "tpch")]
        [TestCase(HammerDBExecutor.DatabaseServer.PostgreSQL, HammerDBExecutor.BenchmarkName.TPCC)]
        [TestCase(HammerDBExecutor.DatabaseServer.MySQL, HammerDBExecutor.BenchmarkName.TPCH)]
        public async Task HammerDBExecutorConfiguresAndPopulatesTheSelectedDatabase(string sqlServer, string workload)
        {
            bool isPostgreSQL = string.Equals(sqlServer, HammerDBExecutor.DatabaseServer.PostgreSQL, StringComparison.OrdinalIgnoreCase);
            const string password = "password with 'quotes' and $symbols";
            Environment.SetEnvironmentVariable("PGPASSWORD", "unrelated-password");
            this.fixture.Parameters[nameof(HammerDBExecutor.PackageName)] = "hammerdb";
            this.fixture.Parameters[nameof(HammerDBExecutor.SQLServer)] = sqlServer;
            this.fixture.Parameters[nameof(HammerDBExecutor.Workload)] = workload;
            this.fixture.Parameters[nameof(HammerDBExecutor.Port)] = isPostgreSQL ? 5432 : 3306;
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabaseName)] = "hammerdbtest";
            this.fixture.Parameters[nameof(HammerDBExecutor.VirtualUserCount)] = "1";
            this.fixture.Parameters[nameof(HammerDBExecutor.WarehouseCount)] = "1";
            this.fixture.Parameters[nameof(HammerDBExecutor.ScaleFactor)] = "10";
            this.fixture.Parameters[nameof(HammerDBExecutor.Duration)] = "00:01:00";
            string databasePackageName = isPostgreSQL ? "postgresql" : "mysql-server";
            this.fixture.Parameters[nameof(HammerDBExecutor.DatabasePackageName)] = databasePackageName;
            DependencyPath databasePackage = new DependencyPath(databasePackageName, this.fixture.GetPackagePath(databasePackageName));
            this.fixture.PackageManager.OnGetPackage(databasePackageName).ReturnsAsync(databasePackage);
            string passwordFile = this.fixture.PlatformSpecifics.Combine(
                this.fixture.ToPlatformSpecificPath(databasePackage, PlatformID.Unix, Architecture.X64).Path, "Password.json");
            this.fixture.File.Setup(file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{\"Password\":\"password with 'quotes' and $symbols\"}");
            this.fixture.Directory.Setup(directory => directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.SystemManagement.SetupGet(manager => manager.AgentId).Returns($"{Environment.MachineName}-Server");
            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new HammerDBExecutor.HammerDBState
            {
                HammerDBInitialized = true
            }));

            List<string> commands = new List<string>();
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                commands.Add($"{command} {arguments}");
                StringAssert.DoesNotContain(password, arguments);
                InMemoryProcess process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = command, Arguments = arguments },
                    ExitCode = 0,
                    OnHasExited = () => true
                };
                process.OnStart = () =>
                {
                    Assert.AreEqual(
                        isPostgreSQL ? password : Environment.GetEnvironmentVariable("PGPASSWORD"),
                        process.EnvironmentVariables["PGPASSWORD"]);
                    if (!isPostgreSQL)
                    {
                        Assert.AreEqual(password, process.EnvironmentVariables["MYSQLPASSWORD"]);
                    }

                    return true;
                };
                return process;
            };

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.ExecuteAsync(CancellationToken.None);

            Assert.AreEqual(2, commands.Count);
            StringAssert.Contains($"configure-workload-generator.py --workload {workload.ToLowerInvariant()} --sqlServer {sqlServer.ToLowerInvariant()}", commands[0]);
            StringAssert.Contains("populate-database.py --createDBTCLPath createDB.tcl", commands[1]);
            this.fixture.File.Verify(
                file => file.ReadAllTextAsync(passwordFile, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestCase(0, "password with 'quotes' and $symbols")]
        [TestCase(1, "password with 'quotes' and $symbols")]
        [TestCase(0, "1")]
        [TestCase(0, null)]
        [TestCase(1, null)]
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
            Assert.AreEqual(password == null ? output : output.Replace(password, "...", StringComparison.Ordinal), loggedOutput[0]);
            Assert.AreEqual(password == null ? "Connection details: " : "Connection details: ...", loggedOutput[1]);
            StringAssert.Contains(loggedOutput[0], logFiles[0]);
            StringAssert.Contains(loggedOutput[1], logFiles[0]);
            if (password != null)
            {
                StringAssert.DoesNotContain($"to {password} for PostgreSQL", logFiles[0]);
                StringAssert.DoesNotContain($"Connection details: {password}", logFiles[0]);
            }

            if (exitCode == 0 || password == null)
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

            public Func<IProcessProxy, string, EventContext, string, Task> OnLogPostgreSQLProcessDetails => base.LogDatabaseProcessDetailsAsync;

            public async Task<string> OnReadPasswordFile(CancellationToken cancellationToken)
            {
                await base.ReadPasswordFileAsync(cancellationToken).ConfigureAwait(false);
                return this.DatabasePassword;
            }

        }
    }
}
